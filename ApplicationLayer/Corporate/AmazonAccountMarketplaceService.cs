using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Application service for Amazon Account–Marketplace associations (GET, POST, PUT, DELETE).
/// </summary>
public class AmazonAccountMarketplaceService
{
    private readonly AmazonAccountMarketplaceQueryRepository _queryRepository;
    private readonly CreateAmazonAccountMarketplaceCommand _createCommand;
    private readonly UpdateAmazonAccountMarketplaceCommand _updateCommand;
    private readonly DeleteAmazonAccountMarketplaceCommand _deleteCommand;

    public AmazonAccountMarketplaceService(
        AmazonAccountMarketplaceQueryRepository queryRepository,
        CreateAmazonAccountMarketplaceCommand createCommand,
        UpdateAmazonAccountMarketplaceCommand updateCommand,
        DeleteAmazonAccountMarketplaceCommand deleteCommand)
    {
        _queryRepository = queryRepository;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
        _deleteCommand = deleteCommand;
    }

    /// <summary>
    /// Gets account–marketplace associations with optional filters. All filters use exact match.
    /// </summary>
    public async Task<IEnumerable<AmazonAccountMarketplace>> GetAmazonAccountMarketplacesAsync(
        int? id = null,
        int? amazonAccountId = null,
        int? amazonMarketplaceId = null,
        bool? isPrimary = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetAmazonAccountMarketplacesAsync(
            id, amazonAccountId, amazonMarketplaceId, isPrimary, isActive);
    }

    /// <summary>
    /// Creates a new association. Throws InvalidOperationException if account or marketplace does not exist, or if the pair is already linked.
    /// </summary>
    public async Task<int> CreateAsync(CreateAmazonAccountMarketplaceRequest request)
    {
        return await _createCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Updates an existing association by ID. Returns false if not found.
    /// </summary>
    public async Task<bool> UpdateAsync(int amazonAccountMarketplaceId, UpdateAmazonAccountMarketplaceRequest request)
    {
        return await _updateCommand.ExecuteAsync(amazonAccountMarketplaceId, request);
    }

    /// <summary>
    /// Deletes an association by ID. Returns false if not found.
    /// </summary>
    public async Task<bool> DeleteAsync(int amazonAccountMarketplaceId)
    {
        return await _deleteCommand.ExecuteAsync(amazonAccountMarketplaceId);
    }
}
