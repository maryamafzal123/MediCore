import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { Doctor } from '../../core/models';
import { initials } from '../../core/format';
import { BookModal } from '../../shared/book-modal';

@Component({
  selector: 'app-doctors',
  imports: [FormsModule, BookModal],
  template: `
    <div class="page-head">
      <div><h1>Doctors</h1><p>{{ doctors().length }} specialists available</p></div>
      <div class="row">
        <input class="input" style="width:220px" placeholder="Search doctor…" [(ngModel)]="q">
        <select style="width:190px" [(ngModel)]="spec">
          <option value="">All specializations</option>
          @for (s of specs(); track s) { <option [value]="s">{{ s }}</option> }
        </select>
      </div>
    </div>
    @if (loading()) { <div class="empty"><div class="spinner" style="margin:auto"></div></div> }
    <div class="grid">
      @for (d of filtered(); track d.id) {
        <div class="card doc">
          <div class="row">
            <div class="avatar lg">{{ initials(d.fullName) }}</div>
            <div><h3>Dr. {{ d.fullName }}</h3><span class="badge badge-neutral">{{ d.specialization }}</span></div>
          </div>
          <div class="meta">
            <div><span class="muted">Fee</span><b>PKR {{ d.consultationFee.toLocaleString() }}</b></div>
            <div><span class="muted">License</span><b>{{ d.licenseNumber }}</b></div>
          </div>
          @if (auth.role() === 'Patient') {
            <button class="btn btn-primary btn-block" (click)="booking.set(d)">Book appointment</button>
          } @else if (auth.role() === 'Admin') {
            <div class="muted small">{{ d.email }} · {{ d.phoneNumber }}</div>
            <button class="btn btn-danger btn-block" (click)="deactivate(d)">Deactivate</button>
          }
        </div>
      } @empty {
        @if (!loading()) { <div class="card empty">No doctors match your search.</div> }
      }
    </div>
    @if (booking(); as d) {
      <app-book-modal [doctor]="d" (closed)="booking.set(null)" (booked)="onBooked()" />
    }
  `,
  styles: `
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(270px, 1fr)); gap: 16px; }
    .doc { display: grid; gap: 14px; }
    .avatar.lg { width: 50px; height: 50px; font-size: 16px; }
    .doc h3 { margin-bottom: 4px; }
    .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 12px; background: #f7f9f8; border-radius: 10px; }
    .meta span { display: block; font-size: 12px; }
  `,
})
export class Doctors implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  auth = inject(AuthService);

  doctors = signal<Doctor[]>([]);
  loading = signal(true);
  booking = signal<Doctor | null>(null);
  q = '';
  spec = '';
  initials = initials;

  specs = computed(() => [...new Set(this.doctors().map(d => d.specialization))].sort());
  filtered() {
    const q = this.q.toLowerCase();
    return this.doctors().filter(d => (!this.spec || d.specialization === this.spec) && (!q || d.fullName.toLowerCase().includes(q)));
  }

  async ngOnInit() { await this.load(); }
  async load() {
    try { this.doctors.set(await this.api.doctors()); }
    catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loading.set(false); }
  }

  onBooked() {
    this.booking.set(null);
    this.toast.success('Appointment booked!');
    this.router.navigate(['/appointments']);
  }

  async deactivate(d: Doctor) {
    if (!confirm(`Deactivate Dr. ${d.fullName}? They will no longer be able to log in.`)) return;
    try { await this.api.deactivateDoctor(d.id); this.toast.success('Doctor deactivated'); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
}
