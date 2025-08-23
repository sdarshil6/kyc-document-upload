import easyocr
import cv2
import numpy as np
import os
from typing import List, Dict, Any


class EasyOCRProcessor:
    def __init__(self, languages: List[str] = None, use_gpu: bool = True):
        """
        :param languages: List of languages to use (default: ['en', 'hi'])
        :param use_gpu: Use GPU if available
        """
        if languages is None:
            languages = ['en', 'hi']

        self.reader = easyocr.Reader(languages, gpu=use_gpu)

    def preprocess_image(self, image_path: str) -> str:
        """Preprocess image for better OCR: denoise, deskew, scale up, binarize."""
        image = cv2.imread(image_path)
        if image is None:
            raise ValueError(f"Could not read image: {image_path}")

        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        # Denoise
        denoised = cv2.fastNlMeansDenoising(gray, h=30)

        # Scale up small text
        scaled = cv2.resize(denoised, None, fx=2, fy=2,
                            interpolation=cv2.INTER_CUBIC)

        # Threshold (binarization)
        _, thresh = cv2.threshold(
            scaled, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

        # Deskew
        coords = np.column_stack(np.where(thresh > 0))
        angle = cv2.minAreaRect(coords)[-1]
        if angle < -45:
            angle = -(90 + angle)
        else:
            angle = -angle
        (h, w) = thresh.shape
        M = cv2.getRotationMatrix2D((w // 2, h // 2), angle, 1.0)
        deskewed = cv2.warpAffine(
            thresh, M, (w, h), flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)

        # Save temporary preprocessed file
        preprocessed_path = image_path.replace('.', '_preprocessed.')
        cv2.imwrite(preprocessed_path, deskewed)
        return preprocessed_path

    def clean_text(self, text: str) -> str:
        """Clean up OCR text without corrupting numbers."""
        text = ' '.join(text.split())
        # Replace 0→O only when surrounded by letters
        import re
        text = re.sub(r'(?<=\D)0(?=\D)', 'O', text)
        # Fix common OCR issues
        text = text.replace('|', 'I')
        return text.strip()

    def extract_text(self, image_path: str, options: Dict[str, Any] = None) -> Dict[str, Any]:
        if options is None:
            options = {}

        processed_image_path = self.preprocess_image(image_path)

        extract_word_details = options.get('extract_word_details', True)
        confidence_threshold = options.get(
            'confidence_threshold', 0.3)  # Lower threshold
        paragraph_mode = options.get('paragraph_mode', True)

        results = self.reader.readtext(
            processed_image_path,
            detail=extract_word_details,
            paragraph=paragraph_mode,
            width_ths=0.7,
            height_ths=0.7,
            add_margin=0.2
        )

        extracted_text = ""
        word_details = []

        if extract_word_details:
            for detection in results:
                bbox, text, confidence = detection
                text = self.clean_text(text)

                if not text.strip():
                    continue

                word_details.append({
                    "text": text,
                    "confidence": float(confidence),
                    "bounding_box": bbox,
                    "low_confidence": confidence < confidence_threshold
                })

                extracted_text += text + " "
        else:
            extracted_text = " ".join([self.clean_text(text)
                                      for (_, text, _) in results])

        extracted_text = extracted_text.strip()

        return {
            "engine": "easyocr",
            "success": True,
            "extracted_text": extracted_text,
            "confidence": float(np.mean([c for (_, _, c) in results]) if results else 0),
            "word_details": word_details
        }
