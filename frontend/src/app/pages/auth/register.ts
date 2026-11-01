import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../core/auth.interceptor';
import { Role } from '../../core/models';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  styleUrl: './auth.css',
  template: `
    <div class="auth">
      <section class="hero">
        <div class="brand"><span class="logo">✚</span> MediCore</div>
        <div>
          <h1>Join MediCore in under a minute.</h1>
          <p>Patients book and track visits. Doctors manage their day and write prescriptions.</p>
        </div>
        <div class="chips"><span>Secure JWT login</span><span>Your data stays yours</span></div>
      </section>
      <section class="panel">
        <form class="form" (ngSubmit)="submit()">
          <h2>Create account</h2>
          <p>Choose how you'll use MediCore</p>
          @if (error()) { <div class="error-box">{{ error() }}</div> }
          <div class="field roles">
            <button type="button" class="role" [class.sel]="role === 'Patient'" (click)="role = 'Patient'"><b>Patient</b><span class="small muted">Book & track visits</span></button>
            <button type="button" class="role" [class.sel]="role === 'Doctor'" (click)="role = 'Doctor'"><b>Doctor</b><span class="small muted">Manage schedule</span></button>
          </div>
          <div class="grid-2">
            <div class="field"><label>First name</label><input class="input" name="fn" [(ngModel)]="firstName" required></div>
            <div class="field"><label>Last name</label><input class="input" name="ln" [(ngModel)]="lastName" required></div>
          </div>
          <div class="field"><label>Email</label><input class="input" type="email" name="email" [(ngModel)]="email" required></div>
          <div class="field"><label>Password</label><input class="input" type="password" name="pw" [(ngModel)]="password" minlength="6" required></div>
          <button class="btn btn-primary btn-block" [disabled]="loading()">{{ loading() ? 'Creating…' : 'Create account' }}</button>
          <div class="switch">Already have an account? <a routerLink="/login">Log in</a></div>
        </form>
      </section>
    </div>
  `,
})
export class Register {
  private auth = inject(AuthService);
  private router = inject(Router);
  role: Role = 'Patient';
  firstName = ''; lastName = ''; email = ''; password = '';
  loading = signal(false);
  error = signal('');

  async submit() {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.register({ firstName: this.firstName, lastName: this.lastName, email: this.email, password: this.password, role: this.role });
      this.router.navigate(['/profile-setup']);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.loading.set(false);
    }
  }
}
