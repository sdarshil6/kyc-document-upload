using KYCDocumentAPI.API.Models.Responses;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InstallationTestController : ControllerBase
    {
        private readonly ILogger<InstallationTestController> _logger;

        public InstallationTestController(ILogger<InstallationTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test OCR installation and dependencies
        /// </summary>
        [HttpGet("ocr-dependencies")]
        public async Task<ActionResult<ApiResponse<object>>> TestOCRDependencies()
        {
            try
            {
                var tests = new List<object>();

                // Test Tesseract
                var tesseractResult = await TestTesseract();
                tests.Add(new
                {
                    Component = "Tesseract OCR",
                    Status = tesseractResult.Success ? "✅ PASS" : "❌ FAIL",
                    Details = tesseractResult.Details,
                    Version = tesseractResult.Version
                });

                // Test Python
                var pythonResult = await TestPython();
                tests.Add(new
                {
                    Component = "Python Runtime",
                    Status = pythonResult.Success ? "✅ PASS" : "❌ FAIL",
                    Details = pythonResult.Details,
                    Version = pythonResult.Version
                });

                // Test EasyOCR
                var easyocrResult = await TestEasyOCR();
                tests.Add(new
                {
                    Component = "EasyOCR Library",
                    Status = easyocrResult.Success ? "✅ PASS" : "❌ FAIL",
                    Details = easyocrResult.Details,
                    Version = easyocrResult.Version
                });

                // Test Languages
                var languagesResult = await TestLanguages();
                tests.Add(new
                {
                    Component = "Language Packs",
                    Status = languagesResult.Success ? "✅ PASS" : "❌ FAIL",
                    Details = languagesResult.Details,
                    SupportedLanguages = languagesResult.Languages
                });

                var passedTests = tests.Count(t => t.GetType().GetProperty("Status")?.GetValue(t)?.ToString()?.Contains("PASS") == true);
                var totalTests = tests.Count;

                var response = new
                {
                    Summary = new
                    {
                        TotalTests = totalTests,
                        PassedTests = passedTests,
                        FailedTests = totalTests - passedTests,
                        OverallStatus = passedTests == totalTests ? "✅ ALL TESTS PASSED" : "⚠️ SOME TESTS FAILED",
                        ReadyForProduction = passedTests >= 3
                    },
                    SystemInfo = new
                    {
                        OperatingSystem = GetOperatingSystem(),
                        DotNetVersion = Environment.Version.ToString(),
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount
                    },
                    TestResults = tests,
                    NextSteps = passedTests == totalTests ?
                        new[] { "All dependencies verified!", "Ready to proceed to Step 2: OCR Engine Abstractions" } :
                        new[] { "Fix failed dependencies", "Refer to installation guide", "Re-run tests" }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "OCR dependency test completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OCR dependency testing");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("OCR dependency test failed"));
            }
        }

        private async Task<(bool Success, string Details, string Version)> TestTesseract()
        {
            try
            {
                var result = await RunCommand("tesseract", "--version");
                if (result.Success && result.Output.ToLower().Contains("tesseract"))
                {
                    var version = result.Output.Split('\n')[0].Trim();
                    return (true, "Tesseract found and working", version);
                }
                return (false, "Tesseract not found or not working", "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside TestTesseract() in InstallationTestController.cs : " + ex);
                return (false, $"Error: {ex.Message}", "Unknown");
            }
        }

        private async Task<(bool Success, string Details, string Version)> TestPython()
        {
            try
            {
                var pythonCommands = new[] { "python", "python3", "py" };

                foreach (var cmd in pythonCommands)
                {
                    try
                    {
                        var result = await RunCommand(cmd, "--version");
                        if (result.Success && result.Output.ToLower().Contains("python"))                        
                            return (true, $"Python found using command: {cmd}", result.Output.Trim());                        
                        
                        _logger.LogError($"Issue occured inside TestPython() for python command {cmd} in InstallationTestController.cs : " + result.Output);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError($"Error occured inside TestPython() for python command {cmd} in InstallationTestController.cs : " + ex);
                        continue;
                    }
                }

                return (false, "Python not found", "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside TestPython() in InstallationTestController.cs : " + ex);
                return (false, $"Error: {ex.Message}", "Unknown");
            }
        }

        private async Task<(bool Success, string Details, string Version)> TestEasyOCR()
        {
            try
            {
                var pythonCommands = new[] { "python", "python3", "py" };

                foreach (var cmd in pythonCommands)
                {
                    try
                    {
                        var result = await RunCommand(cmd, "-c \"import easyocr; print('EasyOCR version:', easyocr.__version__)\"");
                        if (result.Success && result.Output.Contains("EasyOCR version"))                        
                            return (true, "EasyOCR installed and accessible", result.Output.Trim());
                                                
                        _logger.LogError($"Issue occured inside TestEasyOCR() for python command {cmd} in InstallationTestController.cs : " + result.Output);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError($"Error occured inside TestEasyOCR() for python command {cmd} in InstallationTestController.cs : " + ex);
                        continue;
                    }
                }
                return (false, "EasyOCR not installed or not accessible", "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside TestEasyOCR() in InstallationTestController.cs : " + ex);
                return (false, $"Error: {ex.Message}", "Unknown");
            }
        }

        private async Task<(bool Success, string Details, string[] Languages)> TestLanguages()
        {
            try
            {
                var result = await RunCommand("tesseract", "--list-langs");

                if (result.Success)
                {
                    var languages = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

                    bool hasEnglish = languages.Contains("eng");
                    bool hasHindi = languages.Contains("hin");

                    var details = $"English: {(hasEnglish ? "✅" : "❌")}, Hindi: {(hasHindi ? "✅" : "❌")}";

                    return (hasEnglish, details, languages);
                }

                _logger.LogError($"Issue occured inside TestLanguages() in InstallationTestController.cs : " + result.Output);
                return (false, "Could not retrieve language list", Array.Empty<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside TestLanguages() in InstallationTestController.cs : " + ex);
                return (false, $"Error: {ex.Message}", Array.Empty<string>());
            }
        }

        private string GetOperatingSystem()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "Windows";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "Linux";
                else
                    return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetOperatingSystem() in InstallationTestController.cs : " + ex);
                throw;
            }
        }

        private async Task<(bool Success, string Output)> RunCommand(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode == 0, output + error);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside RunCommand() in InstallationTestController.cs : " + ex);
                return (false, ex.Message);
            }
        }
    }
}