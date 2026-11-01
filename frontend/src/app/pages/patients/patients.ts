import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { Patient } from '../../core/models';
import { initials } from '../../core/format';

@Component({
  selector: 'app-patients',
  imports: [FormsModule],
  template: `
    <div class="page-head">
      <div><h1>Patients</h1><p>{{ patients().length }} registered patients</p></div>
      <input class="input" style="width:240px" placeholder="Search name, email, phone…" [(ngModel)]="q">
    </div>
    <div class="card">
      @if (loading()) { <div class="empty"><div class="spinner" style="margin:auto"></div></div> }
      @else {
        <div class="table-wrap"><table>
          <thead><tr><th>Patient</th><th>Phone</th><th>Gender</th><th>Age</th><th>Blood</th><th>Address</th></tr></thead>
          <tbody>
            @for (p of filtered(); track p.id) {
              <tr>
                <td><div class="row"><div class="avatar">{{ initials(p.fullName) }}</div><div><b>{{ p.fullName }}</b><div class="muted small">{{ p.email }}</div></div></div></td>
                <td>{{ p.phoneNumber }}</td>
                <td>{{ p.gender }}</td>
                <td>{{ age(p.dateOfBirth) }}</td>
                <td><span class="badge badge-Cancelled">{{ p.bloodGroup }}</span></td>
                <td class="muted">{{ p.address }}</td>
              </tr>
            } @empty { <tr><td colspan="6" class="empty">No patients found.</td></tr> }
          </tbody>
        </table></div>
      }
    </div>
  `,
})
export class Patients implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  patients = signal<Patient[]>([]);
  loading = signal(true);
  q = '';
  initials = initials;

  filtered() {
    const q = this.q.toLowerCase();
    return this.patients().filter(p => !q || [p.fullName, p.email, p.phoneNumber].some(v => v.toLowerCase().includes(q)));
  }
  age(dob: string) { return Math.floor((Date.now() - new Date(dob).getTime()) / 31557600000); }

  async ngOnInit() {
    try { this.patients.set(await this.api.patients()); }
    catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loading.set(false); }
  }
}
