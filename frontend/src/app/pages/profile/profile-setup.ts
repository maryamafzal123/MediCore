import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ProfileForm } from '../../shared/profile-form';

@Component({
  selector: 'app-profile-setup',
  imports: [ProfileForm],
  template: `
    <div class="wrap">
      <div class="card">
        <h1>Complete your profile</h1>
        <p class="muted">Hi {{ auth.name() }}, one last step before you start{{ auth.role() === 'Doctor' ? ' seeing patients' : ' booking appointments' }}.</p>
        <app-profile-form [create]="true" (saved)="router.navigate(['/dashboard'])" />
        <button class="btn btn-sm logout" (click)="auth.logout()">Log out</button>
      </div>
    </div>
  `,
  styles: `
    .wrap { min-height: 100vh; display: grid; place-items: center; padding: 20px; }
    .card { max-width: 620px; width: 100%; padding: 32px; }
    p { margin: 6px 0 24px; }
    .logout { margin-top: 18px; }
  `,
})
export class ProfileSetup {
  auth = inject(AuthService);
  router = inject(Router);
}
