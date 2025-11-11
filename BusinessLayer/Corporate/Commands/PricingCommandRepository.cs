using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class PricingCommandRepository
{
    private readonly DBcontext _context;

    public PricingCommandRepository(DBcontext context)
    {
        _context = context;
    }

    // =====================================================
    // ServiceBudgetRange Commands
    // =====================================================

    public async Task<int> CreateServiceBudgetRangeAsync(ServiceBudgetRange range)
    {
        range.CreatedAt = DateTimeService.GetCostaRicaNow();
        _context.ServiceBudgetRanges.Add(range);
        await _context.SaveChangesAsync();
        return range.ServiceBudgetRangeId;
    }

    public async Task UpdateServiceBudgetRangeAsync(int id, UpdateServiceBudgetRangeCommand command)
    {
        var range = await _context.ServiceBudgetRanges.FindAsync(id);
        if (range == null)
            throw new KeyNotFoundException($"ServiceBudgetRange con ID {id} no encontrado");

        if (command.ServiceId.HasValue)
            range.ServiceId = command.ServiceId.Value;
        if (command.BusinessTypeId.HasValue)
            range.BusinessTypeId = command.BusinessTypeId.Value;
        if (command.PlatformId.HasValue)
            range.PlatformId = command.PlatformId.Value;
        if (command.MinBudgetValue.HasValue)
            range.MinBudgetValue = command.MinBudgetValue.Value;
        if (command.MaxBudgetValue.HasValue)
            range.MaxBudgetValue = command.MaxBudgetValue.Value;
        if (command.Percentage.HasValue)
            range.Percentage = command.Percentage.Value;
        if (command.IsActive.HasValue)
            range.IsActive = command.IsActive.Value;

        range.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task DeleteServiceBudgetRangeAsync(int id)
    {
        var range = await _context.ServiceBudgetRanges.FindAsync(id);
        if (range == null)
            throw new KeyNotFoundException($"ServiceBudgetRange con ID {id} no encontrado");

        range.IsActive = false;
        range.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    // =====================================================
    // ServiceAdBudgetRange Commands
    // =====================================================

    public async Task<int> CreateServiceAdBudgetRangeAsync(ServiceAdBudgetRange range)
    {
        range.CreatedAt = DateTimeService.GetCostaRicaNow();
        _context.ServiceAdBudgetRanges.Add(range);
        await _context.SaveChangesAsync();
        return range.ServiceAdBudgetRangeId;
    }

    public async Task UpdateServiceAdBudgetRangeAsync(int id, UpdateServiceAdBudgetRangeCommand command)
    {
        var range = await _context.ServiceAdBudgetRanges.FindAsync(id);
        if (range == null)
            throw new KeyNotFoundException($"ServiceAdBudgetRange con ID {id} no encontrado");

        if (command.ServiceId.HasValue)
            range.ServiceId = command.ServiceId.Value;
        if (command.BusinessTypeId.HasValue)
            range.BusinessTypeId = command.BusinessTypeId.Value;
        if (command.PlatformId.HasValue)
            range.PlatformId = command.PlatformId.Value;
        if (command.MinAdBudgetValue.HasValue)
            range.MinAdBudgetValue = command.MinAdBudgetValue.Value;
        if (command.MaxAdBudgetValue.HasValue)
            range.MaxAdBudgetValue = command.MaxAdBudgetValue.Value;
        if (command.FixedQuote.HasValue)
            range.FixedQuote = command.FixedQuote.Value;
        if (command.IsActive.HasValue)
            range.IsActive = command.IsActive.Value;

        range.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task DeleteServiceAdBudgetRangeAsync(int id)
    {
        var range = await _context.ServiceAdBudgetRanges.FindAsync(id);
        if (range == null)
            throw new KeyNotFoundException($"ServiceAdBudgetRange con ID {id} no encontrado");

        range.IsActive = false;
        range.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    // =====================================================
    // Validation Helpers
    // =====================================================

    public async Task<bool> ServiceExistsAsync(int serviceId)
    {
        return await _context.PricingServices.AnyAsync(s => s.ServiceId == serviceId && s.IsActive);
    }

    public async Task<bool> BusinessTypeExistsAsync(int businessTypeId)
    {
        // Asumiendo que existe una tabla BusinessTypes en Corporate
        // Si no existe, esta validación se puede hacer en el query repository
        return true; // Placeholder - implementar cuando exista la entidad BusinessType
    }

    public async Task<bool> PlatformExistsAsync(int platformId)
    {
        // Asumiendo que existe una tabla Platforms en Corporate
        // Si no existe, esta validación se puede hacer en el query repository
        return true; // Placeholder - implementar cuando exista la entidad Platform
    }

    public async Task<bool> HasOverlappingBudgetRangeAsync(int serviceId, int businessTypeId, int? platformId, 
        decimal minBudget, decimal? maxBudget, int? excludeId = null)
    {
        var query = _context.ServiceBudgetRanges
            .Where(r => r.ServiceId == serviceId 
                && r.BusinessTypeId == businessTypeId 
                && r.PlatformId == platformId
                && r.IsActive);

        if (excludeId.HasValue)
            query = query.Where(r => r.ServiceBudgetRangeId != excludeId.Value);

        var existingRanges = await query.ToListAsync();

        foreach (var existing in existingRanges)
        {
            // Verificar solapamiento
            bool overlaps = (minBudget < (existing.MaxBudgetValue ?? decimal.MaxValue) || !existing.MaxBudgetValue.HasValue)
                && ((maxBudget ?? decimal.MaxValue) > existing.MinBudgetValue || !maxBudget.HasValue);

            if (overlaps)
                return true;
        }

        return false;
    }

    public async Task<bool> HasOverlappingAdBudgetRangeAsync(int serviceId, int businessTypeId, int? platformId, 
        decimal minAdBudget, decimal? maxAdBudget, int? excludeId = null)
    {
        var query = _context.ServiceAdBudgetRanges
            .Where(r => r.ServiceId == serviceId 
                && r.BusinessTypeId == businessTypeId 
                && r.PlatformId == platformId
                && r.IsActive);

        if (excludeId.HasValue)
            query = query.Where(r => r.ServiceAdBudgetRangeId != excludeId.Value);

        var existingRanges = await query.ToListAsync();

        foreach (var existing in existingRanges)
        {
            // Verificar solapamiento
            bool overlaps = (minAdBudget < (existing.MaxAdBudgetValue ?? decimal.MaxValue) || !existing.MaxAdBudgetValue.HasValue)
                && ((maxAdBudget ?? decimal.MaxValue) > existing.MinAdBudgetValue || !maxAdBudget.HasValue);

            if (overlaps)
                return true;
        }

        return false;
    }
}

