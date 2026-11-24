namespace Chessero.Api.DTOs
{
    public class GameMoveDto
    {
        public int MoveNumber { get; set; }
        public string Player { get; set; } = string.Empty;
        public string San { get; set; } = string.Empty;
        public string Uci { get; set; } = string.Empty;
        public string Fen { get; set; } = string.Empty;
    }
}