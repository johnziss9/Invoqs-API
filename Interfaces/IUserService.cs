using Invoqs.API.DTOs;

namespace Invoqs.API.Services;

public interface IUserService
{
    Task<AuthResponseDTO?> LoginAsync(LoginUserDTO loginDto);
    Task<UserDTO?> GetUserByIdAsync(int userId);
}
