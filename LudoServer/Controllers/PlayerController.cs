using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public PlayerController(LudoDbContext context)
        {
            _context = context;
        }

        // Get All players
        [HttpGet]
        public IActionResult GetAllPlayers()
        {
            var players = _context.Players.ToList();

            if (players != null && players.Any())
            {
                return Ok(players.Select(p => new
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.Name,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    PlayerPicture = p.PictureUrl
                }));
            }

            return NotFound(new { Message = "No players found." });
        }

        // Get player by Id
        [HttpGet("{id}")]
        public IActionResult GetPlayerById(int id)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerId == id);

            if (player != null)
            {
                return Ok(new
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.Name,
                    Email = player.Email,
                    PhoneNumber = player.PhoneNumber,
                    PlayerPicture = player.PictureUrl
                });
            }

            return NotFound(new { Message = "Player not found." });
        }

        // Create new player
        [HttpPost("create")]
        public IActionResult CreatePlayer([FromBody] PlayerDto playerDto)
        {
            if (playerDto == null)
            {
                return BadRequest(new { Message = "Invalid player data." });
            }

            var player = new Player
            {
                Name = playerDto.Name,
                Email = playerDto.Email,
                PhoneNumber = playerDto.PhoneNumber,
                PictureUrl = playerDto.PictureUrl
            };

            _context.Players.Add(player);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetPlayerById), new { id = player.PlayerId }, player);
        }

        // Update player by Id
        [HttpPut("{id}")]
        public IActionResult UpdatePlayer(int id, [FromBody] PlayerDto updatedPlayerDto)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound(new { Message = "Player not found." });
            }

            player.Name = updatedPlayerDto.Name;
            player.Email = updatedPlayerDto.Email;
            player.PhoneNumber = updatedPlayerDto.PhoneNumber;
            player.PictureUrl = updatedPlayerDto.PictureUrl;

            _context.SaveChanges();

            return Ok(player);
        }

        // Delete player by Id
        [HttpDelete("{id}")]
        public IActionResult DeletePlayer(int id)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound(new { Message = "Player not found." });
            }

            _context.Players.Remove(player);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
