using DeliveryValidationService.Api.Data;
using DeliveryValidationService.Api.Services;
using MassTransit;
using Shared.Events;

namespace DeliveryValidationService.Api.Consumers
{
    public class DeliveryRequestConsumer : IConsumer<DeliveryRequestReceived>
    {
        private readonly IAiValidationService _validationService;
        private readonly ValidationDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<DeliveryRequestConsumer> _logger;

        public DeliveryRequestConsumer(
            IAiValidationService validationService,
            ValidationDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<DeliveryRequestConsumer> logger)
        {
            _validationService = validationService;
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DeliveryRequestReceived> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing delivery validation for request {RequestId}", message.RequestId);

            try
            {
                // Perform AI validation
                var result = await _validationService.ValidateDeliveryAsync(
                    message.RequestId,
                    message.DeliveredImageUrl,
                    message.ExpectedText
                );

                // Save to database
                _context.ValidationResults.Add(result);
                await _context.SaveChangesAsync();

                // Publish validation completed event
                await _publishEndpoint.Publish(new ValidationCompleted
                {
                    RequestId = result.RequestId,
                    Status = result.Status,
                    ConfidenceScore = result.ConfidenceScore,
                    DetectedBodyPart = result.DetectedBodyPart,
                    ExtractedText = result.ExtractedText,
                    TextSimilarityScore = result.TextSimilarityScore,
                    ValidatedAt = result.ValidatedAt,
                    RequiresHumanReview = result.RequiresHumanReview,
                    ErrorMessage = result.ErrorMessage
                });

                _logger.LogInformation(
                    "Validation completed for request {RequestId} with status {Status}",
                    message.RequestId, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate delivery for request {RequestId}", message.RequestId);

                // Publish failed validation event
                await _publishEndpoint.Publish(new ValidationCompleted
                {
                    RequestId = message.RequestId,
                    Status = ValidationStatus.Failed,
                    ConfidenceScore = 0.0,
                    ValidatedAt = DateTime.UtcNow,
                    RequiresHumanReview = true,
                    ErrorMessage = ex.Message
                });

                throw; // Re-throw to trigger retry policy
            }
        }
    }
}
