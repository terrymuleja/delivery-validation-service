using Shared.Events;

namespace DeliveryValidationService.Api.Models
{
    public class ValidationResult
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public ValidationStatus Status { get; set; }
        public double ConfidenceScore { get; set; }
        public string? DetectedBodyPart { get; set; }
        public string? ExtractedText { get; set; }
        public double? TextSimilarityScore { get; set; }
        public DateTime ValidatedAt { get; set; }
        public bool RequiresHumanReview { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ImagePath { get; set; }
        public string? ExpectedText { get; set; }
    }
}
