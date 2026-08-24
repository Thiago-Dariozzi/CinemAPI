using System;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Services;
using Domain.Entities;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    // GET: api/user
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAll();
        return Ok(users.Select(ToDto));
    }

    // GET: api/user/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(ToDto(user));
    }

    // GET: api/user/email/{email}
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var user = await _userService.GetByEmail(email);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(ToDto(user));
    }

    // POST: api/user
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        var existing = await _userService.GetByEmail(user.Email);
        if (existing != null)
        {
            return Conflict("Ya existe un usuario con ese email.");
        }

        var created = await _userService.Add(user);
        return CreatedAtAction(nameof(GetUser), new { id = created!.Id }, ToDto(created));
    }

    private static UserResponseDto ToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.IsActive);

    // PUT: api/user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user)
    {
        if (id != user.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del usuario.");
        }

        var existing = await _userService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _userService.Update(user);
        return NoContent();
    }

    // DELETE: api/user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var existing = await _userService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _userService.Delete(id);
        return NoContent();
    }
}