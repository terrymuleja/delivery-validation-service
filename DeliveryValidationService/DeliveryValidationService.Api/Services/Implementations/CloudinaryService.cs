namespace DeliveryValidationService.Api.Services.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(HttpClient httpClient, ILogger<CloudinaryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            try
            {
                _logger.LogInformation("Downloading image from {ImageUrl}", imageUrl);

                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync();

                _logger.LogInformation("Image downloaded successfully, size: {Size} bytes", imageData.Length);

                return imageData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download image from {ImageUrl}", imageUrl);
                throw;
            }
        }
    }
}
