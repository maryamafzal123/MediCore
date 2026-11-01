import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, Me, Role } from './models';

const KEY = 'medicore.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Signals = reactive state (like useState in React)
  readonly session = signal<AuthResponse | null>(this.load());
  // null = not checked yet
  readonly hasProfile = signal<boolean | null>(null);

  readonly isLoggedIn = computed(() => !!this.session());
  readonly role = computed<Role | null>(() => this.session()?.role ?? null);
  readonly name = computed(() => this.session()?.fullName ?? '');

  get token(): string | null { return this.session()?.token ?? null; }

  async login(email: string, password: string) {
    const res = await firstValueFrom(this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, { email, password }));
    this.save(res);
    await this.refreshMe();
    return res;
  }

  async register(data: { firstName: string; lastName: string; email: string; password: string; role: Role }) {
    const res = await firstValueFrom(this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, data));
    this.save(res);
    this.hasProfile.set(false);
    return res;
  }

  async refreshMe(): Promise<Me | null> {
    if (!this.token) return null;
    try {
      const me = await firstValueFrom(this.http.get<Me>(`${environment.apiUrl}/auth/me`));
      this.hasProfile.set(me.hasProfile);
      return me;
    } catch {
      return null;
    }
  }

  logout() {
    localStorage.removeItem(KEY);
    this.session.set(null);
    this.hasProfile.set(null);
    this.router.navigate(['/login']);
  }

  private save(res: AuthResponse) {
    localStorage.setItem(KEY, JSON.stringify(res));
    this.session.set(res);
  }

  private load(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(KEY);
      if (!raw) return null;
      const s = JSON.parse(raw) as AuthResponse;
      // drop expired tokens
      const payload = JSON.parse(atob(s.token.split('.')[1]));
      return payload.exp * 1000 > Date.now() ? s : null;
    } catch {
      return null;
    }
  }
}
