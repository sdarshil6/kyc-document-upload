import { Component, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { DocumentService } from '../../../../core/services/document.service';
import { DocumentUploadRequest } from '../../../../core/models/document-upload-request';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { DocumentUploadResponse } from '../../../../core/models/document-upload-response';
import { DocumentType } from '../../../../core/models/document-type';
import { DOCUMENT_TYPES } from '../../../../core/constants/document-types';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './document-upload.component.html',
  styleUrls: ['./document-upload.component.css'],
})
export class DocumentUploadComponent implements OnDestroy {
  uploadForm: FormGroup;
  loading = false;
  responseMessage: string | null = null;
  isSuccess = false;
  uploadResponse: DocumentUploadResponse | null = null;
  documentTypes: DocumentType[] = DOCUMENT_TYPES;

  // File preview and drag & drop properties
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  isDragOver = false;

  // File validation constants
  private readonly MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
  private readonly ALLOWED_TYPES = [
    'image/png',
    'image/jpeg',
    'image/jpg',
    'image/webp',
  ];

  constructor(
    private fb: FormBuilder,
    private uploadService: DocumentService,
    private cdr: ChangeDetectorRef
  ) {
    // Only include documentType in form - file is handled separately
    this.uploadForm = this.fb.group({
      documentType: ['', [Validators.required, Validators.pattern('^[0-9]+$')]],
    });
  }

  ngOnDestroy(): void {
    // Clean up file preview URL to prevent memory leaks
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
    }
  }

  // Drag and Drop Event Handlers
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      const file = files[0];
      this.processSelectedFile(file);
    }
  }

  // File selection handler
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.clearFilePreview();
      return;
    }

    const file = input.files[0];

    this.processSelectedFile(file);
  }

  // Process selected file with validation and preview
  private processSelectedFile(file: File): void {
    // Validate file type
    if (!this.ALLOWED_TYPES.includes(file.type)) {
      this.clearFilePreview();
      this.resetFileInput();
      return;
    }

    // Validate file size
    if (file.size > this.MAX_FILE_SIZE) {
      this.clearFilePreview();
      this.resetFileInput();
      return;
    }

    // File is valid - store it and create preview
    this.selectedFile = file;
    this.createFilePreview(file);

    // Force change detection to update the view
    this.cdr.detectChanges();
  }

  // Create file preview using blob URL
  private createFilePreview(file: File): void {
    // Clean up previous preview URL
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
      this.filePreviewUrl = null;
    }

    try {
      // Ensure we have a valid file
      if (!file) {
        return;
      }

      // Create new preview URL
      this.filePreviewUrl = URL.createObjectURL(file);

      // Force change detection
      this.cdr.detectChanges();
    } catch (error) {
      this.filePreviewUrl = null;
    }
  }

  // Remove selected file and clear preview
  removeSelectedFile(): void {
    this.selectedFile = null;
    this.resetForm();
  }

  // Clear file preview and URL
  private clearFilePreview(): void {
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
      this.filePreviewUrl = null;
    }
    this.selectedFile = null;
  }

  // Reset the file input element
  private resetFileInput(): void {
    const fileInput = document.getElementById('fileInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  // Format file size for display
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // Handle image load errors
  onImageError(event: any): void {}

  // Check if entire form (including file) is valid
  isFormValid(): boolean {
    return this.uploadForm.valid && this.selectedFile !== null;
  }

  // Enhanced submit handler
  onSubmit(): void {
    // Check if file is selected
    if (!this.selectedFile) {
      return;
    }

    // Check if form is valid
    if (this.uploadForm.invalid) {
      this.uploadForm.markAllAsTouched();

      return;
    }

    this.loading = true;
    this.responseMessage = null;

    const documentTypeValue = parseInt(
      this.uploadForm.get('documentType')?.value,
      10
    );

    if (isNaN(documentTypeValue)) {
      this.loading = false;

      return;
    }

    // Create upload request with selected file
    const documentUploadRequest: DocumentUploadRequest = {
      file: this.selectedFile,
      documentType: documentTypeValue,
    };

    this.uploadService.uploadDocument(documentUploadRequest).subscribe({
      next: (res) => {
        this.loading = false;
        this.isSuccess = true;
        this.responseMessage =
          res.message || 'Your document has been uploaded successfully!';
        this.uploadResponse = res.data;
      },
      error: (err) => {
        this.loading = false;
        this.isSuccess = false;
        this.responseMessage =
          err?.error?.message ||
          'Something went wrong. Please try again later.';
        this.uploadResponse = null;
      },
    });
  }

  // Reset entire form and clear all data
  resetForm(): void {
    this.uploadForm.reset({ documentType: '' });
    this.clearFilePreview();
    this.resetFileInput();
    this.responseMessage = null;
    this.uploadResponse = null;
    this.isSuccess = false;
    this.loading = false;
  }
}
