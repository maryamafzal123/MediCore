import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { Prescription } from '../../core/models';
import { fmtDate } from '../../core/format';

@Component({
  selector: 'app-prescriptions',
  template: `
    <div class="page-head">
      <div><h1>Prescriptions</h1><p>{{ auth.role() === 'Doctor' ? 'Prescriptions you have written' : 'Your prescriptions from MediCore doctors' }}</p></div>
    </div>
    @if (loading()) { <div class="empty"><div class="spinner" style="margin:auto"></div></div> }
    <div class="list">
      @for (p of items(); track p.id) {
        <div class="card rx">
          <div class="rx-head">
            <div class="rx-icon">℞</div>
            <div class="grow">
              <h3>{{ p.diagnosis }}</h3>
              <div class="muted small">
                {{ auth.role() === 'Doctor' ? p.patientName : 'Dr. ' + p.doctorName }} · Visit on {{ fmtDate(p.appointmentDate) }}
              </div>
            </div>
            <span class="badge" [class]="expired(p) ? 'badge-Cancelled' : 'badge-Completed'">
              {{ expired(p) ? 'Expired' : 'Valid till ' + fmtDate(p.validUntil) }}
            </span>
          </div>
          <div class="sec"><label>Medications</label><p>{{ p.medications }}</p></div>
          <div class="sec"><label>Instructions</label><p>{{ p.instructions }}</p></div>
          <button class="btn btn-sm print" (click)="print()">Print</button>
        </div>
      } @empty {
        @if (!loading()) { <div class="card empty">No prescriptions yet.</div> }
      }
    </div>
  `,
  styles: `
    .list { display: grid; gap: 16px; max-width: 860px; }
    .rx-head { display: flex; gap: 14px; align-items: center; margin-bottom: 14px; flex-wrap: wrap; }
    .grow { flex: 1; min-width: 200px; }
    .rx-icon { width: 44px; height: 44px; border-radius: 12px; background: var(--primary-soft); color: var(--primary); display: grid; place-items: center; font-size: 20px; font-weight: 800; }
    .sec { padding: 12px 14px; background: #f7f9f8; border-radius: 10px; margin-bottom: 10px; }
    .sec label { font-size: 11.5px; text-transform: uppercase; letter-spacing: .05em; color: var(--muted); margin: 0; }
    .sec p { margin: 4px 0 0; white-space: pre-line; }
    @media print { .print { display: none; } }
  `,
})
export class Prescriptions implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  auth = inject(AuthService);
  items = signal<Prescription[]>([]);
  loading = signal(true);
  fmtDate = fmtDate;

  expired(p: Prescription) { return new Date(p.validUntil).getTime() < Date.now(); }
  print() { window.print(); }

  async ngOnInit() {
    try { this.items.set(await this.api.prescriptions()); }
    catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loading.set(false); }
  }
}
