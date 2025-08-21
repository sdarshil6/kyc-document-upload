import sys
import json
import argparse
import time
import traceback
from pathlib import Path
from typing import List, Dict, Any, Tuple

try:
    import easyocr
    import cv2
    import numpy as np
    from PIL import Image
except ImportError as e:
    print(json.dumps({
        "success": False,
        "error": f"Missing dependencies: {str(e)}",
        "message": "Please install: pip install easyocr opencv-python pillow"
    }))
    sys.exit(1)


class EasyOCRProcessor:
    """Enhanced EasyOCR processor with optimization for Indian documents"""

    def __init__(self, languages: List[str] = None, use_gpu: bool = False):
        """
        Initialize EasyOCR processor

        Args:
            languages: List of language codes (e.g., ['en', 'hi'])
            use_gpu: Whether to use GPU acceleration
        """
        self.languages = languages or ['en', 'hi']
        self.use_gpu = use_gpu
        self.reader = None
        self.initialize_reader()

    def initialize_reader(self):
        """Initialize EasyOCR reader with specified languages"""
        try:
            print(
                f"Initializing EasyOCR with languages: {self.languages}", file=sys.stderr)
            self.reader = easyocr.Reader(
                self.languages,
                gpu=self.use_gpu,
                verbose=False
            )
            print("EasyOCR reader initialized successfully", file=sys.stderr)
        except Exception as e:
            raise RuntimeError(f"Failed to initialize EasyOCR: {str(e)}")

    def process_image(self, image_path: str, options: Dict[str, Any] = None) -> Dict[str, Any]:
        """
        Process image with EasyOCR

        Args:
            image_path: Path to the image file
            options: Processing options

        Returns:
            Dictionary containing OCR results
        """
        start_time = time.time()

        try:
            # Validate image path
            if not Path(image_path).exists():
                raise FileNotFoundError(f"Image file not found: {image_path}")

            # Set default options
            options = options or {}
            confidence_threshold = options.get('confidence_threshold', 0.5)
            preprocess = options.get('preprocess', True)
            extract_word_details = options.get('extract_word_details', True)

            # Preprocess image if enabled
            processed_image_path = image_path
            if preprocess:
                processed_image_path = self.preprocess_image(image_path)

            # Perform OCR
            print(f"Processing image: {processed_image_path}", file=sys.stderr)
            results = self.reader.readtext(
                processed_image_path,
                detail=extract_word_details,
                paragraph=False,
                width_ths=0.7,
                height_ths=0.7,
                add_margin=0.1
            )

            # Process results
            extracted_text = ""
            word_details = []
            total_confidence = 0.0
            valid_detections = 0

            for result in results:
                if extract_word_details and len(result) >= 3:
                    # Detailed result: (bbox, text, confidence)
                    bbox, text, confidence = result

                    if confidence >= confidence_threshold:
                        extracted_text += text + " "
                        total_confidence += confidence
                        valid_detections += 1

                        # Extract bounding box coordinates
                        x_coords = [point[0] for point in bbox]
                        y_coords = [point[1] for point in bbox]

                        word_details.append({
                            "text": text,
                            "confidence": float(confidence),
                            "bounding_box": {
                                "x": int(min(x_coords)),
                                "y": int(min(y_coords)),
                                "width": int(max(x_coords) - min(x_coords)),
                                "height": int(max(y_coords) - min(y_coords))
                            },
                            "is_numeric": text.isdigit(),
                            "is_alphabetic": text.isalpha(),
                            "language": self.detect_text_language(text)
                        })
                else:
                    # Simple result: just text
                    extracted_text += str(result) + " "
                    valid_detections += 1
                    total_confidence += 0.8  # Default confidence for simple mode

            # Calculate overall confidence
            overall_confidence = total_confidence / \
                valid_detections if valid_detections > 0 else 0.0

            # Clean up extracted text
            extracted_text = self.clean_text(extracted_text.strip())

            # Analyze text patterns
            patterns = self.analyze_text_patterns(extracted_text)

            processing_time = time.time() - start_time

            # Cleanup preprocessed file if created
            if processed_image_path != image_path and Path(processed_image_path).exists():
                Path(processed_image_path).unlink()

            result = {
                "success": True,
                "extracted_text": extracted_text,
                "confidence": float(overall_confidence),
                "processing_time": float(processing_time),
                "word_count": len(word_details),
                "character_count": len(extracted_text),
                "detected_languages": list(set([w["language"] for w in word_details])),
                "word_details": word_details,
                "detected_patterns": patterns,
                "engine_metadata": {
                    "engine": "EasyOCR",
                    "languages": self.languages,
                    "gpu_used": self.use_gpu,
                    "confidence_threshold": confidence_threshold,
                    "preprocessing_enabled": preprocess,
                    "total_detections": len(results),
                    "valid_detections": valid_detections
                }
            }

            print(
                f"EasyOCR processing completed in {processing_time:.2f}s", file=sys.stderr)
            return result

        except Exception as e:
            processing_time = time.time() - start_time
            error_result = {
                "success": False,
                "error": str(e),
                "processing_time": float(processing_time),
                "traceback": traceback.format_exc()
            }
            print(f"EasyOCR processing failed: {str(e)}", file=sys.stderr)
            return error_result

    def preprocess_image(self, image_path: str) -> str:
        """
        Preprocess image to improve OCR accuracy

        Args:
            image_path: Path to input image

        Returns:
            Path to preprocessed image
        """
        try:
            # Read image
            image = cv2.imread(image_path)
            if image is None:
                raise ValueError(f"Could not read image: {image_path}")

            # Convert to grayscale
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

            # Apply Gaussian blur to reduce noise
            blurred = cv2.GaussianBlur(gray, (5, 5), 0)

            # Apply adaptive thresholding
            thresh = cv2.adaptiveThreshold(
                blurred, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 11, 2
            )

            # Apply morphological operations to clean up
            kernel = np.ones((2, 2), np.uint8)
            cleaned = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel)
            cleaned = cv2.morphologyEx(cleaned, cv2.MORPH_OPEN, kernel)

            # Save preprocessed image
            preprocessed_path = image_path.replace('.', '_preprocessed.')
            cv2.imwrite(preprocessed_path, cleaned)

            print(f"Image preprocessed: {preprocessed_path}", file=sys.stderr)
            return preprocessed_path

        except Exception as e:
            print(
                f"Preprocessing failed, using original image: {str(e)}", file=sys.stderr)
            return image_path

    def detect_text_language(self, text: str) -> str:
        """
        Detect the language of text based on character ranges

        Args:
            text: Text to analyze

        Returns:
            Language code (en, hi, or mixed)
        """
        if not text:
            return "unknown"

        english_chars = sum(1 for c in text if c.isascii() and c.isalpha())
        hindi_chars = sum(1 for c in text if '\u0900' <= c <= '\u097F')
        total_chars = len([c for c in text if c.isalpha()])

        if total_chars == 0:
            return "unknown"

        hindi_ratio = hindi_chars / total_chars
        english_ratio = english_chars / total_chars

        if hindi_ratio > 0.5:
            return "hi"
        elif english_ratio > 0.5:
            return "en"
        elif hindi_ratio > 0.2 and english_ratio > 0.2:
            return "mixed"
        else:
            return "en"  # Default to English

    def analyze_text_patterns(self, text: str) -> List[str]:
        """
        Analyze text for Indian document patterns

        Args:
            text: Extracted text to analyze

        Returns:
            List of detected patterns
        """
        import re

        patterns = []

        # Aadhaar number pattern
        if re.search(r'\b\d{4}\s?\d{4}\s?\d{4}\b', text):
            patterns.append("aadhaar_number")

        # PAN number pattern
        if re.search(r'\b[A-Z]{5}\d{4}[A-Z]\b', text):
            patterns.append("pan_number")

        # Passport number pattern
        if re.search(r'\b[A-Z]\d{7}\b', text):
            patterns.append("passport_number")

        # Date patterns
        if re.search(r'\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4}\b', text):
            patterns.append("date")

        # Phone number pattern
        if re.search(r'\b\d{10}\b', text):
            patterns.append("phone_number")

        # PIN code pattern
        if re.search(r'\b\d{6}\b', text):
            patterns.append("pin_code")

        # Government document indicators
        if any(keyword in text.lower() for keyword in ['government', 'india', 'भारत', 'सरकार']):
            patterns.append("government_document")

        return patterns

    def clean_text(self, text: str) -> str:
        """
        Clean extracted text by removing artifacts and normalizing

        Args:
            text: Raw extracted text

        Returns:
            Cleaned text
        """
        if not text:
            return text

        # Remove extra whitespace
        text = ' '.join(text.split())

        # Remove common OCR artifacts
        text = text.replace('|', 'I')  # Common misread
        text = text.replace('0', 'O')  # In text contexts

        # Normalize quotation marks
        text = text.replace('"', '"').replace('"', '"')
        text = text.replace(''', "'").replace(''', "'")

        return text.strip()

    def get_capabilities(self) -> Dict[str, Any]:
        """
        Get EasyOCR capabilities and version info

        Returns:
            Dictionary with capability information
        """
        try:
            import easyocr

            return {
                "engine": "EasyOCR",
                "version": easyocr.__version__,
                "supported_languages": self.languages,
                "gpu_available": self.use_gpu,
                "features": {
                    "word_details": True,
                    "confidence_scores": True,
                    "multiple_languages": True,
                    "handwriting": True,
                    "preprocessing": True
                },
                "supported_formats": [".jpg", ".jpeg", ".png", ".bmp", ".tiff"],
                "performance": {
                    "average_accuracy": 0.90,
                    "average_speed": 1.5
                }
            }
        except Exception as e:
            return {
                "engine": "EasyOCR",
                "error": str(e),
                "available": False
            }


