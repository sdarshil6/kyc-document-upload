import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DocumentUploadComponent } from './features/document-upload/components/document-upload/document-upload.component';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, DocumentUploadComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'kyc-document-api-frontend';
}
