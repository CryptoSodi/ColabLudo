using Azure.Core;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using static System.Net.WebRequestMethods;


namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly LudoDbContext _context;
        String FilePath = "C:/otpmanager/otp.txt";
        public OtpController(LudoDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePlayerData(string name, string email, string pictureUrl, string phoneNumber = null)
        {
            // If phoneNumber is available, check if the player exists by phone number (in case they used WhatsApp OTP first)
            var existingPlayerByPhone = !string.IsNullOrEmpty(phoneNumber)
                ? _context.Players.FirstOrDefault(p => p.PhoneNumber == phoneNumber)
                : null;

            if (existingPlayerByPhone != null)
            {
                // If player exists by phone number, update the player with email, name, and picture
                existingPlayerByPhone.Name = name;
                existingPlayerByPhone.Email = email;
                existingPlayerByPhone.PictureUrl = pictureUrl;
                _context.Players.Update(existingPlayerByPhone);
            }
            else
            {
                // Check if player exists by email (in case they used Google login first)
                var existingPlayerByEmail = _context.Players.FirstOrDefault(p => p.Email == email && p.Name == name);
                if (existingPlayerByEmail != null)
                {
                    // If player exists by email, return a conflict message
                    return Ok(new
                    {
                        Message = "Player created/updated successfully.",
                        PlayerId = existingPlayerByEmail.PlayerId
                    });
                }
                // If neither phone nor email exists, create a new player record
                var newPlayer = new Player
                {
                    Name = name,
                    Email = email,
                    PictureUrl = pictureUrl,
                    PhoneNumber = phoneNumber // This could be null if the user didn't provide a phone number
                };
                _context.Players.Add(newPlayer);
            }
            // Save changes to the database
            await _context.SaveChangesAsync();
            var player = _context.Players.FirstOrDefault(r => r.Name == name && r.Email == email && r.PictureUrl == pictureUrl);
            if (player != null)
            {
                return Ok(new
                {
                    Message = "Player created/updated successfully.",
                    PlayerId = player.PlayerId
                });
            }
            return NotFound(new { Message = "Player created/updated Failed." });
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] OtpRequest request)
        {
            // Check if the phone number already exists in the database

            Player existingPlayer = null;
            if (request.playerId != null)
                existingPlayer = _context.Players.FirstOrDefault(p => p.PlayerId + "" == request.playerId + "");
            else
                existingPlayer = _context.Players.FirstOrDefault(p => p.PhoneNumber == request.PhoneNumber);

            String Otp = GenerateOtp();
            if (existingPlayer != null)
            {
                // Update the OTP if the player already exists
                existingPlayer.Otp = Otp;
                existingPlayer.PhoneNumber = request.PhoneNumber;
                existingPlayer.Country = request.country;
                existingPlayer.CountryCallingCode = request.countryCode;
                existingPlayer.City = request.city;
                _context.Players.Update(existingPlayer);
            }
            else
            {
                // If the player doesn't exist, create a new record
                var newPlayer = new Player
                {
                    Otp = Otp,
                    PhoneNumber = request.PhoneNumber,
                    Country = request.country,
                    CountryCallingCode = request.countryCode,
                    City = request.city,
                };
                _context.Players.Add(newPlayer);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            if (!System.IO.File.Exists(FilePath))
            {
                Console.WriteLine("File does not exist. Creating...");
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            }

            String line = request.PhoneNumber + "," + Otp + Environment.NewLine;
            await System.IO.File.AppendAllTextAsync(FilePath, line);

            return Ok(new { Message = "OTP saved successfully." });
        }
        static string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string phoneNumber, [FromQuery] string otp)
        {
            var otpRecord = _context.Players.FirstOrDefault(r => r.PhoneNumber == phoneNumber && r.Otp == otp);

            if (otpRecord != null)
            {
                return Ok(new
                {
                    Message = "OTP verified successfully.",
                    GoogleId = otpRecord.GoogleId,
                    Name = otpRecord.Name,
                    Email = otpRecord.Email,
                    PictureUrl = otpRecord.PictureUrl,
                    PhoneNumber = otpRecord.PhoneNumber,
                    Country = otpRecord.Country,
                    CountryCallingCode = otpRecord.CountryCallingCode,
                    City = otpRecord.City,
                    GamesPlayed = otpRecord.GamesPlayed,
                    GamesWon = otpRecord.GamesWon,
                    GamesLost = otpRecord.GamesLost,
                    BestWin = otpRecord.BestWin,
                    TotalLost = otpRecord.TotalLost,
                    TotalWin = otpRecord.TotalWin
                });
            }

            return NotFound(new { Message = "OTP verification failed." });
        }
    }
}
