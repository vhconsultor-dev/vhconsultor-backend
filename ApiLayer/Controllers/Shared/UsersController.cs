using Microsoft.AspNetCore.Mvc;
using ApplicationLayer.Security;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    #region Queries

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? email = null, [FromQuery] string? sippUser = null)
    {
        try
        {
            var users = await _userService.GetUsersAsync(email, sippUser);
            var response = ResponseStructure<IEnumerable<ModelLayer.Security.Entities.User>>.Success(users, "Usuarios obtenidos exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = ResponseStructure<object>.Error($"Error al obtener usuarios: {ex.Message}");
            return BadRequest(response);
        }
    }

    #endregion
} 