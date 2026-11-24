using System.ComponentModel.DataAnnotations;

namespace Chessero.Api.DTOs
{
    public class AnalyzeGameRequest
    {
        [Required]
        public string InitialPosition { get; set; } =
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        [Required]
        [MinLength(1, ErrorMessage = "Debes enviar al menos un movimiento.")]
        public List<GameMoveDto> Moves { get; set; } = new();
    }
}