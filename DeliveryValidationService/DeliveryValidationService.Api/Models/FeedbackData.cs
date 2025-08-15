namespace DeliveryValidationService.Api.Models
{
    public class FeedbackData
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string CorrectBodyPart { get; set; } = string.Empty;
        public string CorrectText { get; set; } = string.Empty;
        public bool HumanValidation { get; set; }
        public string? Notes { get; set; }
        public DateTime ProvidedAt { get; set; }
    }
}
