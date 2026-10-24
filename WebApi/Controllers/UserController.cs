using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    // GET: api/user
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(ct);
        return Ok(users.Select(ToDto));
    }

    // GET: api/user/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(ToDto(user));
    }

    // GET: api/user/email/{email}
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken ct)
    {
        var user = await _userService.GetByEmailAsync(email, ct);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(ToDto(user));
    }

    // POST: api/user
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user, CancellationToken ct)
    {
        var existing = await _userService.GetByEmailAsync(user.Email, ct);
        if (existing != null)
        {
            return Conflict("Ya existe un usuario con ese email.");
        }

        var created = await _userService.AddAsync(user, ct);
        return CreatedAtAction(nameof(GetUser), new { id = created!.Id }, ToDto(created));
    }

    private static UserResponseDto ToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.IsActive);

    // PUT: api/user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user, CancellationToken ct)
    {
        if (id != user.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del usuario.");
        }

        var existing = await _userService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _userService.UpdateAsync(user, ct);
        return NoContent();
    }

    // DELETE: api/user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var existing = await _userService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _userService.DeleteAsync(id, ct);
        return NoContent();
    }
}
