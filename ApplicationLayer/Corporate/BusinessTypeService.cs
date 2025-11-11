using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class BusinessTypeService
{
    private readonly BusinessTypeQueryRepository _queryRepository;

    public BusinessTypeService(BusinessTypeQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IEnumerable<dynamic>> GetBusinessTypesAsync(
        int? businessTypeId = null,
        string? businessTypeName = null,
        string? businessTypeKey = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetBusinessTypesAsync(
            businessTypeId, businessTypeName, businessTypeKey, isActive);
    }

    public async Task<dynamic?> GetBusinessTypeByIdAsync(int id)
    {
        return await _queryRepository.GetBusinessTypeByIdAsync(id);
    }
}

