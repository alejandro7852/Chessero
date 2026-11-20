using Microsoft.AspNetCore.Mvc;
using Chessero.Api.Models;
using Chessero.Api.DTOs;
using pax.chess;
using System.Text.RegularExpressions;
using Chessero.Api.Services;

namespace Chessero.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly StockfishService _stockfishService;

        public GamesController(StockfishService stockfishService)
        {
            _stockfishService = stockfishService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var game = new Game
            {
                Id = 1,
                WhitePlayer = "Alejandro",
                BlackPlayer = "Opponent",
                Result = "1-0",
                Opening = "Sicilian Defense"
            };

            return Ok(game);
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

                // Asignar los jugadores a los metadatos
                game.Metadata.White = whitePlayer ?? "?";
                game.Metadata.Black = blackPlayer ?? "?";

                return Ok(new
                {
                    whitePlayer,
                    blackPlayer,
                    result,
                    totalMoves = game.Moves.Count,
                    finalPosition = FenSerializer.Serialize(game.CurrentPosition),
                    pgn = request.Pgn
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo interpretar la partida PGN.",
                    detail = ex.Message
                });
            }
        }

        [HttpPost("analyze-position")]
        public async Task<IActionResult> AnalyzePosition([FromBody] string fen)
        {
            try
            {
                var analysis = await _stockfishService.AnalyzePosition(fen);

                return Ok(new
                {
                    fen,
                    analysis
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo analizar la posición.",
                    detail = ex.Message
                });
            }
        }


    }
}