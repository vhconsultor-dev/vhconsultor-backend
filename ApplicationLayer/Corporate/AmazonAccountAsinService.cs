using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class AmazonAccountAsinService
{
    private readonly AmazonAccountAsinQueryRepository _queryRepository;

    public AmazonAccountAsinService(AmazonAccountAsinQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    /// <summary>
    /// Obtiene ASINs paginados de una cuenta Amazon con sus catálogos
    /// </summary>
    public async Task<PaginatedAsinResult> GetAsinsPaginatedAsync(AsinQueryFilters filters)
    {
        // Validar que la cuenta Amazon existe
        var accountExists = await _queryRepository.AmazonAccountExistsAsync(filters.AmazonAccountId);
        if (!accountExists)
        {
            throw new InvalidOperationException($"Amazon Account with ID {filters.AmazonAccountId} does not exist.");
        }

        // Validar paginación
        if (filters.PageNumber < 1)
            filters.PageNumber = 1;

        if (filters.PageSize < 1)
            filters.PageSize = 100;

        // Limitar el tamaño máximo de página para evitar sobrecarga
        if (filters.PageSize > 500)
            filters.PageSize = 500;

        return await _queryRepository.GetAsinsPaginatedAsync(filters);
    }
}
