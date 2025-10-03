using BusinessLayer.Security.Queries;
using ModelLayer.Security.Entities;

namespace ApplicationLayer.Security;

public class UserService
{
    private readonly UserQueryRepository _userQueryRepository;

    public UserService(UserQueryRepository userQueryRepository)
    {
        _userQueryRepository = userQueryRepository;
    }

    #region Queries

    public async Task<IEnumerable<User>> GetUsersAsync(string? email = null, string? sippUser = null)
    {
        return await _userQueryRepository.GetUsersAsync(email, sippUser);
    }

    #endregion
} 