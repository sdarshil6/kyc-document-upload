namespace KYCDocumentAPI.ML.Services
{
    //public class MockOCRService : IOCRService
    //{
        //private readonly ILogger<MockOCRService> _logger;
        //private readonly Random _random = new();

        //// Mock text samples for different document types
        //private readonly Dictionary<DocumentType, string[]> _mockTexts = new()
        //{
        //    [DocumentType.Aadhaar] = new[]
        //    {
        //        "Government of India\nUnique Identification Authority of India\nआधार\n\nJohn Doe\nजॉन डो\nMale/पुरुष\nDOB: 15/01/1990\nयूआईडी: 1234 5678 9012\n\nAddress:\n123 Sample Street\nMumbai, Maharashtra\nPIN: 400001",
        //        "भारत सरकार\nआधार\nUID: 9876 5432 1098\nName: Priya Sharma\nप्रिया शर्मा\nFemale/महिला\nजन्म तिथि: 22/08/1985\nAddress: 456 Test Road, Delhi 110001"
        //    },
        //    [DocumentType.PAN] = new[]
        //    {
        //        "INCOME TAX DEPARTMENT\nGOVT. OF INDIA\nPermanent Account Number Card\n\nJOHN DOE\nजॉन डो\nPAN: ABCDE1234F\nDOB: 15/01/1990\nFather's Name: ROBERT DOE\nSignature",
        //        "आयकर विभाग\nभारत सरकार\nस्थायी खाता संख्या कार्ड\nPRIYA SHARMA\nप्रिया शर्मा\nPAN: BCDEA5678G\nजन्म दिनांक: 22/08/1985"
        //    },
        //    [DocumentType.Passport] = new[]
        //    {
        //        "REPUBLIC OF INDIA\nपासपोर्ट\nPASSPORT\n\nType/प्रकार: P\nCountry Code/देश कोड: IND\nPassport No./पासपोर्ट संख्या: A1234567\n\nSurname/उपनाम: DOE\nGiven Name(s)/नाम: JOHN\nNationality/राष्ट्रीयता: INDIAN\nDate of Birth/जन्म तिथि: 15 JAN/जन 1990\nPlace of Birth/जन्म स्थान: MUMBAI\nDate of Issue/जारी करने की तिथि: 01 JAN/जन 2020\nDate of Expiry/समाप्ति तिथि: 31 DEC/दिस 2029",
        //        "भारत गणराज्य\nREPUBLIC OF INDIA\nPASSPORT\nPassport No.: B9876543\nName: PRIYA SHARMA\nNationality: INDIAN\nDate of Birth: 22 AUG 1985\nPlace of Issue: NEW DELHI"
        //    },
        //    [DocumentType.DrivingLicense] = new[]
        //    {
        //        "DRIVING LICENCE\nGOVERNMENT OF MAHARASHTRA\nTransport Department\n\nLicence No: MH02 20110012345\nName: JOHN DOE\nS/W/D of: ROBERT DOE\nDOB: 15-01-1990\nBG: B+\nAddress: 123 Sample Street, Mumbai\nClass of Vehicle: LMV\nValid Till: 14-01-2030",
        //        "ड्राइविंग लाइसेंस\nदिल्ली सरकार\nपरिवहन विभाग\nDL: DL-1320110067890\nनाम: PRIYA SHARMA\nजन्म तिथि: 22/08/1985\nवैधता: 21/08/2030"
        //    }
        //};

        //public MockOCRService(ILogger<MockOCRService> logger)
        //{
        //    _logger = logger;
        //}

        //public async Task<OCRResult> ExtractTextFromImageAsync(string imagePath)
        //{
        //    var stopwatch = Stopwatch.StartNew();

        //    try
        //    {
        //        // Simulate processing time
        //        await Task.Delay(_random.Next(500, 2000));

        //        // Determine document type from filename for better mock data
        //        var documentType = GuessDocumentTypeFromPath(imagePath);
        //        var mockText = GetMockTextForDocumentType(documentType);
        //        var confidence = 0.75f + (_random.NextSingle() * 0.2f); // 75-95% confidence

        //        stopwatch.Stop();

        //        var result = new OCRResult
        //        {
        //            Success = true,
        //            ExtractedText = mockText,
        //            Confidence = confidence,
        //            DetectedLanguages = new List<string> { "en", "hi" },
        //            ProcessingTime = stopwatch.Elapsed,
        //            Metadata = new Dictionary<string, object>
        //            {
        //                { "ImagePath", imagePath },
        //                { "DetectedDocumentType", documentType.ToString() },
        //                { "ProcessingMethod", "Mock OCR" },
        //                { "CharacterCount", mockText.Length }
        //            }
        //        };

        //        _logger.LogInformation("Mock OCR completed for {ImagePath} in {ProcessingTime}ms with {Confidence}% confidence",
        //            imagePath, stopwatch.ElapsedMilliseconds, Math.Round(confidence * 100, 1));

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        stopwatch.Stop();
        //        _logger.LogError(ex, "Error in mock OCR processing for {ImagePath}", imagePath);

        //        return new OCRResult
        //        {
        //            Success = false,
        //            ProcessingTime = stopwatch.Elapsed,
        //            Errors = new List<string> { $"OCR processing failed: {ex.Message}" }
        //        };
        //    }
        //}

        //public async Task<OCRResult> ExtractTextFromPDFAsync(string pdfPath)
        //{
        //    try
        //    {
        //        // For PDFs, we'll use similar logic but with different processing time
        //        await Task.Delay(_random.Next(1000, 3000)); // PDFs take longer

        //        var result = await ExtractTextFromImageAsync(pdfPath);
        //        result.Metadata["ProcessingMethod"] = "Mock PDF OCR";

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error occurred inside ExtractTextFromPDFAsync() in MockOCRService.cs : " + ex);
        //        throw;
        //    }
        //}

        //public async Task<ImageQualityResult> AnalyzeImageQualityAsync(string imagePath)
        //{
        //    try
        //    {
        //        await Task.Delay(_random.Next(200, 500)); // Quick quality analysis

        //        var brightness = 0.3f + (_random.NextSingle() * 0.4f); // 0.3-0.7
        //        var contrast = 0.4f + (_random.NextSingle() * 0.4f);   // 0.4-0.8
        //        var sharpness = 0.5f + (_random.NextSingle() * 0.4f);  // 0.5-0.9
        //        var noiseLevel = _random.NextSingle() * 0.3f;          // 0-0.3

        //        var qualityIssues = new List<string>();
        //        var isBlurry = sharpness < 0.6f;
        //        var isTooDark = brightness < 0.4f;
        //        var isTooLight = brightness > 0.8f;

        //        if (isBlurry) qualityIssues.Add("Image appears blurry");
        //        if (isTooDark) qualityIssues.Add("Image is too dark");
        //        if (isTooLight) qualityIssues.Add("Image is overexposed");
        //        if (noiseLevel > 0.2f) qualityIssues.Add("High noise level detected");
        //        if (contrast < 0.5f) qualityIssues.Add("Low contrast");

        //        var overallQuality = (brightness + contrast + sharpness + (1 - noiseLevel)) / 4f;

        //        return new ImageQualityResult
        //        {
        //            OverallQuality = overallQuality,
        //            Brightness = brightness,
        //            Contrast = contrast,
        //            Sharpness = sharpness,
        //            NoiseLevel = noiseLevel,
        //            IsBlurry = isBlurry,
        //            IsTooDark = isTooDark,
        //            IsTooLight = isTooLight,
        //            QualityIssues = qualityIssues
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error occurred inside AnalyzeImageQualityAsync() in MockOCRService.cs : " + ex);
        //        throw;
        //    }
        //}

        //private DocumentType GuessDocumentTypeFromPath(string path)
        //{
        //    try
        //    {
        //        var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        //        if (fileName.Contains("aadhaar") || fileName.Contains("aadhar")) return DocumentType.Aadhaar;
        //        if (fileName.Contains("pan")) return DocumentType.PAN;
        //        if (fileName.Contains("passport")) return DocumentType.Passport;
        //        if (fileName.Contains("license") || fileName.Contains("licence")) return DocumentType.DrivingLicense;
        //        if (fileName.Contains("voter")) return DocumentType.VoterID;                

        //        return DocumentType.Other;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error occurred inside GuessDocumentTypeFromPath() in MockOCRService.cs : " + ex);
        //        throw;
        //    }
        //}

        //private string GetMockTextForDocumentType(DocumentType documentType)
        //{
        //    try
        //    {
        //        if (_mockTexts.ContainsKey(documentType) && _mockTexts[documentType].Length > 0)
        //        {
        //            var texts = _mockTexts[documentType];
        //            return texts[_random.Next(texts.Length)];
        //        }

        //        return "Sample document text\nMock OCR processing\nDocument type: " + documentType.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error occurred inside GetMockTextForDocumentType() in MockOCRService.cs : " + ex);
        //        throw;
        //    }
        //}
    //}
}
