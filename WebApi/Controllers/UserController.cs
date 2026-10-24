using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        return Ok(users.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        return Ok(ToDto(await userService.GetByIdAsync(id, ct)));
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken ct)
    {
        return Ok(ToDto(await userService.GetByEmailAsync(email, ct)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user, CancellationToken ct)
    {
        var created = await userService.AddAsync(user, ct);
        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user, CancellationToken ct)
    {
        if (id != user.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del usuario.");
        }

        await userService.UpdateAsync(user, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static UserResponseDto ToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.IsActive);
}
