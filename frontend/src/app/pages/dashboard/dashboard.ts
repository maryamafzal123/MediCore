import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { AdminStats, Appointment, Prescription } from '../../core/models';
import { fmtDate, fmtDateTime } from '../../core/format';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  auth = inject(AuthService);
  role = this.auth.role;

  stats = signal<AdminStats | null>(null);
  appointments = signal<Appointment[]>([]);
  prescriptions = signal<Prescription[]>([]);
  loading = signal(true);
  fmtDate = fmtDate;
  fmtDateTime = fmtDateTime;

  greeting = (() => {
    const h = new Date().getHours();
    return h < 12 ? 'Good morning' : h < 17 ? 'Good afternoon' : 'Good evening';
  })();

  upcoming = computed(() => this.appointments()
    .filter(a => a.status === 'Scheduled' && new Date(a.appointmentDate).getTime() > Date.now() - 3600_000)
    .sort((a, b) => a.appointmentDate.localeCompare(b.appointmentDate)));

  today = computed(() => {
    const d = new Date().toDateString();
    return this.appointments()
      .filter(a => new Date(a.appointmentDate).toDateString() === d && a.status !== 'Cancelled')
      .sort((a, b) => a.appointmentDate.localeCompare(b.appointmentDate));
  });

  completed = computed(() => this.appointments().filter(a => a.status === 'Completed').length);
  uniquePatients = computed(() => new Set(this.appointments().map(a => a.patientId)).size);

  maxDay = computed(() => Math.max(1, ...(this.stats()?.last7Days.map(d => d.count) ?? [1])));
  statusRows = computed(() => Object.entries(this.stats()?.appointmentsByStatus ?? {}));
  statusTotal = computed(() => this.statusRows().reduce((s, [, n]) => s + n, 0) || 1);
  specRows = computed(() => Object.entries(this.stats()?.doctorsBySpecialization ?? {}).sort((a, b) => b[1] - a[1]));

  suggestions = computed(() => ({
    Patient: ['Book a cardiologist for tomorrow', 'Mere upcoming appointments dikhao', 'What are the visiting hours?'],
    Doctor: ["What's my schedule today?", 'How many appointments do I have this week?', 'Show prescriptions I wrote'],
    Admin: ['Give me a summary of hospital stats', 'Which appointments are scheduled for tomorrow?', 'List all cancelled appointments'],
  })[this.role()!]);

  async ngOnInit() {
    try {
      const r = this.role();
      if (r === 'Admin') {
        const [s, a] = await Promise.all([this.api.stats(), this.api.allAppointments()]);
        this.stats.set(s);
        this.appointments.set(a);
      } else if (r === 'Doctor') {
        this.appointments.set(await this.api.doctorAppointments());
      } else {
        const [a, p] = await Promise.all([this.api.myAppointments(), this.api.prescriptions()]);
        this.appointments.set(a);
        this.prescriptions.set(p);
      }
    } catch (e) {
      this.toast.error(errorMessage(e));
    } finally {
      this.loading.set(false);
    }
  }

  dayLabel(iso: string) {
    return new Date(iso + 'T00:00:00').toLocaleDateString('en-PK', { weekday: 'short' });
  }
  time(iso: string) {
    return new Date(iso).toLocaleTimeString('en-PK', { hour: 'numeric', minute: '2-digit' });
  }
}
