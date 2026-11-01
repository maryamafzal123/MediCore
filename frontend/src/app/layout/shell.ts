import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Role } from '../core/models';
import { initials } from '../core/format';

interface NavItem { path: string; label: string; icon: string; roles: Role[]; }

const NAV: NavItem[] = [
  { path: '/dashboard', label: 'Dashboard', icon: '◧', roles: ['Admin', 'Doctor', 'Patient'] },
  { path: '/assistant', label: 'AI Assistant', icon: '✦', roles: ['Admin', 'Doctor', 'Patient'] },
  { path: '/appointments', label: 'Appointments', icon: '◷', roles: ['Admin', 'Doctor', 'Patient'] },
  { path: '/doctors', label: 'Doctors', icon: '✚', roles: ['Admin', 'Patient'] },
  { path: '/patients', label: 'Patients', icon: '☺', roles: ['Admin', 'Doctor'] },
  { path: '/prescriptions', label: 'Prescriptions', icon: '℞', roles: ['Doctor', 'Patient'] },
  { path: '/users', label: 'Users', icon: '⚙', roles: ['Admin'] },
  { path: '/profile', label: 'My Profile', icon: '◉', roles: ['Doctor', 'Patient'] },
];

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="layout" [class.open]="menuOpen()">
      <aside class="sidebar">
        <div class="brand"><span class="logo">✚</span> MediCore</div>
        <nav>
          @for (item of nav(); track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active" (click)="menuOpen.set(false)">
              <span class="ico">{{ item.icon }}</span>{{ item.label }}
            </a>
          }
        </nav>
        <div class="me">
          <div class="avatar">{{ initials(auth.name()) }}</div>
          <div class="me-text">
            <div class="me-name">{{ auth.name() }}</div>
            <div class="me-role">{{ auth.role() }}</div>
          </div>
          <button class="logout" title="Log out" (click)="auth.logout()">⏻</button>
        </div>
      </aside>
      <div class="scrim" (click)="menuOpen.set(false)"></div>
      <main>
        <button class="menu-btn" (click)="menuOpen.set(true)">☰</button>
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    .layout { display: flex; min-height: 100vh; }
    .sidebar { width: 248px; background: var(--sidebar); color: #cfe3df; display: flex; flex-direction: column; padding: 20px 14px; position: sticky; top: 0; height: 100vh; flex-shrink: 0; }
    .brand { display: flex; align-items: center; gap: 10px; font-weight: 800; font-size: 19px; color: #fff; padding: 4px 10px 22px; letter-spacing: -.02em; }
    .logo { width: 32px; height: 32px; border-radius: 9px; background: var(--primary); display: grid; place-items: center; font-size: 16px; }
    nav { display: grid; gap: 2px; flex: 1; align-content: start; }
    nav a { display: flex; align-items: center; gap: 12px; padding: 10px 12px; border-radius: 10px; color: #b5cdc8; font-weight: 600; }
    nav a:hover { background: rgba(255,255,255,.06); color: #fff; }
    nav a.active { background: rgba(20, 184, 166, .16); color: #fff; }
    .ico { width: 20px; text-align: center; font-size: 15px; }
    .me { display: flex; align-items: center; gap: 10px; padding: 12px 8px 0; border-top: 1px solid rgba(255,255,255,.08); }
    .me .avatar { background: rgba(255,255,255,.1); color: #fff; }
    .me-text { flex: 1; min-width: 0; }
    .me-name { color: #fff; font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .me-role { font-size: 12px; color: #8fb0aa; }
    .logout { background: none; border: none; color: #8fb0aa; font-size: 18px; cursor: pointer; padding: 6px; border-radius: 8px; }
    .logout:hover { background: rgba(255,255,255,.08); color: #fff; }
    main { flex: 1; min-width: 0; padding: 28px 32px; }
    .menu-btn, .scrim { display: none; }
    @media (max-width: 860px) {
      .sidebar { position: fixed; z-index: 40; left: 0; transform: translateX(-100%); transition: transform .2s; }
      .layout.open .sidebar { transform: none; }
      .layout.open .scrim { display: block; position: fixed; inset: 0; background: rgba(0,0,0,.35); z-index: 30; }
      main { padding: 16px; }
      .menu-btn { display: inline-block; border: 1px solid var(--line); background: var(--surface); border-radius: 10px; padding: 6px 12px; font-size: 18px; margin-bottom: 14px; cursor: pointer; }
    }
  `,
})
export class Shell {
  auth = inject(AuthService);
  menuOpen = signal(false);
  nav = computed(() => NAV.filter(n => n.roles.includes(this.auth.role()!)));
  initials = initials;
}
