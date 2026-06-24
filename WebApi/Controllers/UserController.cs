using System.Net.Http.Headers;
using Application.Services;


namespace Application.Services;

public class UserController
{
    private readonly UserService _userService;
    public UserController(UserService userService)
    {
        _userService = userService;
    }



}