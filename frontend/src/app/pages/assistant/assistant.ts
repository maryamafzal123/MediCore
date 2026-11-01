import { AfterViewChecked, Component, ElementRef, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { marked } from 'marked';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { errorMessage } from '../../core/auth.interceptor';
import { ChatMessage, Conversation } from '../../core/models';

// Friendly labels for the tools the agent used (shown under the reply)
const TOOL_LABELS: Record<string, string> = {
  search_hospital_info: 'Hospital handbook', list_doctors: 'Doctor list', check_doctor_availability: 'Doctor availability',
  get_my_appointments: 'Your appointments', book_appointment: 'Booked appointment', cancel_appointment: 'Cancelled appointment',
  get_my_prescriptions: 'Your prescriptions', get_my_profile: 'Your profile', get_my_schedule: 'Your schedule',
  update_appointment_status: 'Updated status', get_patient_details: 'Patient profile', get_prescriptions_i_wrote: 'Prescriptions',
  get_hospital_stats: 'Hospital stats', search_appointments: 'Appointments', list_patients: 'Patient list',
};

interface UiMessage extends ChatMessage { tools?: string[]; }

@Component({
  selector: 'app-assistant',
  imports: [FormsModule],
  templateUrl: './assistant.html',
  styleUrl: './assistant.css',
})
export class Assistant implements OnInit, AfterViewChecked {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  auth = inject(AuthService);

  q = input<string>();                      // ?q=... from dashboard suggestion chips
  scroller = viewChild<ElementRef<HTMLDivElement>>('scroller');

  conversations = signal<Conversation[]>([]);
  activeId = signal<number | null>(null);
  messages = signal<UiMessage[]>([]);
  draft = '';
  thinking = signal(false);
  showList = signal(false);
  private shouldScroll = false;

  activeTitle = computed(() => this.conversations().find(c => c.id === this.activeId())?.title ?? 'New chat');

  starters = computed(() => ({
    Patient: ['Find me a skin specialist and show free slots for tomorrow', 'Mera agla appointment kab hai?', 'What is the cancellation policy?', 'Kya hospital Sunday ko khula hai?'],
    Doctor: ["What's my schedule for today?", 'Show my upcoming appointments this week', 'Prescriptions I wrote recently', 'What are the hospital visiting hours?'],
    Admin: ['Summarize hospital stats', 'Show scheduled appointments for tomorrow', 'Which specialization has the most doctors?', 'List patients named Ali'],
  })[this.auth.role()!]);

  async ngOnInit() {
    await this.loadConversations();
    const first = this.conversations()[0];
    if (first) await this.open(first.id);
    const q = this.q();
    if (q) {
      this.router.navigate([], { queryParams: {}, replaceUrl: true });
      await this.newChat();
      this.send(q);
    }
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.scroller()) {
      const el = this.scroller()!.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  async loadConversations() {
    try { this.conversations.set(await this.api.conversations()); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  async open(id: number) {
    this.activeId.set(id);
    this.showList.set(false);
    try {
      this.messages.set(await this.api.messages(id));
      this.shouldScroll = true;
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  async newChat() {
    // reuse an empty "New chat" instead of creating many
    if (this.activeId() && !this.messages().length) return;
    try {
      const c = await this.api.newConversation();
      this.conversations.update(list => [c, ...list]);
      this.activeId.set(c.id);
      this.messages.set([]);
      this.showList.set(false);
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  async remove(c: Conversation, ev: Event) {
    ev.stopPropagation();
    if (!confirm(`Delete "${c.title}"?`)) return;
    try {
      await this.api.deleteConversation(c.id);
      this.conversations.update(list => list.filter(x => x.id !== c.id));
      if (this.activeId() === c.id) {
        this.activeId.set(null);
        this.messages.set([]);
      }
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  async send(text?: string) {
    const message = (text ?? this.draft).trim();
    if (!message || this.thinking()) return;
    if (!this.activeId()) await this.newChat();
    const id = this.activeId()!;
    this.draft = '';

    // show the user's message immediately (optimistic UI)
    const temp: UiMessage = { id: -Date.now(), role: 'user', content: message, createdAt: new Date().toISOString() };
    this.messages.update(m => [...m, temp]);
    this.thinking.set(true);
    this.shouldScroll = true;

    try {
      const r = await this.api.sendMessage(id, message);
      this.messages.update(m => [...m.filter(x => x.id !== temp.id), r.userMessage, { ...r.assistantMessage, tools: r.toolsUsed }]);
      this.conversations.update(list => [
        { id, title: r.title, updatedAt: new Date().toISOString() },
        ...list.filter(c => c.id !== id),
      ]);
    } catch (e) {
      this.messages.update(m => m.filter(x => x.id !== temp.id));
      this.draft = message;
      this.toast.error(errorMessage(e));
    } finally {
      this.thinking.set(false);
      this.shouldScroll = true;
    }
  }

  onKey(ev: KeyboardEvent) {
    if (ev.key === 'Enter' && !ev.shiftKey) {
      ev.preventDefault();
      this.send();
    }
  }

  render(md: string) { return marked.parse(md, { async: false, breaks: true }) as string; }
  toolLabel(t: string) { return TOOL_LABELS[t] ?? t; }
  isUrdu(text: string) { return /[؀-ۿ]/.test(text); }
}
