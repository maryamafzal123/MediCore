import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../core/auth.interceptor';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  styleUrl: './auth.css',
  template: `
    <div class="auth">
      <section class="hero">
        <div class="brand"><span class="logo">✚</span> MediCore</div>
        <div>
          <h1>Hospital care, organised, with an assistant that actually helps.</h1>
          <p>Book appointments, manage schedules and prescriptions, or just ask the AI assistant — in English or Urdu.</p>
        </div>
        <div class="chips"><span>Appointments</span><span>Prescriptions</span><span>AI Assistant</span><span>Role-based access</span></div>
      </section>
      <section class="panel">
        <form class="form" (ngSubmit)="submit()">
          <h2>Welcome back</h2>
          <p>Log in to your MediCore account</p>
          @if (error()) { <div class="error-box">{{ error() }}</div> }
          <div class="field"><label>Email</label><input class="input" type="email" name="email" [(ngModel)]="email" required></div>
          <div class="field"><label>Password</label><input class="input" type="password" name="password" [(ngModel)]="password" required></div>
          <button class="btn btn-primary btn-block" [disabled]="loading()">{{ loading() ? 'Logging in…' : 'Log in' }}</button>
          <div class="demo">
            <b>Demo accounts</b> (password <code>Password&#64;123</code>)<br>
            <button type="button" (click)="fill('admin@medicore.com')">Admin</button> ·
            <button type="button" (click)="fill('ahmed.khan@medicore.com')">Doctor</button> ·
            <button type="button" (click)="fill('patient@medicore.com')">Patient</button>
          </div>
          <div class="switch">New here? <a routerLink="/register">Create an account</a></div>
        </form>
      </section>
    </div>
  `,
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);
  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  fill(email: string) { this.email = email; this.password = 'Password@123'; }

  async submit() {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.login(this.email, this.password);
      this.router.navigate(['/dashboard']);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.loading.set(false);
    }
  }
}
