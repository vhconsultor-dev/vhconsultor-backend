using ModelLayer.Security;
using BusinessLayer.Security.Queries;

namespace ApplicationLayer.Security;

public class AuthenticationService
{
    private readonly JwtService _jwtService;
    private readonly SystemCredentialsQueryRepository _systemCredentialsRepository;

    public AuthenticationService(
        JwtService jwtService, 
        SystemCredentialsQueryRepository systemCredentialsRepository)
    {
        _jwtService = jwtService;
        _systemCredentialsRepository = systemCredentialsRepository;
    }

    public async Task<LoginResponse?> AuthenticateAsync(LoginRequest loginRequest)
    {
        // Validar credenciales del sistema (tabla SystemCredentials)
        var systemCredentials = await _systemCredentialsRepository.GetSystemCredentialsAsync(
            loginRequest.Username, 
            loginRequest.Password, 
            loginRequest.SecurityKey);
        
        if (systemCredentials == null)
        {
            return null; // Credenciales inválidas
        }

        // Generar token JWT
        var token = _jwtService.GenerateToken(
            systemCredentials.User, 
            systemCredentials.ApplicationName, // Usar ApplicationName como rol
            0, // CompanyId = 0 para credenciales del sistema
            0  // BranchId = 0 para credenciales del sistema
        );
        var expiresAt = _jwtService.GetExpirationTime();

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = systemCredentials.User,
            Role = systemCredentials.ApplicationName,
            CompanyId = 0, // Credenciales del sistema
            BranchId = 0   // Credenciales del sistema
        };
    }
} 