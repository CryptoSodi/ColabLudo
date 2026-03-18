using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LudoServer.Data;
using LudoServer.Models;

namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public ChatMessagesController(LudoDbContext context)
        {
            _context = context;
        }

        // GET: api/ChatMessages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatMessageDTO>>> GetAll([FromQuery] int? senderId, [FromQuery] int? receiverId)
        {
            var query = _context.ChatMessages.AsQueryable();

            if (senderId.HasValue)
                query = query.Where(e => e.SenderId == senderId.Value);

            if (receiverId.HasValue)
                query = query.Where(e => e.ReceiverId == receiverId.Value);

            var dtos = await query
                .OrderBy(e => e.CreatedDate)
                .Select(e => new ChatMessageDTO
                {
                    SenderId = e.SenderId,
                    SenderName = e.SenderName,
                    SenderColor = e.SenderColor,
                    SenderPicture = e.SenderPicture,
                    ReceiverId = e.ReceiverId,
                    ReceiverName = e.ReceiverName,
                    Message = e.Message,
                    CreatedDate = e.CreatedDate
                })
                .ToListAsync();

            return Ok(dtos);
        }

        // GET: api/ChatMessages/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ChatMessageDTO>> GetById(int id)
        {
            var e = await _context.ChatMessages.FindAsync(id);
            if (e == null)
                return NotFound();

            var dto = new ChatMessageDTO
            {
                SenderId = e.SenderId,
                SenderName = e.SenderName,
                SenderColor = e.SenderColor,
                SenderPicture = e.SenderPicture,
                ReceiverId = e.ReceiverId,
                ReceiverName = e.ReceiverName,
                Message = e.Message,
                CreatedDate = e.CreatedDate
            };

            return Ok(dto);
        }

        // POST: api/ChatMessages
        [HttpPost]
        public async Task<ActionResult<ChatMessageDTO>> Create([FromBody] ChatMessageDTO dto)
        {
            ChatMessage entity = new ChatMessage
            {
                SenderId = dto.SenderId,
                SenderName = dto.SenderName,
                SenderColor = dto.SenderColor,
                SenderPicture = dto.SenderPicture,
                ReceiverId = dto.ReceiverId,
                ReceiverName = dto.ReceiverName,
                Message = dto.Message,
                CreatedDate = dto.CreatedDate
            };

            _context.ChatMessages.Add(entity);
            await _context.SaveChangesAsync();

            var createdDto = new ChatMessageDTO
            {
                SenderId = entity.SenderId,
                SenderName = entity.SenderName,
                SenderColor = entity.SenderColor,
                SenderPicture = entity.SenderPicture,
                ReceiverId = entity.ReceiverId,
                ReceiverName = entity.ReceiverName,
                Message = entity.Message,
                CreatedDate = entity.CreatedDate
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Index }, createdDto);
        }

        // PUT: api/ChatMessages/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ChatMessageDTO dto)
        {
            var entity = await _context.ChatMessages.FindAsync(id);
            if (entity == null)
                return NotFound();

            entity.SenderId = dto.SenderId;
            entity.SenderName = dto.SenderName;
            entity.SenderColor = dto.SenderColor;
            entity.SenderPicture = dto.SenderPicture;
            entity.ReceiverId = dto.ReceiverId;
            entity.ReceiverName = dto.ReceiverName;
            entity.Message = dto.Message;
            entity.CreatedDate = dto.CreatedDate;

            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/ChatMessages/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.ChatMessages.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.ChatMessages.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}