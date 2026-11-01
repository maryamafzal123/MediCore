import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/auth.interceptor';
import { Appointment } from '../core/models';
import { fmtDateTime, todayISO } from '../core/format';

@Component({
  selector: 'app-prescription-modal',
  imports: [FormsModule],
  template: `
    <div class="backdrop" (click)="closed.emit()">
      <div class="modal" (click)="$event.stopPropagation()">
        <div class="modal-head">
          <div>
            <h2>Write prescription</h2>
            <div class="muted small">{{ appointment().patientName }} · {{ fmtDateTime(appointment().appointmentDate) }}</div>
          </div>
          <button class="icon-btn" (click)="closed.emit()">✕</button>
        </div>
        @if (error()) { <div class="error-box">{{ error() }}</div> }
        <div class="field"><label>Diagnosis</label><input class="input" [(ngModel)]="diagnosis" maxlength="500"></div>
        <div class="field"><label>Medications</label><textarea [(ngModel)]="medications" placeholder="Paracetamol 500mg — 1 tablet every 8 hours for 3 days"></textarea></div>
        <div class="field"><label>Instructions</label><textarea [(ngModel)]="instructions" placeholder="Rest, drink fluids, follow up in 1 week"></textarea></div>
        <div class="field"><label>Valid until</label><input class="input" type="date" [min]="today" [(ngModel)]="validUntil"></div>
        <p class="muted small">Saving a prescription marks the appointment as Completed.</p>
        <div class="modal-foot">
          <button class="btn" (click)="closed.emit()">Cancel</button>
          <button class="btn btn-primary" [disabled]="saving() || !diagnosis || !medications || !instructions" (click)="save()">{{ saving() ? 'Saving…' : 'Save prescription' }}</button>
        </div>
      </div>
    </div>
  `,
})
export class PrescriptionModal {
  private api = inject(ApiService);
  appointment = input.required<Appointment>();
  closed = output<void>();
  saved = output<void>();

  today = todayISO(1);
  diagnosis = ''; medications = ''; instructions = '';
  validUntil = todayISO(30);
  saving = signal(false);
  error = signal('');
  fmtDateTime = fmtDateTime;

  async save() {
    this.saving.set(true);
    this.error.set('');
    try {
      await this.api.createPrescription({
        appointmentId: this.appointment().id, diagnosis: this.diagnosis, medications: this.medications,
        instructions: this.instructions, validUntil: this.validUntil + 'T23:59:00Z',
      });
      this.saved.emit();
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
}
