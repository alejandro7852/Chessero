namespace Chessero.Api.Models
{
    public class StockfishAnalysis
    {
        public int Depth { get; set; }

        public StockfishScore? Score { get; set; }

        public string? BestMove { get; set; }

        public string? Ponder { get; set; }

        public List<string> PrincipalVariation { get; set; } = [];

        public long? Nodes { get; set; }

        public long? TimeMs { get; set; }
    }

    public class StockfishScore
    {
        public string Type { get; set; } = string.Empty;

        public int Value { get; set; }

        public decimal? Pawns { get; set; }
    }
}
