using DeliveryValidationService.Api.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Shared.Events;

namespace DeliveryValidationService.Api.Services.Implementations
{
    public class AiValidationService : IAiValidationService
    {
        private readonly IBodyPartDetectionService _bodyPartDetector;
        private readonly IOcrService _ocrService;
        private readonly ITextSimilarityService _textSimilarity;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<AiValidationService> _logger;
        private readonly IConfiguration _configuration;

        private readonly double _confidenceThreshold;
        private readonly double _textSimilarityThreshold;

        public AiValidationService(
            IBodyPartDetectionService bodyPartDetector,
            IOcrService ocrService,
            ITextSimilarityService textSimilarity,
            ICloudinaryService cloudinaryService,
            ILogger<AiValidationService> logger,
            IConfiguration configuration)
        {
            _bodyPartDetector = bodyPartDetector;
            _ocrService = ocrService;
            _textSimilarity = textSimilarity;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _configuration = configuration;

            _confidenceThreshold = _configuration.GetValue("AI:ConfidenceThreshold", 0.85);
            _textSimilarityThreshold = _configuration.GetValue("AI:TextSimilarityThreshold", 0.8);
        }

        public async Task<ValidationResult> ValidateDeliveryAsync(Guid requestId, string imageUrl, string expectedText)
        {
            try
            {
                _logger.LogInformation("Starting validation for request {RequestId}", requestId);

                // Download image from Cloudinary
                var imageData = await _cloudinaryService.DownloadImageAsync(imageUrl);

                // Detect body part
                var (detectedBodyPart, bodyPartConfidence) = await _bodyPartDetector.DetectBodyPartAsync(imageData);

                // Extract handwritten text
                var (extractedText, ocrConfidence) = await _ocrService.ExtractTextAsync(imageData);

                // Calculate text similarity
                var textSimilarity = _textSimilarity.CalculateSimilarity(expectedText, extractedText);

                // Calculate overall confidence
                var overallConfidence = (bodyPartConfidence + ocrConfidence + textSimilarity) / 3.0;

                // Determine validation status
                var status = DetermineValidationStatus(bodyPartConfidence, ocrConfidence, textSimilarity, overallConfidence);
                var requiresHumanReview = status != ValidationStatus.Validated;

                var result = new ValidationResult
                {
                    Id = Guid.NewGuid(),
                    RequestId = requestId,
                    Status = status,
                    ConfidenceScore = overallConfidence,
                    DetectedBodyPart = detectedBodyPart,
                    ExtractedText = extractedText,
                    TextSimilarityScore = textSimilarity,
                    ValidatedAt = DateTime.UtcNow,
                    RequiresHumanReview = requiresHumanReview,
                    ExpectedText = expectedText,
                    ImagePath = imageUrl
                };

                _logger.LogInformation(
                    "Validation completed for {RequestId}: Status={Status}, Confidence={Confidence}",
                    requestId, status, overallConfidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for request {RequestId}", requestId);
                throw;
            }
        }

        private ValidationStatus DetermineValidationStatus(
            double bodyPartConfidence,
            double ocrConfidence,
            double textSimilarity,
            double overallConfidence)
        {
            if (bodyPartConfidence < 0.6 || ocrConfidence < 0.5 || textSimilarity < _textSimilarityThreshold)
            {
                return ValidationStatus.Manual;
            }

            return overallConfidence >= _confidenceThreshold
                ? ValidationStatus.Validated
                : ValidationStatus.Manual;
        }
    }
}
