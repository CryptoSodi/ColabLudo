using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DailyBonusController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public DailyBonusController(LudoDbContext context)
        {
            _context = context;
        }

        // Get All Daily Bonus
        [HttpGet]
        public IActionResult GetAllDailyBonus()
        {
            // Get all dailyBonus from the database
            List<DailyBonus> dailyBonus = _context.DailyBonus.ToList();

            if (dailyBonus != null && dailyBonus.Any())
            {
                // Return all dailyBonus as a list
                return Ok(dailyBonus.Select(d => new
                {
                    DailyBonusId = d.DailyBonusId,
                    PlayerId = d.PlayerId,
                    Day1 = d.Day1,
                    Day2 = d.Day2,
                    Day3 = d.Day3,
                    Day4 = d.Day4,
                    Day5 = d.Day5,
                    Day6 = d.Day6,
                    Day7 = d.Day7,
                    DayCounter = d.DayCounter
                }));
            }

            return NotFound(new { Message = "No Daily Bonus found." });
        }

        // Get DailyBonus by Player Id
        [HttpGet("{id}")]
        public IActionResult GetDailyBonusByPlayerId(int id)
        {
            var dailyBonus = _context.DailyBonus.FirstOrDefault(d => d.PlayerId == id);

            if (dailyBonus != null)
            {
                return Ok(new
                {
                    DailyBonusId = dailyBonus.DailyBonusId,
                    PlayerId = dailyBonus.PlayerId,
                    Day1 = dailyBonus.Day1,
                    Day2 = dailyBonus.Day2,
                    Day3 = dailyBonus.Day3,
                    Day4 = dailyBonus.Day4,
                    Day5 = dailyBonus.Day5,
                    Day6 = dailyBonus.Day6,
                    Day7 = dailyBonus.Day7,
                    DayCounter = dailyBonus.DayCounter

                });
            }

            return NotFound(new { Message = "Daily Bonus not found." });
        }

        // Create new Daily Bonus
        [HttpPost]
        public IActionResult CreateDailyBonus([FromBody] DailyBonusDto dailyBonusDto)
        {
            if (dailyBonusDto == null)
            {
                return BadRequest(new { Message = "Invalid Daily Bonus data." });
            }

            // Map the DTO to the entity
            var dailyBonus = new DailyBonus
            {
                PlayerId = dailyBonusDto.PlayerId,
                Day1 = dailyBonusDto.Day1,
                Day2 = dailyBonusDto.Day2,
                Day3 = dailyBonusDto.Day3,
                Day4 = dailyBonusDto.Day4,
                Day5 = dailyBonusDto.Day5,
                Day6 = dailyBonusDto.Day6,
                Day7 = dailyBonusDto.Day7,
                DayCounter = dailyBonusDto.DayCounter,
            };

            // Add the dailyBonus to the database
            _context.DailyBonus.Add(dailyBonus);
            _context.SaveChanges();

            // Return the created dailyBonus
            return CreatedAtAction(nameof(GetDailyBonusByPlayerId), new { id = dailyBonus.DailyBonusId }, dailyBonus);
        }


        // Update dailyBonus by Id
        [HttpPut("{id}")]
        public IActionResult UpdateDailyBonus(int id, [FromBody] DailyBonusDto updatedDailyBonusDto)
        {
            var dailyBonus = _context.DailyBonus.FirstOrDefault(d => d.DailyBonusId == id);

            if (dailyBonus == null)
            {
                return NotFound(new { Message = "Daily Bonus not found." });
            }

            dailyBonus.PlayerId = updatedDailyBonusDto.PlayerId;
            dailyBonus.Day1 = updatedDailyBonusDto.Day1;
            dailyBonus.Day2 = updatedDailyBonusDto.Day2;
            dailyBonus.Day3 = updatedDailyBonusDto.Day3;
            dailyBonus.Day4 = updatedDailyBonusDto.Day4;
            dailyBonus.Day5 = updatedDailyBonusDto.Day5;
            dailyBonus.Day6 = updatedDailyBonusDto.Day6;
            dailyBonus.Day7 = updatedDailyBonusDto.Day7;
            dailyBonus.DayCounter = updatedDailyBonusDto.DayCounter;

            _context.SaveChanges();

            return Ok(dailyBonus);
        }

        // Delete DailyBonus by Player Id
        [HttpDelete("{id}")]
        public IActionResult DeleteDailyBonus(int id)
        {
            var dailyBonus = _context.DailyBonus.FirstOrDefault(d => d.PlayerId == id);

            if (dailyBonus == null)
            {
                return NotFound(new { Message = "Daily Bonus not found." });
            }

            _context.DailyBonus.Remove(dailyBonus);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