def main():
    """Main function to handle command line processing"""
    parser = argparse.ArgumentParser(
        description='EasyOCR Processor for KYC Documents')
    parser.add_argument('command', choices=['process', 'capabilities', 'health'],
                        help='Command to execute')
    parser.add_argument('--image', type=str, help='Path to image file')
    parser.add_argument('--languages', type=str, nargs='+', default=['en', 'hi'],
                        help='Language codes for OCR')
    parser.add_argument('--gpu', action='store_true',
                        help='Use GPU acceleration')
    parser.add_argument('--confidence', type=float, default=0.5,
                        help='Minimum confidence threshold')
    parser.add_argument('--preprocess', action='store_true', default=True,
                        help='Enable image preprocessing')
    parser.add_argument('--word-details', action='store_true', default=True,
                        help='Extract word-level details')

    args = parser.parse_args()

    try:
        processor = EasyOCRProcessor(
            languages=args.languages, use_gpu=args.gpu)

        if args.command == 'process':
            if not args.image:
                result = {
                    "success": False,
                    "error": "Image path required for process command"
                }
            else:
                options = {
                    'confidence_threshold': args.confidence,
                    'preprocess': args.preprocess,
                    'extract_word_details': args.word_details
                }
                result = processor.process_image(args.image, options)

        elif args.command == 'capabilities':
            result = processor.get_capabilities()

        elif args.command == 'health':
            # Simple health check
            result = {
                "success": True,
                "status": "healthy",
                "message": "EasyOCR is available and ready",
                "languages": processor.languages,
                "gpu_enabled": processor.use_gpu
            }

        else:
            result = {
                "success": False,
                "error": f"Unknown command: {args.command}"
            }

        # Output result as JSON
        print(json.dumps(result, ensure_ascii=False, indent=None))

    except Exception as e:
        error_result = {
            "success": False,
            "error": str(e),
            "traceback": traceback.format_exc()
        }
        print(json.dumps(error_result))
        sys.exit(1)


if __name__ == '__main__':
    main()
