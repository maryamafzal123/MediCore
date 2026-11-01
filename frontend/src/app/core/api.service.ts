import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AdminStats, Appointment, AppointmentStatus, ChatMessage, ChatReply, Conversation,
  Doctor, Patient, Prescription, TimeSlot, UserRow,
} from './models';

// One place for every backend call. Returns Promises so components can use async/await.
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  private get<T>(url: string, params?: Record<string, string | number>) {
    return firstValueFrom(this.http.get<T>(this.base + url, { params }));
  }
  private send<T = unknown>(method: 'post' | 'put' | 'delete', url: string, body?: unknown): Promise<T> {
    const full = this.base + url;
    if (method === 'delete') return firstValueFrom(this.http.delete<T>(full));
    if (method === 'put') return firstValueFrom(this.http.put<T>(full, body ?? {}));
    return firstValueFrom(this.http.post<T>(full, body ?? {}));
  }

  // Doctors
  doctors(specialization = '') { return this.get<Doctor[]>('/doctor', specialization ? { specialization } : undefined); }
  myDoctorProfile() { return this.get<Doctor>('/doctor/myprofile'); }
  saveDoctorProfile(data: Partial<Doctor>, create: boolean) {
    return this.send(create ? 'post' : 'put', '/doctor/profile', data).catch(textOk);
  }
  deactivateDoctor(id: number) { return this.send('delete', `/doctor/${id}`).catch(textOk); }

  // Patients
  patients() { return this.get<Patient[]>('/patient'); }
  patient(id: number) { return this.get<Patient>(`/patient/${id}`); }
  myPatientProfile() { return this.get<Patient>('/patient/myprofile'); }
  savePatientProfile(data: Partial<Patient>, create: boolean) {
    return this.send(create ? 'post' : 'put', '/patient/profile', data).catch(textOk);
  }

  // Appointments
  allAppointments() { return this.get<Appointment[]>('/appointment'); }
  myAppointments() { return this.get<Appointment[]>('/appointment/myappointments'); }
  doctorAppointments() { return this.get<Appointment[]>('/appointment/doctorappointments'); }
  availability(doctorId: number, date: string) { return this.get<TimeSlot[]>('/appointment/availability', { doctorId, date }); }
  book(doctorId: number, appointmentDate: string, notes: string) {
    return this.send<Appointment>('post', '/appointment', { doctorId, appointmentDate, notes });
  }
  cancel(id: number) { return this.send('delete', `/appointment/${id}`).catch(textOk); }
  setStatus(id: number, status: AppointmentStatus) { return this.send('put', `/appointment/${id}/status`, { status }).catch(textOk); }

  // Prescriptions
  prescriptions() { return this.get<Prescription[]>('/prescription/myprescriptions'); }
  createPrescription(data: { appointmentId: number; diagnosis: string; medications: string; instructions: string; validUntil: string }) {
    return this.send('post', '/prescription', data).catch(textOk);
  }

  // Admin
  stats() { return this.get<AdminStats>('/admin/stats'); }
  users() { return this.get<UserRow[]>('/admin/users'); }
  toggleUser(id: number) { return this.send<{ id: number; isActive: boolean }>('put', `/admin/users/${id}/toggle`); }

  // Chat
  conversations() { return this.get<Conversation[]>('/chat/conversations'); }
  newConversation() { return this.send<Conversation>('post', '/chat/conversations'); }
  messages(id: number) { return this.get<ChatMessage[]>(`/chat/conversations/${id}/messages`); }
  sendMessage(id: number, message: string) { return this.send<ChatReply>('post', `/chat/conversations/${id}/messages`, { message }); }
  renameConversation(id: number, title: string) { return this.send('put', `/chat/conversations/${id}`, { id, title, updatedAt: new Date().toISOString() }); }
  deleteConversation(id: number) { return this.send('delete', `/chat/conversations/${id}`); }
}

// Several .NET endpoints return plain text ("Appointment cancelled successfully").
// Angular expects JSON, so a 200 with text arrives as a parse error — treat it as success.
function textOk(err: any): string {
  if (err?.status === 200) return err.error?.text ?? 'OK';
  throw err;
}
