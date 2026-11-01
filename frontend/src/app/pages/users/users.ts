import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { UserRow } from '../../core/models';
import { fmtDate } from '../../core/format';

@Component({
  selector: 'app-users',
  imports: [FormsModule],
  template: `
    <div class="page-head">
      <div><h1>Users</h1><p>Activate or deactivate accounts</p></div>
      <div class="row">
        <select style="width:150px" [(ngModel)]="role"><option value="">All roles</option><option>Admin</option><option>Doctor</option><option>Patient</option></select>
        <input class="input" style="width:220px" placeholder="Search…" [(ngModel)]="q">
      </div>
    </div>
    <div class="card"><div class="table-wrap"><table>
      <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Joined</th><th>Status</th><th></th></tr></thead>
      <tbody>
        @for (u of filtered(); track u.id) {
          <tr>
            <td><b>{{ u.fullName }}</b></td>
            <td class="muted">{{ u.email }}</td>
            <td><span class="badge badge-neutral">{{ u.role }}</span></td>
            <td>{{ fmtDate(u.createdAt) }}</td>
            <td><span class="badge" [class]="u.isActive ? 'badge-Completed' : 'badge-Cancelled'">{{ u.isActive ? 'Active' : 'Disabled' }}</span></td>
            <td style="text-align:right">
              @if (u.role !== 'Admin') {
                <button class="btn btn-sm" [class.btn-danger]="u.isActive" (click)="toggle(u)">{{ u.isActive ? 'Deactivate' : 'Activate' }}</button>
              }
            </td>
          </tr>
        }
      </tbody>
    </table></div></div>
  `,
})
export class Users implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  users = signal<UserRow[]>([]);
  q = ''; role = '';
  fmtDate = fmtDate;

  filtered() {
    const q = this.q.toLowerCase();
    return this.users().filter(u => (!this.role || u.role === this.role) && (!q || (u.fullName + u.email).toLowerCase().includes(q)));
  }

  async ngOnInit() {
    try { this.users.set(await this.api.users()); } catch (e) { this.toast.error(errorMessage(e)); }
  }

  async toggle(u: UserRow) {
    try {
      const r = await this.api.toggleUser(u.id);
      this.users.update(list => list.map(x => x.id === u.id ? { ...x, isActive: r.isActive } : x));
      this.toast.success(r.isActive ? 'Account activated' : 'Account deactivated');
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
}
