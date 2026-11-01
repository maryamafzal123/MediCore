import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/auth.interceptor';
import { Doctor, TimeSlot } from '../core/models';
import { todayISO } from '../core/format';

@Component({
  selector: 'app-book-modal',
  imports: [FormsModule],
  template: `
    <div class="backdrop" (click)="closed.emit()">
      <div class="modal" (click)="$event.stopPropagation()">
        <div class="modal-head">
          <div>
            <h2>Book with Dr. {{ doctor().fullName }}</h2>
            <div class="muted small">{{ doctor().specialization }} · PKR {{ doctor().consultationFee }}</div>
          </div>
          <button class="icon-btn" (click)="closed.emit()">✕</button>
        </div>
        @if (error()) { <div class="error-box">{{ error() }}</div> }
        <div class="field">
          <label>Date</label>
          <input class="input" type="date" [min]="minDate" [max]="maxDate" [(ngModel)]="date" (change)="loadSlots()">
        </div>
        <div class="field">
          <label>Time</label>
          @if (loading()) { <div class="spinner"></div> }
          @else if (!freeCount()) { <div class="muted">No free slots on this day. Try another date.</div> }
          @else {
            <div class="slots">
              @for (s of slots(); track s.startUtc) {
                <button type="button" class="slot" [class.sel]="selected()?.startUtc === s.startUtc" [disabled]="!s.isAvailable" (click)="selected.set(s)">
                  {{ to12h(s.localTime) }}
                </button>
              }
            </div>
          }
        </div>
        <div class="field"><label>Reason for visit</label><textarea [(ngModel)]="notes" maxlength="500" placeholder="e.g. Chest pain while walking"></textarea></div>
        <div class="modal-foot">
          <button class="btn" (click)="closed.emit()">Cancel</button>
          <button class="btn btn-primary" [disabled]="!selected() || saving()" (click)="book()">{{ saving() ? 'Booking…' : 'Confirm booking' }}</button>
        </div>
      </div>
    </div>
  `,
  styles: `
    .slots { display: grid; grid-template-columns: repeat(auto-fill, minmax(86px, 1fr)); gap: 8px; }
    .slot { padding: 8px 4px; border: 1px solid var(--line); border-radius: 9px; background: var(--surface); font: inherit; font-weight: 600; font-size: 13px; cursor: pointer; }
    .slot:hover:not(:disabled) { border-color: var(--primary); }
    .slot.sel { background: var(--primary); color: #fff; border-color: var(--primary); }
    .slot:disabled { color: #b8c2bf; text-decoration: line-through; cursor: not-allowed; background: #f7f9f8; }
  `,
})
export class BookModal implements OnInit {
  private api = inject(ApiService);
  doctor = input.required<Doctor>();
  closed = output<void>();
  booked = output<void>();

  minDate = todayISO();
  maxDate = todayISO(30);
  date = todayISO(1);
  notes = '';
  slots = signal<TimeSlot[]>([]);
  selected = signal<TimeSlot | null>(null);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  freeCount = () => this.slots().filter(s => s.isAvailable).length;

  ngOnInit() { this.loadSlots(); }

  async loadSlots() {
    this.loading.set(true);
    this.selected.set(null);
    try { this.slots.set(await this.api.availability(this.doctor().id, this.date)); }
    catch (e) { this.error.set(errorMessage(e)); }
    finally { this.loading.set(false); }
  }

  async book() {
    this.saving.set(true);
    this.error.set('');
    try {
      await this.api.book(this.doctor().id, this.selected()!.startUtc, this.notes);
      this.booked.emit();
    } catch (e) {
      this.error.set(errorMessage(e));
      this.loadSlots();
    } finally {
      this.saving.set(false);
    }
  }

  to12h(t: string) {
    const [h, m] = t.split(':').map(Number);
    return `${((h + 11) % 12) + 1}:${String(m).padStart(2, '0')} ${h < 12 ? 'AM' : 'PM'}`;
  }
}
