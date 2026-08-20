using Domain.Entities;
using Application.Services;
using Microsoft.AspNetCore.Mvc;


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


    [HttpGet]
    public async Task<IActionResult>GetAll()
    {
        return Ok(await _userService.GetAll());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult>GetById(Guid Id)
    {
     var user =await _userService.GetById(Id);
        if (user != null){
            return Ok (user);
        }
        else
        {
            return NotFound();
            
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult>GetByEmail(string email)
    {
        var user = await _userService.GetByEmail(email);
        if (user != null)
        {
            return Ok(user);
        }
        else
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody]User user)
    {
        var created = await _userService.Add(user);
        return Ok(created);
        
    }

    [HttpPut("{id}")]
    
    public async Task<IActionResult>UpdateUser(Guid id, [FromBody] User user)
    {
       await _userService.Update(user);
       return NoContent();
        
    }






}