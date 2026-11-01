import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards';
import { Shell } from './layout/shell';

// loadComponent = lazy loading: each page's code is downloaded only when visited
export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/login').then(m => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/register').then(m => m.Register) },
  { path: 'profile-setup', loadComponent: () => import('./pages/profile/profile-setup').then(m => m.ProfileSetup) },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'assistant', loadComponent: () => import('./pages/assistant/assistant').then(m => m.Assistant) },
      { path: 'appointments', loadComponent: () => import('./pages/appointments/appointments').then(m => m.Appointments) },
      { path: 'doctors', canActivate: [roleGuard('Admin', 'Patient')], loadComponent: () => import('./pages/doctors/doctors').then(m => m.Doctors) },
      { path: 'patients', canActivate: [roleGuard('Admin', 'Doctor')], loadComponent: () => import('./pages/patients/patients').then(m => m.Patients) },
      { path: 'prescriptions', canActivate: [roleGuard('Doctor', 'Patient')], loadComponent: () => import('./pages/prescriptions/prescriptions').then(m => m.Prescriptions) },
      { path: 'users', canActivate: [roleGuard('Admin')], loadComponent: () => import('./pages/users/users').then(m => m.Users) },
      { path: 'profile', canActivate: [roleGuard('Doctor', 'Patient')], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
