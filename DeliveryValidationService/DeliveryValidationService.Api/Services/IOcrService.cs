namespace DeliveryValidationService.Api.Services
{
    public interface IOcrService
    {
        Task<(string Text, double Confidence)> ExtractTextAsync(byte[] imageData);
    }
}
