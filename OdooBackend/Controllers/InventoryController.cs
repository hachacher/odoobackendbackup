using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdooBackend.Models;

namespace OdooBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly OdooDBContext _context;
 
        public InventoryController(OdooDBContext context)
        {
            _context = context;
        }

        [HttpPost("postsession")]
        public async Task<IActionResult> PostInventorySession([FromBody] InventorySessionDto sessionDto)
        {
            try
            {

                //var x = await _context.InventoryEntries.ToListAsync();
                if (sessionDto == null || sessionDto.Entries == null || !sessionDto.Entries.Any())
                    return BadRequest("Invalid session or no entries.");

                var session = new InventorySession
                {
                    UserId = sessionDto.UserId,
                    Location = sessionDto.Location,
                    StartDate = sessionDto.StartDate,
                    EndDate = sessionDto.EndDate,
                    IsPosted = sessionDto.IsPosted,
                    SessionName = sessionDto.SessionName
                };

                _context.InventorySessions.Add(session);
                await _context.SaveChangesAsync();

                foreach (var entryDto in sessionDto.Entries)
                {
                    var entry = new InventoryEntry
                    {
                        SessionId = session.Id,
                        Barcode = entryDto.Barcode,
                        Quantity = entryDto.Quantity,
                        ScannedAt = entryDto.ScannedAt,
                        Comment = entryDto.Comment 
                    };

                    _context.InventoryEntries.Add(entry);
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true, sessionId = session.Id });
            }
            catch(Exception e)
            {
                return BadRequest("Invalid session or no entries.");
            }
            
        }

        [HttpGet("unposted-sessions")]
        public async Task<IActionResult> GetUnpostedSessionsByLocation([FromQuery] string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest("Location is required.");

            var sessions = await _context.InventorySessions
                .Where(s => s.Location == location && !s.IsPosted)
                .Include(s => s.User)
                .Select(s => new 
                {
                    s.Id,
                    s.UserId,
                    User = new 
                    {
                        s.User.Id,
                        s.User.UserName 
                    },
                    s.Location,
                    s.StartDate,
                    s.EndDate,
                    s.IsPosted,
                    s.SessionName
                })
                .ToListAsync();

            return Ok(sessions);
        }
    }

    public class InventorySessionDto
    {
        public int UserId { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPosted { get; set; }
        public string? SessionName { get; set; }
        public List<InventoryEntryDto> Entries { get; set; } = new();
    }

    public class InventoryEntryDto
    {
        public string? Barcode { get; set; }
        public int Quantity { get; set; }
        public DateTime ScannedAt { get; set; }
        public string? Comment { get; set; }
    }

}

