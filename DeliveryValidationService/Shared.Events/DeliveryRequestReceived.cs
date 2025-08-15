namespace Shared.Events
{
    public record DeliveryRequestReceived
    {
        public Guid RequestId { get; init; }
        public string DeliveredImageUrl { get; init; } = string.Empty;
        public string ExpectedText { get; init; } = string.Empty;
        public DateTime DeliveredAt { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }
}
