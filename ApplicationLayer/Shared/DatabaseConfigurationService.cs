using Microsoft.Extensions.Options;
using BusinessLayer.Shared;

namespace ApplicationLayer.Shared;

public interface IDatabaseConfigurationService
{
    string GetCurrentDatabaseType();
    bool IsUsingLegacyDatabase();
}

public class DatabaseConfigurationService : IDatabaseConfigurationService
{
    private readonly DatabaseSettings _databaseSettings;

    public DatabaseConfigurationService(IOptions<DatabaseSettings> databaseSettings)
    {
        _databaseSettings = databaseSettings.Value;
    }

    public string GetCurrentDatabaseType()
    {
        return _databaseSettings.UseLegacyDatabase ? "Legacy" : "New";
    }

    public bool IsUsingLegacyDatabase()
    {
        return _databaseSettings.UseLegacyDatabase;
    }


} 