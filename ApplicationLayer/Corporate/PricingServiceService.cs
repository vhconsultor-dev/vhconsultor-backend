using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class PricingServiceService
{
    private readonly PricingServiceQueryRepository _queryRepository;

    public PricingServiceService(PricingServiceQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IEnumerable<dynamic>> GetPricingServicesAsync(
        int? serviceId = null,
        string? serviceName = null,
        string? serviceKey = null,
        string? serviceCategory = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetPricingServicesAsync(
            serviceId, serviceName, serviceKey, serviceCategory, isActive);
    }

    public async Task<dynamic?> GetPricingServiceByIdAsync(int id)
    {
        return await _queryRepository.GetPricingServiceByIdAsync(id);
    }

    public async Task<dynamic?> GetPricingServiceByKeyAsync(string serviceKey)
    {
        return await _queryRepository.GetPricingServiceByKeyAsync(serviceKey);
    }
}
