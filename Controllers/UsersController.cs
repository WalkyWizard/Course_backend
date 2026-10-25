using Microsoft.AspNetCore.Mvc;
using SNovimGodom.Data;
using SNovimGodom.DTOs;
using SNovimGodom.Helpers;

namespace SNovimGodom.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var admin = AuthHelper.GetUser(Request, _db);
        if (admin == null || admin.Role != "Admin")
            return Unauthorized();

        var users = _db.Users
            .AsEnumerable()
            .Select(user => new
            {
                user.Id,
                user.Email,
                Name = user.Email.Split('@')[0],
                user.Role
            })
            .OrderBy(user => user.Email)
            .ToList();

        return Ok(users);
    }

    [HttpPut("{email}/role")]
    public IActionResult UpdateRole(string email, UpdateUserRoleDto dto)
    {
        var admin = AuthHelper.GetUser(Request, _db);
        if (admin == null || admin.Role != "Admin")
            return Unauthorized();

        var normalizedRole = dto.Role.Trim().ToLowerInvariant();
        var nextRole = normalizedRole switch
        {
            "admin" => "Admin",
            "user" => "User",
            _ => null
        };

        if (nextRole == null)
            return BadRequest("Invalid role");

        var user = _db.Users.FirstOrDefault(x => x.Email == email);
        if (user == null) return NotFound();

        user.Role = nextRole;
        _db.SaveChanges();

        return Ok(new
        {
            user.Id,
            user.Email,
            Name = user.Email.Split('@')[0],
            user.Role
        });
    }

    [HttpDelete("{email}")]
    public IActionResult Delete(string email)
    {
        var admin = AuthHelper.GetUser(Request, _db);
        if (admin == null || admin.Role != "Admin")
            return Unauthorized();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var currentUserEmail = admin.Email.Trim().ToLowerInvariant();

        if (normalizedEmail == currentUserEmail)
            return BadRequest("You cannot delete yourself");

        var user = _db.Users.FirstOrDefault(x => x.Email == email);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        _db.SaveChanges();

        return NoContent();
    }
}
