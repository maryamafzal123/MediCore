export type Role = 'Admin' | 'Doctor' | 'Patient';
export type AppointmentStatus = 'Scheduled' | 'Completed' | 'Cancelled';

export interface AuthResponse { token: string; email: string; fullName: string; role: Role; }
export interface Me { userId: number; fullName: string; email: string; role: Role; hasProfile: boolean; }

export interface Doctor {
  id: number; fullName: string; email: string; specialization: string;
  licenseNumber: string; phoneNumber: string; consultationFee: number;
}

export interface Patient {
  id: number; fullName: string; email: string; phoneNumber: string;
  gender: string; bloodGroup: string; address: string; dateOfBirth: string;
}

export interface Appointment {
  id: number; doctorId: number; patientId: number; hasPrescription: boolean;
  appointmentDate: string; notes: string; status: AppointmentStatus;
  doctorName: string; patientName: string; specialization: string;
}

export interface TimeSlot { startUtc: string; localTime: string; isAvailable: boolean; }

export interface Prescription {
  id: number; appointmentId: number; diagnosis: string; medications: string; instructions: string;
  validUntil: string; patientName: string; doctorName: string; appointmentDate: string;
}

export interface AdminStats {
  totalDoctors: number; totalPatients: number; totalAppointments: number;
  todayAppointments: number; totalPrescriptions: number;
  appointmentsByStatus: Record<string, number>;
  doctorsBySpecialization: Record<string, number>;
  last7Days: { date: string; count: number }[];
}

export interface UserRow { id: number; fullName: string; email: string; role: Role; isActive: boolean; createdAt: string; }

export interface Conversation { id: number; title: string; updatedAt: string; }
export interface ChatMessage { id: number; role: 'user' | 'assistant'; content: string; createdAt: string; }
export interface ChatReply {
  conversationId: number; title: string;
  userMessage: ChatMessage; assistantMessage: ChatMessage; toolsUsed: string[];
}
