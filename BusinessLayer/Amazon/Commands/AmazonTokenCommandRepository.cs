using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Amazon.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Amazon.Commands;

/// <summary>
/// Repositorio de comandos para tokens de Amazon
/// </summary>
public class AmazonTokenCommandRepository
{
    private readonly DBcontext _context;

    public AmazonTokenCommandRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Guarda un nuevo token de acceso de Amazon
    /// </summary>
    public async Task<int> SaveTokenAsync(AmazonToken token)
    {
        try
        {
            // Desactivar tokens anteriores del mismo cliente
            if (!string.IsNullOrEmpty(token.ClientId))
            {
                try
                {
                    var existingTokens = await _context.AmazonTokens
                        .Where(t => t.ClientId == token.ClientId && t.IsActive)
                        .ToListAsync();

                    foreach (var existingToken in existingTokens)
                    {
                        existingToken.IsActive = false;
                    }
                }
                catch (Exception)
                {
                    // Si la tabla no existe, continuar sin desactivar tokens anteriores
                }
            }

            // Agregar el nuevo token
            _context.AmazonTokens.Add(token);
            await _context.SaveChangesAsync();

            return token.TokenId;
        }
        catch (Exception)
        {
            // Si hay cualquier error de BD (tabla no existe, etc.), lanzar excepción
            // para que el servicio la maneje y continúe sin guardar
            throw;
        }
    }

    /// <summary>
    /// Actualiza un token existente
    /// </summary>
    public async Task<bool> UpdateTokenAsync(AmazonToken token)
    {
        _context.AmazonTokens.Update(token);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    /// <summary>
    /// Desactiva tokens expirados
    /// </summary>
    public async Task<int> DeactivateExpiredTokensAsync()
    {
        var now = DateTimeService.GetCostaRicaNow();
        var expiredTokens = await _context.AmazonTokens
            .Where(t => t.IsActive && t.ExpiresAt <= now)
            .ToListAsync();

        foreach (var token in expiredTokens)
        {
            token.IsActive = false;
        }

        return await _context.SaveChangesAsync();
    }
}
