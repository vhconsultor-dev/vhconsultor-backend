using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Amazon.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Amazon.Queries;

/// <summary>
/// Repositorio de consultas para tokens de Amazon
/// </summary>
public class AmazonTokenQueryRepository
{
    private readonly DBcontext _context;

    public AmazonTokenQueryRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene el token activo más reciente
    /// </summary>
    public async Task<AmazonToken?> GetActiveTokenAsync()
    {
        var now = DateTimeService.GetCostaRicaNow();
        return await _context.AmazonTokens
            .Where(t => t.IsActive && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Obtiene un token por ID
    /// </summary>
    public async Task<AmazonToken?> GetByIdAsync(int tokenId)
    {
        return await _context.AmazonTokens
            .FirstOrDefaultAsync(t => t.TokenId == tokenId);
    }

    /// <summary>
    /// Obtiene todos los tokens con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<AmazonToken>> GetTokensAsync(
        int? tokenId = null,
        string? clientId = null,
        bool? isActive = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int? limit = 50)
    {
        var query = _context.AmazonTokens.AsQueryable();

        if (tokenId.HasValue)
            query = query.Where(t => t.TokenId == tokenId.Value);

        if (!string.IsNullOrEmpty(clientId))
            query = query.Where(t => t.ClientId == clientId);

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        if (createdFrom.HasValue)
            query = query.Where(t => t.CreatedAt >= createdFrom.Value);

        if (createdTo.HasValue)
            query = query.Where(t => t.CreatedAt <= createdTo.Value);

        query = query.OrderByDescending(t => t.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }
}
