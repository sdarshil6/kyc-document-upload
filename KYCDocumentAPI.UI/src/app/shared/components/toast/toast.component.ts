import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container position-fixed bottom-0 end-0 p-3">
      <div
        class="toast align-items-center text-bg-{{ type }} border-0 show"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
      >
        <div class="d-flex">
          <div class="toast-body">
            {{ message }}
          </div>
          <button
            type="button"
            class="btn-close btn-close-white me-2 m-auto"
            (click)="close()"
          ></button>
        </div>
      </div>
    </div>
  `,
})
export class ToastComponent implements OnChanges {
  @Input() message: string = '';
  @Input() type: 'success' | 'danger' | 'info' = 'info';

  private timeout: any;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['message'] && this.message) {
      clearTimeout(this.timeout);
      this.timeout = setTimeout(() => this.close(), 4000);
    }
  }

  close() {
    this.message = '';
  }
}
