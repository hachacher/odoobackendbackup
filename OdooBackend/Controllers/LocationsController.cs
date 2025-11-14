using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdooBackend.Models;
using System.Threading.Tasks;
using System.Linq;

namespace OdooBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly OdooDBContext _context;

        public LocationsController(OdooDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var locations = await _context.Locations.ToListAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();
            return Ok(location);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Location  location)
        {
            if (location == null)
                return BadRequest();
            // Ensure Id is not set by client, as it is identity/auto-increment
            location.Id = 0;
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = location.Id }, location);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Location  location)
        {
            if (location == null || location.Id != id)
                return BadRequest();
            var existing = await _context.Locations.FindAsync(id);
            if (existing == null)
                return NotFound();
            existing = location;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();
            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
