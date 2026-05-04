using Google.Apis.Auth;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthenticationController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public GoogleAuthenticationController(LudoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CreatePlayerData(string idToken, string city, string country, string countryCallingCode)
        {
            // Example: extract useful info
            String email = "";
            String name = "";
            String pictureUrl = "";// ✅ profile picture URL
            String googleId = "";// Unique Google user ID
            if (idToken == "Guest1")
            {
                email = "Sodi@gmail.com";
                name = "Sodi";
                pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";// ✅ profile picture URL
                googleId = idToken;
            }
            else
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                // Example: extract useful info
                email = payload.Email;
                name = payload.Name;
                pictureUrl = payload.Picture; // ✅ profile picture URL
                googleId = payload.Subject; // Unique Google user ID
            }

            Player existingPlayer = _context.Players.FirstOrDefault(p => p.GoogleId == googleId);

            if (existingPlayer != null)
            {
                String Message = "Player login successfully.";

                // If player exists by email, return with Player Id to login
                return Ok(new {
                    Message = Message,
                    Id = existingPlayer.PlayerId,
                    GoogleId = existingPlayer.GoogleId,
                    Name = existingPlayer.Name,
                    Email = existingPlayer.Email,
                    PictureUrl = existingPlayer.PictureUrl,
                    PhoneNumber = existingPlayer.PhoneNumber,
                    Country = existingPlayer.Country,
                    CountryCallingCode = existingPlayer.CountryCallingCode,
                    City = existingPlayer.City,
                    GamesPlayed = existingPlayer.GamesPlayed,
                    GamesWon = existingPlayer.GamesWon,
                    GamesLost = existingPlayer.GamesLost,
                    BestWin = existingPlayer.BestWin,
                    TotalLost = existingPlayer.TotalLost,
                    TotalWin = existingPlayer.TotalWin
                });
            }

            Player newPlayer = new Player
            {
                GoogleId = googleId,
                Name = name,
                Email = email,
                PictureUrl = pictureUrl,
                Country = country,
                City = city,
                CountryCallingCode = countryCallingCode
            };
            _context.Players.Add(newPlayer);
            // Save changes to the database
            await _context.SaveChangesAsync();
            
            Player player = _context.Players.FirstOrDefault(p => p.GoogleId == googleId);

            if (player != null)
            {
                return Ok(new
                {
                    Message = "Player created successfully.",
                    Id = player.PlayerId,
                    GoogleId = player.GoogleId,
                    Name = player.Name,
                    Email = player.Email,
                    PictureUrl = player.PictureUrl,
                    PhoneNumber = player.PhoneNumber,
                    Country = player.Country,
                    CountryCallingCode = player.CountryCallingCode,
                    City = player.City,
                    GamesPlayed = player.GamesPlayed,
                    GamesWon = player.GamesWon,
                    GamesLost = player.GamesLost,
                    BestWin = player.BestWin,
                    TotalLost = player.TotalLost,
                    TotalWin = player.TotalWin
                });
            }
            return NotFound(new { Message = "Player created/updated Failed." });
        }
    }
}
