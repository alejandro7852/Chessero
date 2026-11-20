namespace Chessero.Api.Models
{
    public class Game
    {
        public int Id { get; set; }

        public string WhitePlayer { get; set; } = string.Empty;

        public string BlackPlayer { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public string Opening { get; set; } = string.Empty;
    }
}