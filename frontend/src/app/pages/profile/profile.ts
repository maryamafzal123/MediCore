import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { ProfileForm } from '../../shared/profile-form';

@Component({
  selector: 'app-profile',
  imports: [ProfileForm],
  template: `
    <div class="page-head"><div><h1>My Profile</h1><p>{{ auth.name() }} · {{ auth.session()?.email }}</p></div></div>
    <div class="card" style="max-width: 680px">
      <app-profile-form (saved)="toast.success('Profile updated')" />
    </div>
  `,
})
export class Profile {
  auth = inject(AuthService);
  toast = inject(ToastService);
}
