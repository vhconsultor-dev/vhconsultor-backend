using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de queries para historial de login Brand Partner
/// </summary>
public class BrandPartnerLoginHistoryQueryRepository
{
    private readonly DBcontext _context;

    public BrandPartnerLoginHistoryQueryRepository(DBcontext context)
    {
        _context = context;
    }

    public async Task<List<BrandPartnerUserLoginHistory>> GetLoginHistoryAsync(
        int? brandPartnerUserId = null,
        DateTime? loginDateFrom = null,
        DateTime? loginDateTo = null,
        string? ipAddress = null,
        string? country = null,
        string? city = null,
        bool? loginSuccessful = null,
        int? limit = 100)
    {
        var query = _context.BrandPartnerUserLoginHistory.AsQueryable();

        if (brandPartnerUserId.HasValue)
            query = query.Where(h => h.BrandPartnerUserId == brandPartnerUserId.Value);

        if (loginDateFrom.HasValue)
            query = query.Where(h => h.LoginDate >= loginDateFrom.Value);

        if (loginDateTo.HasValue)
            query = query.Where(h => h.LoginDate <= loginDateTo.Value);

        if (!string.IsNullOrWhiteSpace(ipAddress))
            query = query.Where(h => h.IPAddress == ipAddress);

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(h => h.Country != null && h.Country.ToLower().Contains(country.ToLower()));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(h => h.City != null && h.City.ToLower().Contains(city.ToLower()));

        if (loginSuccessful.HasValue)
            query = query.Where(h => h.LoginSuccessful == loginSuccessful.Value);

        query = query.OrderByDescending(h => h.LoginDate);

        if (limit.HasValue && limit.Value > 0)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }
}
