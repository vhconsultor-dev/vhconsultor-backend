using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Application service for Amazon Accounts maintenance (GET, POST, PUT).
/// </summary>
public class AmazonAccountService
{
    private readonly AmazonAccountQueryRepository _queryRepository;
    private readonly CreateAmazonAccountCommand _createCommand;
    private readonly UpdateAmazonAccountCommand _updateCommand;

    public AmazonAccountService(
        AmazonAccountQueryRepository queryRepository,
        CreateAmazonAccountCommand createCommand,
        UpdateAmazonAccountCommand updateCommand)
    {
        _queryRepository = queryRepository;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
    }

    /// <summary>
    /// Gets Amazon accounts with optional filters. String filters use partial match (LIKE).
    /// </summary>
    public async Task<IEnumerable<AmazonAccount>> GetAmazonAccountsAsync(
        int? id = null,
        int? customerId = null,
        string? amazonAccountIdentifier = null,
        bool? isSeller = null,
        bool? isVendor = null,
        string? amazonRegion = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetAmazonAccountsAsync(
            id, customerId, amazonAccountIdentifier, isSeller, isVendor, amazonRegion, isActive);
    }

    /// <summary>
    /// Gets a single Amazon account by ID.
    /// </summary>
    public async Task<AmazonAccount?> GetByIdAsync(int amazonAccountId)
    {
        return await _queryRepository.GetByIdAsync(amazonAccountId);
    }

    /// <summary>
    /// Creates a new Amazon account. Throws InvalidOperationException if CustomerId does not exist.
    /// </summary>
    public async Task<int> CreateAsync(CreateAmazonAccountRequest request)
    {
        return await _createCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Updates an existing Amazon account. Throws InvalidOperationException if CustomerId does not exist.
    /// </summary>
    public async Task<bool> UpdateAsync(int amazonAccountId, UpdateAmazonAccountRequest request)
    {
        return await _updateCommand.ExecuteAsync(amazonAccountId, request);
    }
}
