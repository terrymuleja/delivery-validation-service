namespace DeliveryValidationService.Api.Services
{
    public interface ICloudinaryService
    {
        Task<byte[]> DownloadImageAsync(string imageUrl);
    }
}
