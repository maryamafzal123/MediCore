import { Injectable, signal } from '@angular/core';

export interface Toast { id: number; text: string; kind: 'success' | 'error'; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 1;

  success(text: string) { this.push(text, 'success'); }
  error(text: string) { this.push(text, 'error'); }

  private push(text: string, kind: Toast['kind']) {
    const id = this.nextId++;
    this.toasts.update(t => [...t, { id, text, kind }]);
    setTimeout(() => this.toasts.update(t => t.filter(x => x.id !== id)), 4000);
  }
}
