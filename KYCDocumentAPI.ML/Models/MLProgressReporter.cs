using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.Models
{
    public class MLProgressReporter
    {
        private readonly IProgress<TrainingProgress>? _progress;
        private readonly int _totalEpochs;
        private readonly ILogger _logger;
        private int _currentEpoch = 0;

        public MLProgressReporter(IProgress<TrainingProgress>? progress, int totalEpochs, ILogger logger)
        {
            _progress = progress;
            _totalEpochs = totalEpochs;
            _logger = logger;
        }

        public void ReportProgress(string phase, float accuracy = 0f, float loss = 0f)
        {
            _currentEpoch = Math.Min(_currentEpoch + 1, _totalEpochs);

            var progressReport = new TrainingProgress
            {
                CurrentEpoch = _currentEpoch,
                TotalEpochs = _totalEpochs,
                CurrentPhase = phase,
                CurrentAccuracy = accuracy,
                CurrentLoss = loss
            };

            _progress?.Report(progressReport);
            _logger.LogInformation("Training progress: {Progress}", progressReport.ToString());
        }
    }
}
