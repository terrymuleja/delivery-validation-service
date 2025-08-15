namespace DeliveryValidationService.Api.Services
{
    public interface ITextSimilarityService
    {
        double CalculateSimilarity(string expected, string actual);
    }
}
