
using System.Diagnostics;
using Chessero.Api.Models;

namespace Chessero.Api.Services
{
    public class StockfishService
    {
        private readonly string _enginePath;

        public StockfishService(IWebHostEnvironment environment)
        {
            _enginePath = Path.Combine(
                environment.ContentRootPath,
                "..",
                "..",
                "stockfish",
                "stockfish-windows-x86-64-universal.exe"
            );
        }

        public async Task<StockfishAnalysis> AnalyzePosition(string fen)
        {
            if (!File.Exists(_enginePath))
            {
                throw new FileNotFoundException(
                    "No se encontró Stockfish.",
                    _enginePath
                );
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _enginePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var input = process.StandardInput;
            var output = process.StandardOutput;

            await input.WriteLineAsync("uci");
            await input.FlushAsync();

            string? line;

            while ((line = await output.ReadLineAsync()) != null)
            {
                if (line == "uciok")
                    break;
            }

            await input.WriteLineAsync("isready");
            await input.FlushAsync();

            while ((line = await output.ReadLineAsync()) != null)
            {
                if (line == "readyok")
                    break;
            }

            await input.WriteLineAsync($"position fen {fen}");
            await input.WriteLineAsync("go depth 15");
            await input.FlushAsync();

            var analysis = new StockfishAnalysis();

            while ((line = await output.ReadLineAsync()) != null)
            {
                if (line.StartsWith("info depth "))
                {
                    ParseInfo(line, analysis);
                }

                if (line.StartsWith("bestmove"))
                {
                    var parts = line.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries
                    );

                    if (parts.Length > 1 && parts[1] != "(none)")
                        analysis.BestMove = parts[1];

                    if (parts.Length > 3 && parts[2] == "ponder")
                        analysis.Ponder = parts[3];

                    break;
                }
            }

            await input.WriteLineAsync("quit");
            await input.FlushAsync();

            return analysis;
        }

        private static void ParseInfo(
            string line,
            StockfishAnalysis analysis)
        {
            var parts = line.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            );

            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i])
                {
                    case "depth":
                        if (int.TryParse(parts[i + 1], out var depth))
                            analysis.Depth = depth;
                        break;

                    case "nodes":
                        if (long.TryParse(parts[i + 1], out var nodes))
                            analysis.Nodes = nodes;
                        break;

                    case "time":
                        if (long.TryParse(parts[i + 1], out var time))
                            analysis.TimeMs = time;
                        break;

                    case "score":
                        var type = parts[i + 1];

                        if (i + 2 >= parts.Length ||
                            !int.TryParse(parts[i + 2], out var value))
                            break;

                        analysis.Score = new StockfishScore
                        {
                            Type = type,
                            Value = value,
                            Pawns = type == "cp"
                                ? value / 100m
                                : null
                        };
                        break;

                    case "pv":
                        analysis.PrincipalVariation =
                            parts.Skip(i + 1).ToList();
                        return;
                }
            }
        }
    }
}
