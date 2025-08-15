using FuzzySharp;

namespace DeliveryValidationService.Api.Services.Implementations
{
    public class TextSimilarityService : ITextSimilarityService
    {
        public double CalculateSimilarity(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
                return 0.0;

            var similarity = Fuzz.Ratio(expected.ToLower().Trim(), actual.ToLower().Trim());
            return similarity / 100.0; // Convert to 0-1 range
        }
    }
}
