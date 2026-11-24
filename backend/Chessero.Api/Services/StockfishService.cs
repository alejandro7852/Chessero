
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

        // Analizar una sola posición
        public async Task<StockfishAnalysis> AnalyzePosition(
            string fen,
            CancellationToken cancellationToken = default)
        {
            var results = await AnalyzePositions(
                new[] { fen },
                cancellationToken
            );

            return results[0];
        }

        // Analizar múltiples posiciones usando una sola instancia de Stockfish
        public async Task<List<StockfishAnalysis>> AnalyzePositions(IEnumerable<string> fens, CancellationToken cancellationToken = default)
        {
            var positions = fens.ToList();
            var results = new List<StockfishAnalysis>();

            if (positions.Count == 0)
                return results;

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

            try
            {
                // Inicializar Stockfish
                await input.WriteLineAsync("uci");
                await input.FlushAsync();

                await WaitForResponse(output, "uciok");

                await input.WriteLineAsync("isready");
                await input.FlushAsync();

                await WaitForResponse(output, "readyok");

                // Analizar cada posición

                foreach (var fen in positions)
                {
                    using var timeoutCts = new CancellationTokenSource(
                        TimeSpan.FromSeconds(30)
                    );

                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutCts.Token
                    );

                    var analysis = new StockfishAnalysis();

                    await input.WriteLineAsync($"position fen {fen}");
                    await input.WriteLineAsync("go depth 15");
                    await input.FlushAsync();

                    string? line;
                    bool bestMoveFound = false;

                    try
                    {
                        while ((line = await output.ReadLineAsync(
                            linkedCts.Token)) != null)
                        {
                            linkedCts.Token.ThrowIfCancellationRequested();

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

                                bestMoveFound = true;
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                             !cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException(
                            "Stockfish superó el tiempo máximo de análisis de 30 segundos."
                        );
                    }

                    if (!bestMoveFound)
                    {
                        throw new InvalidOperationException(
                            "Stockfish dejó de responder durante el análisis."
                        );
                    }

                    results.Add(analysis);
                }
                return results;
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        await input.WriteLineAsync("quit");
                        await input.FlushAsync();

                        using var exitCts = new CancellationTokenSource(
                            TimeSpan.FromSeconds(2)
                        );

                        await process.WaitForExitAsync(exitCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                }
            }
        }

        private static async Task WaitForResponse(
            StreamReader output,
            string expectedResponse)
        {
            string? line;

            while ((line = await output.ReadLineAsync()) != null)
            {
                if (line == expectedResponse)
                    return;
            }

            throw new InvalidOperationException(
                $"Stockfish no respondió con {expectedResponse}."
            );
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
