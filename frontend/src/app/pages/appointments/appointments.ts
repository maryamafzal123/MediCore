import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { Appointment } from '../../core/models';
import { fmtDateTime } from '../../core/format';
import { PrescriptionModal } from '../../shared/prescription-modal';

@Component({
  selector: 'app-appointments',
  imports: [FormsModule, RouterLink, PrescriptionModal],
  template: `
    <div class="page-head">
      <div><h1>Appointments</h1><p>{{ subtitle() }}</p></div>
      @if (role() === 'Patient') { <a class="btn btn-primary" routerLink="/doctors">+ Book appointment</a> }
    </div>
    <div class="card">
      <div class="card-head">
        <div class="row tabs">
          @for (t of tabs; track t) {
            <button class="btn btn-sm" [class.btn-primary]="tab() === t" (click)="tab.set(t)">{{ t }} ({{ count(t) }})</button>
          }
        </div>
        <input class="input search" placeholder="Search name…" [(ngModel)]="q">
      </div>
      @if (loading()) { <div class="empty"><div class="spinner" style="margin:auto"></div></div> }
      @else if (!filtered().length) { <div class="empty">No appointments here.</div> }
      @else {
        <div class="table-wrap"><table>
          <thead><tr>
            <th>Date & time</th>
            @if (role() !== 'Patient') { <th>Patient</th> }
            @if (role() !== 'Doctor') { <th>Doctor</th> }
            <th>Reason</th><th>Status</th><th></th>
          </tr></thead>
          <tbody>
            @for (a of filtered(); track a.id) {
              <tr>
                <td><b>{{ fmtDateTime(a.appointmentDate) }}</b></td>
                @if (role() !== 'Patient') { <td>{{ a.patientName }}</td> }
                @if (role() !== 'Doctor') { <td>Dr. {{ a.doctorName }}<div class="muted small">{{ a.specialization }}</div></td> }
                <td class="muted">{{ a.notes || '—' }}</td>
                <td><span class="badge badge-{{ a.status }}">{{ a.status }}</span></td>
                <td class="actions">
                  @if (a.status === 'Scheduled' && role() !== 'Doctor') {
                    <button class="btn btn-sm btn-danger" (click)="cancel(a)">Cancel</button>
                  }
                  @if (role() === 'Doctor' && a.status !== 'Cancelled' && !a.hasPrescription) {
                    <button class="btn btn-sm btn-primary" (click)="prescribing.set(a)">Prescribe</button>
                  }
                  @if (role() === 'Doctor' && a.status === 'Scheduled') {
                    <button class="btn btn-sm" (click)="setStatus(a, 'Completed')">Done</button>
                    <button class="btn btn-sm btn-danger" (click)="setStatus(a, 'Cancelled')">Cancel</button>
                  }
                  @if (a.hasPrescription && role() !== 'Admin') { <a class="btn btn-sm" routerLink="/prescriptions">℞ View</a> }
                </td>
              </tr>
            }
          </tbody>
        </table></div>
      }
    </div>
    @if (prescribing(); as a) {
      <app-prescription-modal [appointment]="a" (closed)="prescribing.set(null)" (saved)="prescribing.set(null); toast.success('Prescription saved'); load()" />
    }
  `,
  styles: `
    .search { max-width: 220px; }
    .actions { display: flex; gap: 6px; justify-content: flex-end; flex-wrap: wrap; }
  `,
})
export class Appointments implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  toast = inject(ToastService);
  role = this.auth.role;

  tabs = ['Upcoming', 'Past', 'Cancelled', 'All'] as const;
  tab = signal<(typeof this.tabs)[number]>('Upcoming');
  items = signal<Appointment[]>([]);
  loading = signal(true);
  prescribing = signal<Appointment | null>(null);
  q = '';
  fmtDateTime = fmtDateTime;

  subtitle = computed(() => ({ Patient: 'Your visits', Doctor: 'Your schedule', Admin: 'All hospital appointments' })[this.role()!]);

  private inTab(a: Appointment, t: string) {
    if (t === 'Upcoming') return a.status === 'Scheduled';
    if (t === 'Past') return a.status === 'Completed';
    if (t === 'Cancelled') return a.status === 'Cancelled';
    return true;
  }
  count(t: string) { return this.items().filter(a => this.inTab(a, t)).length; }

  filtered() {
    const q = this.q.toLowerCase();
    const list = this.items().filter(a => this.inTab(a, this.tab()) &&
      (!q || a.patientName?.toLowerCase().includes(q) || a.doctorName?.toLowerCase().includes(q)));
    // upcoming: soonest first; others: newest first
    return this.tab() === 'Upcoming' ? [...list].reverse() : list;
  }

  async ngOnInit() { await this.load(); }

  async load() {
    try {
      const r = this.role();
      this.items.set(await (r === 'Admin' ? this.api.allAppointments() : r === 'Doctor' ? this.api.doctorAppointments() : this.api.myAppointments()));
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loading.set(false); }
  }

  async cancel(a: Appointment) {
    if (!confirm(`Cancel appointment on ${fmtDateTime(a.appointmentDate)}?`)) return;
    try { await this.api.cancel(a.id); this.toast.success('Appointment cancelled'); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  async setStatus(a: Appointment, status: 'Completed' | 'Cancelled') {
    try { await this.api.setStatus(a.id, status); this.toast.success(`Marked ${status.toLowerCase()}`); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
}
