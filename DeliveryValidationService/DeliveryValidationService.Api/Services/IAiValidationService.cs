using DeliveryValidationService.Api.Models;

namespace DeliveryValidationService.Api.Services
{
    public interface IAiValidationService
    {
        Task<ValidationResult> ValidateDeliveryAsync(Guid requestId, string imageUrl, string expectedText);
    }
}
