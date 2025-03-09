import { Component, effect, inject } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { MessageService } from './message.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgClass, FormsModule, NgIf],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  private readonly messageService = inject(MessageService);
  readonly messages = this.messageService.messages;
  readonly generatingInProgress = this.messageService.generatingInProgress;
  orgId = 'property';
  showConfigModal = false;

  private readonly scrollOnMessageChanges = effect(() => {
    // run this effect on every messages change
    this.messages();

    // scroll after the messages render
    setTimeout(() =>
      window.scrollTo({
        top: document.body.scrollHeight,
        behavior: 'smooth',
      }),
    );
  });

  sendMessage(form: NgForm, messageText: string): void {
    this.messageService.sendMessage(messageText, this.orgId);
    form.resetForm();
  }

  toggleConfigModal(): void {
    this.showConfigModal = !this.showConfigModal;
  }

  saveOrgId(form: NgForm): void {
    if (form.valid) {
      this.orgId = form.value.orgId;
      this.showConfigModal = false;
    }
  }

  closeModal(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.showConfigModal = false;
    }
  }
}
