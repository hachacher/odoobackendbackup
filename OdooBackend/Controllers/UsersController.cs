using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdooBackend;
using OdooBackend.Models;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly OdooDBContext _context;

    public UsersController(OdooDBContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        if (user == null)
            return BadRequest();
            
        // Check if user with the same username and location already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => 
            u.UserName == user.UserName && u.Location == user.Location);
        if (existingUser != null)
            return Conflict(new { message = "A user with this username at this location already exists" });
            
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
    {
        if (user == null || user.Id != id)
            return BadRequest();
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
            return NotFound();
        existingUser.UserName = user.UserName;
        existingUser.Location = user.Location;
        existingUser.Type = user.Type;
        // Add other fields as needed
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpGet("nonmanagers")]
    public async Task<IActionResult> GetUsersByLocation([FromQuery] string location)
    {
        var users = await _context.Users
            .Where(u => u.Type != 1 && u.Location == location)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Location
            })
            .ToListAsync();

        return Ok(users);
    }
    
    [HttpGet("by-location")]
    public async Task<IActionResult> GetAllUsersByLocation([FromQuery] string location)
    {
        if (string.IsNullOrEmpty(location))
            return BadRequest(new { message = "Location parameter is required" });
            
        var users = await _context.Users
            .Where(u => u.Location == location)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Location,
                u.Type,
                u.Inactive,
                IsManager = u.Type == 1
            })
            .ToListAsync();

        return Ok(users);
    }
    
    [HttpPatch("toggle-status")]
    public async Task<IActionResult> ToggleUserStatus([FromBody] UserToggleRequest request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Location))
            return BadRequest(new { message = "Username and location are required" });
            
        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.UserName == request.UserName && u.Location == request.Location);
            
        if (user == null)
            return NotFound(new { message = "User not found with the specified username and location" });
            
        // Toggle the inactive status
        user.Inactive = !user.Inactive;
        await _context.SaveChangesAsync();
        
        return Ok(new { 
            message = $"User status updated successfully", 
            userName = user.UserName,
            location = user.Location,
            isActive = !user.Inactive 
        });
    }
}
