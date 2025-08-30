import { ExtractedDocumentData } from './extracted-document-data';

export interface DocumentUploadResponse {
  documentId: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  status: string;
  inputDocumentType: string;
  classifiedDocumentType: string;
  extractedData?: ExtractedDocumentData;
  message: string;
}
