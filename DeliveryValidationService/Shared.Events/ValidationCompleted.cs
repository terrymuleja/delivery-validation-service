using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Events
{
    public record ValidationCompleted
    {
        public Guid RequestId { get; init; }
        public ValidationStatus Status { get; init; }
        public double ConfidenceScore { get; init; }
        public string? DetectedBodyPart { get; init; }
        public string? ExtractedText { get; init; }
        public double? TextSimilarityScore { get; init; }
        public DateTime ValidatedAt { get; init; }
        public bool RequiresHumanReview { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
