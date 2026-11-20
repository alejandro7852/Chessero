using System.ComponentModel.DataAnnotations;

namespace Chessero.Api.DTOs
{
    public class ImportGameRequest
    {
        [Required]
        public string Pgn { get; set; } = string.Empty;
    }
}