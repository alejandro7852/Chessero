using System.ComponentModel.DataAnnotations;

namespace Chessero.Api.DTOs
{
    public class AnalyzePositionRequest
    {
        [Required]
        public string Fen { get; set; } = string.Empty;
    }
}