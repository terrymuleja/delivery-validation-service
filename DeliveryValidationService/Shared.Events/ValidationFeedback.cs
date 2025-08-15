using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Events
{
    public record ValidationFeedback
    {
        public Guid RequestId { get; init; }
        public string CorrectBodyPart { get; init; } = string.Empty;
        public string CorrectText { get; init; } = string.Empty;
        public bool HumanValidation { get; init; }
        public string? Notes { get; init; }
        public DateTime ProvidedAt { get; init; }
    }
}
