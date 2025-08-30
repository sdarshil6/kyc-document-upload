import { Component } from '@angular/core';
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
export class DocumentUploadComponent {
  uploadForm: FormGroup;
  loading = false;
  responseMessage: string | null = null;
  isSuccess = false;
  uploadResponse: DocumentUploadResponse | null = null;
  documentTypes: DocumentType[] = DOCUMENT_TYPES;

  constructor(private fb: FormBuilder, private uploadService: DocumentService) {
    this.uploadForm = this.fb.group({
      file: [null, Validators.required],
      documentType: ['', [Validators.required, Validators.pattern('^[0-9]+$')]],
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];

    const allowedTypes = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp'];
    if (!allowedTypes.includes(file.type) || file.size > 10 * 1024 * 1024) {
      this.uploadForm.patchValue({ file: null });
      this.uploadForm.get('file')?.setErrors({ invalid: true });
      return;
    }

    this.uploadForm.patchValue({ file });
    this.uploadForm.get('file')?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.uploadForm.invalid) {
      return;
    }

    this.loading = true;
    this.responseMessage = null;

    const documentTypeValue = parseInt(
      this.uploadForm.get('documentType')?.value,
      10
    );

    if (isNaN(documentTypeValue)) {
      this.uploadForm.get('documentType')?.setErrors({ invalid: true });
      this.loading = false;
      return;
    }

    const documentUploadRequest: DocumentUploadRequest = {
      file: this.uploadForm.get('file')?.value,
      documentType: documentTypeValue,
    };

    this.uploadService.uploadDocument(documentUploadRequest).subscribe({
      next: (res) => {
        this.loading = false;
        this.isSuccess = true;
        this.responseMessage =
          res.message || 'Your document has been uploaded successfully!';
        this.uploadResponse = res.data;
        console.log('Success Response:', this.uploadResponse);
      },
      error: (err) => {
        this.loading = false;
        this.isSuccess = false;
        this.responseMessage =
          err?.error?.message ||
          'Something went wrong. Please try again later.';
        this.uploadResponse = null;
        console.log('Failure Response:', this.uploadResponse);
      },
    });
  }
}
