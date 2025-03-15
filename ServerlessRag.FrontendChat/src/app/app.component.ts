import { Component, effect, inject } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { MessageService } from './message.service';
import { DocumentUploadService } from './document-upload.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgClass, FormsModule, NgIf],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  private readonly messageService = inject(MessageService);
  private readonly documentUploadService = inject(DocumentUploadService);
  readonly messages = this.messageService.messages;
  readonly generatingInProgress = this.messageService.generatingInProgress;
  orgId = 'property';
  showConfigModal = false;

  // New properties for file upload
  showUploadModal = false;
  selectedFile: File | null = null;
  uploadOrgId = '';
  fileToUpload: File | null = null;
  isUploading = false; // New property for loading state
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
      this.showUploadModal = false;
    }
  }

  // New methods for file upload functionality
  toggleUploadModal(): void {
    this.showUploadModal = !this.showUploadModal;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.selectedFile = input.files[0];
    }
  }

  uploadFile(): void {
    if (this.selectedFile && this.uploadOrgId) {
      this.isUploading = true; // Set loading state to true
      this.documentUploadService.uploadDocument(this.selectedFile, this.uploadOrgId)
      .subscribe({
        next: (result) => {
            console.log('Success:', result)
            this.showUploadModal = false;
            this.selectedFile = null;
            this.isUploading = false; // Set loading state to false
          },
        error: (error) => {
            console.error('Error:', error);
            this.isUploading = false; // Set loading state to false
          }
      });
    }
  }
}
