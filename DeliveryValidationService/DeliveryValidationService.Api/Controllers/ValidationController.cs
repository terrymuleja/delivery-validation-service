using DeliveryValidationService.Api.Data;
using DeliveryValidationService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace DeliveryValidationService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly ValidationDbContext _context;

        public ValidationController(ValidationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{requestId}")]
        public async Task<ActionResult<ValidationResult>> GetValidationResult(Guid requestId)
        {
            var result = await _context.ValidationResults
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (result == null)
                return NotFound();

            return result;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<List<ValidationResult>>> GetPendingValidations()
        {
            var results = await _context.ValidationResults
                .Where(r => r.Status == ValidationStatus.Manual)
                .OrderByDescending(r => r.ValidatedAt)
                .ToListAsync();

            return results;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetValidationStats()
        {
            var stats = await _context.ValidationResults
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var avgConfidence = await _context.ValidationResults
                .Where(r => r.Status != ValidationStatus.Failed)
                .AverageAsync(r => r.ConfidenceScore);

            return new { StatusCounts = stats, AverageConfidence = avgConfidence };
        }
    }
}
