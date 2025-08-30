export interface ExtractedDocumentData {
  fullName?: string;
  dateOfBirth?: Date;
  gender?: string;
  aadhaarNumber?: string;
  panNumber?: string;
  passportNumber?: string;
  address?: string;
  city?: string;
  state?: string;
  pinCode?: string;
  extractionConfidence: number;
}
