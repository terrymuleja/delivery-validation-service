using DeliveryValidationService.Api.Data;
using DeliveryValidationService.Api.Models;
using MassTransit;
using Shared.Events;

namespace DeliveryValidationService.Api.Consumers
{
    public class ValidationFeedbackConsumer : IConsumer<ValidationFeedback>
    {
        private readonly ValidationDbContext _context;
        private readonly ILogger<ValidationFeedbackConsumer> _logger;

        public ValidationFeedbackConsumer(ValidationDbContext context, ILogger<ValidationFeedbackConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ValidationFeedback> context)
        {
            var feedback = context.Message;
            _logger.LogInformation("Received validation feedback for request {RequestId}", feedback.RequestId);

            try
            {
                var feedbackData = new FeedbackData
                {
                    Id = Guid.NewGuid(),
                    RequestId = feedback.RequestId,
                    CorrectBodyPart = feedback.CorrectBodyPart,
                    CorrectText = feedback.CorrectText,
                    HumanValidation = feedback.HumanValidation,
                    Notes = feedback.Notes,
                    ProvidedAt = feedback.ProvidedAt
                };

                _context.FeedbackData.Add(feedbackData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Feedback stored for request {RequestId}", feedback.RequestId);

                // TODO: Trigger ML model retraining if enough feedback accumulated
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process feedback for request {RequestId}", feedback.RequestId);
                throw;
            }
        }
    }
}
