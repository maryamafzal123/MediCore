import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/auth.interceptor';

// Same form for "first-time setup" (create=true) and "edit my profile" (create=false)
@Component({
  selector: 'app-profile-form',
  imports: [FormsModule],
  template: `
    <form (ngSubmit)="save()">
      @if (error()) { <div class="error-box">{{ error() }}</div> }
      @if (auth.role() === 'Doctor') {
        <div class="grid-2">
          <div class="field"><label>Specialization</label>
            <select name="spec" [(ngModel)]="doc.specialization" required>
              @for (s of specializations; track s) { <option [value]="s">{{ s }}</option> }
            </select>
          </div>
          <div class="field"><label>PMDC license no.</label><input class="input" name="lic" [(ngModel)]="doc.licenseNumber" required></div>
          <div class="field"><label>Phone</label><input class="input" name="ph" [(ngModel)]="doc.phoneNumber" placeholder="03001234567" required></div>
          <div class="field"><label>Consultation fee (PKR)</label><input class="input" type="number" name="fee" [(ngModel)]="doc.consultationFee" min="0" required></div>
        </div>
      } @else {
        <div class="grid-2">
          <div class="field"><label>Phone</label><input class="input" name="ph" [(ngModel)]="pat.phoneNumber" placeholder="03001234567" required></div>
          <div class="field"><label>Date of birth</label><input class="input" type="date" name="dob" [(ngModel)]="pat.dateOfBirth" required></div>
          <div class="field"><label>Gender</label>
            <select name="g" [(ngModel)]="pat.gender" required>
              <option>Male</option><option>Female</option><option>Other</option>
            </select>
          </div>
          <div class="field"><label>Blood group</label>
            <select name="bg" [(ngModel)]="pat.bloodGroup" required>
              @for (b of bloodGroups; track b) { <option [value]="b">{{ b }}</option> }
            </select>
          </div>
        </div>
        <div class="field"><label>Address</label><input class="input" name="addr" [(ngModel)]="pat.address" required></div>
      }
      <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : (create() ? 'Save & continue' : 'Save changes') }}</button>
    </form>
  `,
})
export class ProfileForm implements OnInit {
  auth = inject(AuthService);
  private api = inject(ApiService);
  create = input(false);
  saved = output<void>();

  specializations = ['General Medicine', 'Cardiology', 'Dermatology', 'Pediatrics', 'Orthopedics', 'Neurology', 'ENT', 'Gynecology', 'Psychiatry', 'Ophthalmology'];
  bloodGroups = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  doc = { specialization: 'General Medicine', licenseNumber: '', phoneNumber: '', consultationFee: 1500 };
  pat = { phoneNumber: '', dateOfBirth: '', gender: 'Female', bloodGroup: 'B+', address: '' };
  saving = signal(false);
  error = signal('');

  async ngOnInit() {
    if (this.create()) return;
    try {
      if (this.auth.role() === 'Doctor') {
        const d = await this.api.myDoctorProfile();
        this.doc = { specialization: d.specialization, licenseNumber: d.licenseNumber, phoneNumber: d.phoneNumber, consultationFee: d.consultationFee };
      } else {
        const p = await this.api.myPatientProfile();
        this.pat = { phoneNumber: p.phoneNumber, dateOfBirth: p.dateOfBirth.slice(0, 10), gender: p.gender, bloodGroup: p.bloodGroup, address: p.address };
      }
    } catch (e) { this.error.set(errorMessage(e)); }
  }

  async save() {
    this.saving.set(true);
    this.error.set('');
    try {
      if (this.auth.role() === 'Doctor') await this.api.saveDoctorProfile(this.doc, this.create());
      else await this.api.savePatientProfile({ ...this.pat, dateOfBirth: this.pat.dateOfBirth + 'T00:00:00Z' }, this.create());
      this.auth.hasProfile.set(true);
      this.saved.emit();
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
}
