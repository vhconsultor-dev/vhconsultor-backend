using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class PlatformService
{
    private readonly PlatformQueryRepository _queryRepository;

    public PlatformService(PlatformQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IEnumerable<dynamic>> GetPlatformsAsync(
        int? platformId = null,
        int? businessTypeId = null,
        string? platformName = null,
        string? platformKey = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetPlatformsAsync(
            platformId, businessTypeId, platformName, platformKey, isActive);
    }

    public async Task<dynamic?> GetPlatformByIdAsync(int id)
    {
        return await _queryRepository.GetPlatformByIdAsync(id);
    }
}

