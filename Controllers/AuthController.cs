using Microsoft.AspNetCore.Mvc;
using SNovimGodom.Data;
using SNovimGodom.Models;
using SNovimGodom.DTOs;

namespace SNovimGodom.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterDto dto)
    {
        if (_db.Users.Any(x => x.Email == dto.Email))
            return BadRequest("User exists");

        var user = new User
        {
            Email = dto.Email,
            Password = dto.Password,
            Role = "User"
        };

        _db.Users.Add(user);
        _db.SaveChanges();

        return Ok(user);
    }

    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _db.Users.FirstOrDefault(x =>
            x.Email == dto.Email && x.Password == dto.Password);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            token = user.Email,
            role = user.Role
        });
    }
}