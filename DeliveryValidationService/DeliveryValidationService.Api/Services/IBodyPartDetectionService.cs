namespace DeliveryValidationService.Api.Services
{
    public interface IBodyPartDetectionService
    {
        Task<(string BodyPart, double Confidence)> DetectBodyPartAsync(byte[] imageData);
    }
}
