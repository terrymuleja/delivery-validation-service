using Tesseract;

namespace DeliveryValidationService.Api.Services.Implementations
{
    public class TesseractOcrService : IOcrService
    {
        private readonly ILogger<TesseractOcrService> _logger;

        public TesseractOcrService(ILogger<TesseractOcrService> logger)
        {
            _logger = logger;
        }

        public async Task<(string Text, double Confidence)> ExtractTextAsync(byte[] imageData)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                    using var img = Pix.LoadFromMemory(imageData);
                    using var page = engine.Process(img);

                    var text = page.GetText().Trim();
                    var confidence = page.GetMeanConfidence();

                    _logger.LogInformation("OCR extracted: '{Text}' with confidence {Confidence}",
                        text, confidence);

                    return (text, (double)confidence);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR processing failed");
                return (string.Empty, 0.0);
            }
        }
    }
}
