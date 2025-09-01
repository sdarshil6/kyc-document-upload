import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DocumentUploadRequest } from '../models/document-upload-request';
import { DocumentUploadResponse } from '../models/document-upload-response';
import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root',
})
export class DocumentService {
  private baseUrl = 'http://kyc-document-upload.runasp.net';

  constructor(private http: HttpClient) {}

  uploadDocument(request: DocumentUploadRequest): Observable<any> {
    const formData = new FormData();
    formData.append('File', request.file);
    formData.append('DocumentType', request.documentType.toString());

    return this.http
      .post<any>(`${this.baseUrl}${API_ENDPOINTS.uploadDocument}`, formData, {
        headers: new HttpHeaders({}),
      })
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    let errorMsg = 'Something went wrong!';
    if (error.error instanceof ErrorEvent) {
      errorMsg = `Error: ${error.error.message}`;
    } else {
      errorMsg = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    return throwError(() => errorMsg);
  }
}
