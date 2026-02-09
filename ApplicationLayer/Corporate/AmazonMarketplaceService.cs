using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Application service for Amazon Marketplaces maintenance (GET, POST, PUT).
/// </summary>
public class AmazonMarketplaceService
{
    private readonly AmazonMarketplaceQueryRepository _queryRepository;
    private readonly CreateAmazonMarketplaceCommand _createCommand;
    private readonly UpdateAmazonMarketplaceCommand _updateCommand;

    public AmazonMarketplaceService(
        AmazonMarketplaceQueryRepository queryRepository,
        CreateAmazonMarketplaceCommand createCommand,
        UpdateAmazonMarketplaceCommand updateCommand)
    {
        _queryRepository = queryRepository;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
    }

    /// <summary>
    /// Gets marketplaces with optional filters. String filters use partial match (LIKE).
    /// </summary>
    public async Task<IEnumerable<AmazonMarketplace>> GetAmazonMarketplacesAsync(
        int? id = null,
        string? amazonMarketplaceCode = null,
        string? countryCode = null,
        string? countryName = null,
        string? amazonRegion = null,
        string? currencyCode = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetAmazonMarketplacesAsync(
            id, amazonMarketplaceCode, countryCode, countryName, amazonRegion, currencyCode, isActive);
    }

    /// <summary>
    /// Gets a single marketplace by ID.
    /// </summary>
    public async Task<AmazonMarketplace?> GetByIdAsync(int amazonMarketplaceId)
    {
        return await _queryRepository.GetByIdAsync(amazonMarketplaceId);
    }

    /// <summary>
    /// Creates a new Amazon marketplace.
    /// </summary>
    /// <returns>ID of the created marketplace.</returns>
    public async Task<int> CreateAsync(CreateAmazonMarketplaceRequest request)
    {
        return await _createCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Updates an existing Amazon marketplace.
    /// </summary>
    /// <returns>True if updated, false if not found.</returns>
    public async Task<bool> UpdateAsync(int amazonMarketplaceId, UpdateAmazonMarketplaceRequest request)
    {
        return await _updateCommand.ExecuteAsync(amazonMarketplaceId, request);
    }
}
