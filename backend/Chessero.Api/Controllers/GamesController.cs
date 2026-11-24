using Microsoft.AspNetCore.Mvc;
using Chessero.Api.Models;
using Chessero.Api.DTOs;
using pax.chess;
using System.Text.RegularExpressions;
using Chessero.Api.Services;
using pax.chess.Extensions;

namespace Chessero.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly StockfishService _stockfishService;
        private readonly ILogger<GamesController> _logger;
        public GamesController(StockfishService stockfishService, ILogger<GamesController> logger)
        {
            _stockfishService = stockfishService;
            _logger = logger;
        }

        [HttpPost("import")]
        public IActionResult ImportGame(ImportGameRequest request)
        {
            try
            {
                // Extraer los encabezados originales del PGN
                var headers = Regex.Matches(
                    request.Pgn,
                    "^\\[(\\w+)\\s+\"(.*)\"\\]\\s*$",
                    RegexOptions.Multiline
                ).ToDictionary(
                    m => m.Groups[1].Value,
                    m => m.Groups[2].Value,
                    StringComparer.OrdinalIgnoreCase
                );

                headers.TryGetValue("White", out var whitePlayer);
                headers.TryGetValue("Black", out var blackPlayer);
                headers.TryGetValue("Result", out var result);

                // Interpretar los movimientos
                var game = PgnSerializer.Parse(request.Pgn);

                game.Evaluate();
                // Crear una partida auxiliar para reconstruir las posiciones
                var replayGame = new ChessGame();

                var initialPosition = FenSerializer.Serialize(
                    replayGame.CurrentPosition
                );

                var moves = new List<GameMoveDto>();

                for (int i = 0; i < game.Moves.Count; i++)
                {
                    var moveInfo = game.Moves[i];

                    // Aplicar el movimiento a la partida auxiliar
                    var moveState = replayGame.ApplyMove(
                        moveInfo.Move,
                        moveInfo.San
                    );

                    if (moveState != MoveState.Ok)
                    {
                        throw new InvalidOperationException(
                            $"No se pudo reproducir el movimiento {i + 1}."
                        );
                    }

                    moves.Add(new GameMoveDto
                    {
                        MoveNumber = (i / 2) + 1,
                        Player = i % 2 == 0 ? "White" : "Black",
                        San = moveInfo.San ?? string.Empty,
                        Uci = Uci.GetUci(moveInfo.Move),
                        Fen = FenSerializer.Serialize(replayGame.CurrentPosition)
                    });
                }
                // Asignar los jugadores a los metadatos
                game.Metadata.White = whitePlayer ?? "?";
                game.Metadata.Black = blackPlayer ?? "?";

                return Ok(new
                {
                    whitePlayer,
                    blackPlayer,
                    result,
                    totalMoves = game.Moves.Count,
                    initialPosition,
                    finalPosition = FenSerializer.Serialize(game.CurrentPosition),
                    moves,
                    pgn = request.Pgn
                });
            }

            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is ArgumentException)
            {
                return BadRequest(new
                {
                    message = "No se pudo interpretar la partida PGN.",
                    detail = ex.Message
                });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(504, new
                {
                    message = "Stockfish superó el tiempo máximo de análisis.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar la partida.");

                return StatusCode(500, new
                {
                    message = "Ocurrió un error interno al importar la partida."
                });
            }

        }


        [HttpPost("analyze-position")]
        public async Task<IActionResult> AnalyzePosition(
            [FromBody] AnalyzePositionRequest request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.Fen))
                {
                    throw new InvalidGameRequestException(
                        "Debes proporcionar una posición FEN."
                    );
                }

                var analysis = await _stockfishService.AnalyzePosition(
                    request.Fen,
                    HttpContext.RequestAborted
                );

                return Ok(new
                {
                    fen = request.Fen,
                    analysis
                });
            }
            catch (InvalidGameRequestException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(504, new
                {
                    message = "Stockfish superó el tiempo máximo de análisis.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar la posición.");

                return StatusCode(500, new
                {
                    message = "Ocurrió un error interno al analizar la posición."
                });
            }
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeGame(
    [FromBody] AnalyzeGameRequest request)
        {
            try
            {

                if (request == null)
                {
                    throw new InvalidGameRequestException(
                        "La solicitud de análisis es obligatoria."
                    );
                }

                if (string.IsNullOrWhiteSpace(request.InitialPosition))
                {
                    throw new InvalidGameRequestException(
                        "La posición inicial es obligatoria."
                    );
                }

                if (request.Moves == null || request.Moves.Count == 0)
                {
                    throw new InvalidGameRequestException(
                        "Debes enviar al menos un movimiento."
                    );
                }

                if (request.Moves.Any(m =>
                    m == null ||
                    string.IsNullOrWhiteSpace(m.Fen) ||
                    string.IsNullOrWhiteSpace(m.Uci)))
                {
                    throw new InvalidGameRequestException(
                        "Todos los movimientos deben contener FEN y UCI."
                    );
                }


                // 1. Construir la lista completa de posiciones.
                // La primera es la posición inicial.
                // Las siguientes corresponden a cada movimiento realizado.

                var fens = new List<string>
        {
            request.InitialPosition
        };

                fens.AddRange(request.Moves.Select(m => m.Fen));

                // 2. Analizar todas las posiciones con Stockfish.

                var analyses = await _stockfishService.AnalyzePositions(
                    fens,
                    HttpContext.RequestAborted
                );

                if (analyses.Count != fens.Count)
                {
                    throw new InvalidOperationException(
                        "La cantidad de análisis recibidos no coincide con las posiciones enviadas."
                    );
                }

                // 3. Comparar la posición anterior y posterior
                // para clasificar cada movimiento.

                var analyzedMoves = request.Moves
                    .Select((move, index) =>
                    {
                        var analysisBefore = analyses[index];
                        var analysisAfter = analyses[index + 1];

                        if (analysisBefore.Score == null ||
                            analysisAfter.Score == null)
                        {
                            throw new InvalidOperationException(
                                $"No se pudo obtener la evaluación del movimiento {move.MoveNumber}."
                            );
                        }

                        // La evaluación anterior corresponde al jugador
                        // que debe realizar el movimiento.

                        var evaluationBefore = analysisBefore.Score;

                        var evaluationAfter = new StockfishScore
                        {
                            Type = analysisAfter.Score.Type,
                            Value = -analysisAfter.Score.Value,
                            Pawns = analysisAfter.Score.Pawns.HasValue
                                ? -analysisAfter.Score.Pawns.Value
                                : null
                        };

                        var playedBestMove = string.Equals(
                            move.Uci,
                            analysisBefore.BestMove,
                            StringComparison.OrdinalIgnoreCase
                        );

                        // Solo calculamos pérdida de centipeones cuando ambas
                        // evaluaciones son del tipo cp.
                        int? centipawnLoss = null;

                        if (evaluationBefore.Type == "cp" &&
                            evaluationAfter.Type == "cp")
                        {
                            centipawnLoss = Math.Max(
                                0,
                                evaluationBefore.Value - evaluationAfter.Value
                            );
                        }
                        if (playedBestMove)
                        {
                            centipawnLoss = 0;
                        }
                        string classification;

                        if (playedBestMove)
                        {
                            classification = "Best";
                        }
                        else if (evaluationAfter.Type == "mate" &&
                                 evaluationAfter.Value < 0)
                        {
                            // El movimiento permite un mate forzado del rival.
                            classification = "Blunder";
                        }
                        else if (evaluationBefore.Type == "mate" &&
                                 evaluationBefore.Value > 0 &&
                                 !(evaluationAfter.Type == "mate" &&
                                   evaluationAfter.Value > 0))
                        {
                            // Se pierde una posibilidad de mate forzado.
                            classification = "Blunder";
                        }
                        else if (centipawnLoss.HasValue)
                        {
                            if (centipawnLoss.Value <= 50)
                                classification = "Good";
                            else if (centipawnLoss.Value <= 150)
                                classification = "Mistake";
                            else
                                classification = "Blunder";
                        }
                        else
                        {
                            // Hay una evaluación de mate, pero no una pérdida
                            // de centipeones comparable.
                            classification = "Good";
                        }
                        double? accuracy = null;

                        if (playedBestMove)
                        {
                            accuracy = 100.0;
                        }
                        else if (evaluationAfter.Type == "mate" &&
                                 evaluationAfter.Value < 0)
                        {
                            accuracy = 0.0;
                        }
                        else if (centipawnLoss.HasValue)
                        {
                            accuracy = Math.Round(
                                100 * Math.Exp(-0.003 * centipawnLoss.Value),
                                1
                            );
                        }
                        return new
                        {
                            move.MoveNumber,
                            move.Player,
                            move.San,
                            move.Uci,
                            move.Fen,

                            Classification = classification,
                            PlayedBestMove = playedBestMove,
                            BestMoveUCI = analysisBefore.BestMove,
                            CentipawnLoss = centipawnLoss,
                            Accuracy = accuracy,
                            EvaluationBefore = analysisBefore.Score,
                            EvaluationAfter = evaluationAfter,

                            BestLineUCI = analysisBefore.PrincipalVariation,

                            Depth = analysisBefore.Depth
                        };
                    })
                    .ToList();
                var whiteAccuracyValues = analyzedMoves
                    .Where(m => m.Player == "White" && m.Accuracy.HasValue)
                    .Select(m => m.Accuracy!.Value)
                    .ToList();

                var blackAccuracyValues = analyzedMoves
                    .Where(m => m.Player == "Black" && m.Accuracy.HasValue)
                    .Select(m => m.Accuracy!.Value)
                    .ToList();

                var whiteAccuracy = whiteAccuracyValues.Count > 0
                    ? Math.Round(whiteAccuracyValues.Average(), 1)
                    : 0;

                var blackAccuracy = blackAccuracyValues.Count > 0
                    ? Math.Round(blackAccuracyValues.Average(), 1)
                    : 0;
                // 4. Generar un resumen de la partida.

                var summary = new
                {
                    BestMoves = analyzedMoves.Count(m => m.Classification == "Best"),
                    GoodMoves = analyzedMoves.Count(m => m.Classification == "Good"),
                    Mistakes = analyzedMoves.Count(m => m.Classification == "Mistake"),
                    Blunders = analyzedMoves.Count(m => m.Classification == "Blunder")
                };

                return Ok(new
                {
                    TotalMoves = analyzedMoves.Count,

                    Accuracy = new
                    {
                        White = whiteAccuracy,
                        Black = blackAccuracy
                    },

                    Summary = summary,
                    AnalyzedMoves = analyzedMoves
                });
            }

            catch (InvalidGameRequestException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(504, new
                {
                    message = "Stockfish superó el tiempo máximo de análisis.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno al analizar la partida.");
                return StatusCode(500, new
                {
                    message = "Ocurrió un error interno al analizar la partida."
                });
            }

        }

        // Convertir la evaluación de Stockfish a centipeones.
        // Los valores positivos son favorables para el jugador al turno.

        private static int ScoreToCentipawns(StockfishScore score)
        {
            if (score.Type == "cp")
            {
                return score.Value;
            }

            if (score.Type == "mate")
            {
                // Representación aproximada de las posiciones de mate.
                // Un mate favorable tiene una puntuación positiva.

                var mateDistance = Math.Abs((long)score.Value);

                return score.Value > 0
                    ? (int)Math.Max(0, 100000 - mateDistance * 100)
                    : (int)Math.Min(0, -100000 + mateDistance * 100);
            }

            throw new InvalidOperationException(
                $"Tipo de evaluación de Stockfish no reconocido: {score.Type}"
            );
        }


    }
}