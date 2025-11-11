using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class PricingQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public PricingQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    // =====================================================
    // ServiceBudgetRange Queries
    // =====================================================

    public async Task<IEnumerable<dynamic>> GetServiceBudgetRangesAsync(
        int? serviceId = null,
        int? businessTypeId = null,
        int? platformId = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        var sql = new StringBuilder(@"
            SELECT 
                sbr.ServiceBudgetRangeId,
                sbr.ServiceId,
                s.ServiceName,
                sbr.BusinessTypeId,
                sbr.PlatformId,
                sbr.MinBudgetValue,
                sbr.MaxBudgetValue,
                sbr.Percentage,
                sbr.IsActive,
                sbr.CreatedAt,
                sbr.UpdatedAt
            FROM Corporate.ServiceBudgetRanges sbr
            INNER JOIN Corporate.Services s ON sbr.ServiceId = s.ServiceId
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (serviceId.HasValue)
        {
            sql.Append(" AND sbr.ServiceId = @ServiceId");
            parameters.Add("ServiceId", serviceId.Value);
        }

        if (businessTypeId.HasValue)
        {
            sql.Append(" AND sbr.BusinessTypeId = @BusinessTypeId");
            parameters.Add("BusinessTypeId", businessTypeId.Value);
        }

        if (platformId.HasValue)
        {
            sql.Append(" AND sbr.PlatformId = @PlatformId");
            parameters.Add("PlatformId", platformId.Value);
        }

        if (isActive.HasValue)
        {
            sql.Append(" AND sbr.IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        sql.Append(" ORDER BY sbr.MinBudgetValue ASC");

        return await connection.QueryAsync<dynamic>(sql.ToString(), parameters);
    }

    public async Task<dynamic?> GetServiceBudgetRangeByIdAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        var sql = @"
            SELECT 
                sbr.ServiceBudgetRangeId,
                sbr.ServiceId,
                s.ServiceName,
                sbr.BusinessTypeId,
                sbr.PlatformId,
                sbr.MinBudgetValue,
                sbr.MaxBudgetValue,
                sbr.Percentage,
                sbr.IsActive,
                sbr.CreatedAt,
                sbr.UpdatedAt
            FROM Corporate.ServiceBudgetRanges sbr
            INNER JOIN Corporate.Services s ON sbr.ServiceId = s.ServiceId
            WHERE sbr.ServiceBudgetRangeId = @Id";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
    }

    public async Task<dynamic?> FindBudgetRangeForCalculationAsync(
        int serviceId,
        int businessTypeId,
        int? platformId,
        decimal annualBudget)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        // Primero buscar con PlatformId específico si se proporciona
        if (platformId.HasValue)
        {
            var sqlWithPlatform = @"
                SELECT TOP 1
                    ServiceBudgetRangeId,
                    ServiceId,
                    BusinessTypeId,
                    PlatformId,
                    MinBudgetValue,
                    MaxBudgetValue,
                    Percentage
                FROM Corporate.ServiceBudgetRanges
                WHERE ServiceId = @ServiceId
                    AND BusinessTypeId = @BusinessTypeId
                    AND PlatformId = @PlatformId
                    AND IsActive = 1
                    AND @AnnualBudget >= MinBudgetValue
                    AND (@AnnualBudget < MaxBudgetValue OR MaxBudgetValue IS NULL)
                ORDER BY MinBudgetValue ASC";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlWithPlatform, 
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, PlatformId = platformId, AnnualBudget = annualBudget });

            if (result != null)
                return result;
        }

        // Si no se encuentra con PlatformId específico, buscar con PlatformId NULL
        var sqlWithoutPlatform = @"
            SELECT TOP 1
                ServiceBudgetRangeId,
                ServiceId,
                BusinessTypeId,
                PlatformId,
                MinBudgetValue,
                MaxBudgetValue,
                Percentage
            FROM Corporate.ServiceBudgetRanges
            WHERE ServiceId = @ServiceId
                AND BusinessTypeId = @BusinessTypeId
                AND PlatformId IS NULL
                AND IsActive = 1
                AND @AnnualBudget >= MinBudgetValue
                AND (@AnnualBudget < MaxBudgetValue OR MaxBudgetValue IS NULL)
            ORDER BY MinBudgetValue ASC";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(
            sqlWithoutPlatform, 
            new { ServiceId = serviceId, BusinessTypeId = businessTypeId, AnnualBudget = annualBudget });
    }

    // =====================================================
    // ServiceAdBudgetRange Queries
    // =====================================================

    public async Task<IEnumerable<dynamic>> GetServiceAdBudgetRangesAsync(
        int? serviceId = null,
        int? businessTypeId = null,
        int? platformId = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        var sql = new StringBuilder(@"
            SELECT 
                sabr.ServiceAdBudgetRangeId,
                sabr.ServiceId,
                s.ServiceName,
                sabr.BusinessTypeId,
                sabr.PlatformId,
                sabr.MinAdBudgetValue,
                sabr.MaxAdBudgetValue,
                sabr.FixedQuote,
                sabr.IsActive,
                sabr.CreatedAt,
                sabr.UpdatedAt
            FROM Corporate.ServiceAdBudgetRanges sabr
            INNER JOIN Corporate.Services s ON sabr.ServiceId = s.ServiceId
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (serviceId.HasValue)
        {
            sql.Append(" AND sabr.ServiceId = @ServiceId");
            parameters.Add("ServiceId", serviceId.Value);
        }

        if (businessTypeId.HasValue)
        {
            sql.Append(" AND sabr.BusinessTypeId = @BusinessTypeId");
            parameters.Add("BusinessTypeId", businessTypeId.Value);
        }

        if (platformId.HasValue)
        {
            sql.Append(" AND sabr.PlatformId = @PlatformId");
            parameters.Add("PlatformId", platformId.Value);
        }

        if (isActive.HasValue)
        {
            sql.Append(" AND sabr.IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        sql.Append(" ORDER BY sabr.MinAdBudgetValue ASC");

        return await connection.QueryAsync<dynamic>(sql.ToString(), parameters);
    }

    public async Task<dynamic?> GetServiceAdBudgetRangeByIdAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        var sql = @"
            SELECT 
                sabr.ServiceAdBudgetRangeId,
                sabr.ServiceId,
                s.ServiceName,
                sabr.BusinessTypeId,
                sabr.PlatformId,
                sabr.MinAdBudgetValue,
                sabr.MaxAdBudgetValue,
                sabr.FixedQuote,
                sabr.IsActive,
                sabr.CreatedAt,
                sabr.UpdatedAt
            FROM Corporate.ServiceAdBudgetRanges sabr
            INNER JOIN Corporate.Services s ON sabr.ServiceId = s.ServiceId
            WHERE sabr.ServiceAdBudgetRangeId = @Id";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
    }

    public async Task<dynamic?> FindAdBudgetRangeForCalculationAsync(
        int serviceId,
        int businessTypeId,
        int? platformId,
        decimal annualAdBudget)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        
        // Primero buscar con PlatformId específico si se proporciona
        if (platformId.HasValue)
        {
            var sqlWithPlatform = @"
                SELECT TOP 1
                    ServiceAdBudgetRangeId,
                    ServiceId,
                    BusinessTypeId,
                    PlatformId,
                    MinAdBudgetValue,
                    MaxAdBudgetValue,
                    FixedQuote
                FROM Corporate.ServiceAdBudgetRanges
                WHERE ServiceId = @ServiceId
                    AND BusinessTypeId = @BusinessTypeId
                    AND PlatformId = @PlatformId
                    AND IsActive = 1
                    AND @AnnualAdBudget >= MinAdBudgetValue
                    AND (@AnnualAdBudget < MaxAdBudgetValue OR MaxAdBudgetValue IS NULL)
                ORDER BY MinAdBudgetValue ASC";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlWithPlatform, 
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, PlatformId = platformId, AnnualAdBudget = annualAdBudget });

            if (result != null)
                return result;
        }

        // Si no se encuentra con PlatformId específico, buscar con PlatformId NULL
        var sqlWithoutPlatform = @"
            SELECT TOP 1
                ServiceAdBudgetRangeId,
                ServiceId,
                BusinessTypeId,
                PlatformId,
                MinAdBudgetValue,
                MaxAdBudgetValue,
                FixedQuote
            FROM Corporate.ServiceAdBudgetRanges
            WHERE ServiceId = @ServiceId
                AND BusinessTypeId = @BusinessTypeId
                AND PlatformId IS NULL
                AND IsActive = 1
                AND @AnnualAdBudget >= MinAdBudgetValue
                AND (@AnnualAdBudget < MaxAdBudgetValue OR MaxAdBudgetValue IS NULL)
            ORDER BY MinAdBudgetValue ASC";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(
            sqlWithoutPlatform, 
            new { ServiceId = serviceId, BusinessTypeId = businessTypeId, AnnualAdBudget = annualAdBudget });
    }

    // =====================================================
    // Calculate Pricing - Comprehensive Query
    // =====================================================

    public async Task<dynamic?> CalculatePricingAsync(
        int businessTypeId,
        int? platformId,
        int serviceId,
        decimal budgetAmount)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        // PASO 1: Buscar en ServiceBudgetRanges (porcentaje)
        dynamic? percentageRange = null;

        if (platformId.HasValue)
        {
            var sqlPercentageWithPlatform = @"
                SELECT TOP 1
                    'percentage' as CalculationType,
                    sbr.ServiceBudgetRangeId as RangeId,
                    sbr.ServiceId,
                    sbr.BusinessTypeId,
                    sbr.PlatformId,
                    sbr.MinBudgetValue as MinValue,
                    sbr.MaxBudgetValue as MaxValue,
                    sbr.Percentage,
                    NULL as FixedQuote,
                    s.ServiceName,
                    s.ServiceCode,
                    s.ServiceDescription
                FROM Corporate.ServiceBudgetRanges sbr
                INNER JOIN Corporate.Services s ON sbr.ServiceId = s.ServiceId
                WHERE sbr.ServiceId = @ServiceId
                    AND sbr.BusinessTypeId = @BusinessTypeId
                    AND sbr.PlatformId = @PlatformId
                    AND sbr.IsActive = 1
                    AND @BudgetAmount >= sbr.MinBudgetValue
                    AND (@BudgetAmount < sbr.MaxBudgetValue OR sbr.MaxBudgetValue IS NULL)
                ORDER BY sbr.MinBudgetValue ASC";

            percentageRange = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlPercentageWithPlatform,
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, PlatformId = platformId, BudgetAmount = budgetAmount });
        }

        if (percentageRange == null)
        {
            var sqlPercentageWithoutPlatform = @"
                SELECT TOP 1
                    'percentage' as CalculationType,
                    sbr.ServiceBudgetRangeId as RangeId,
                    sbr.ServiceId,
                    sbr.BusinessTypeId,
                    sbr.PlatformId,
                    sbr.MinBudgetValue as MinValue,
                    sbr.MaxBudgetValue as MaxValue,
                    sbr.Percentage,
                    NULL as FixedQuote,
                    s.ServiceName,
                    s.ServiceCode,
                    s.ServiceDescription
                FROM Corporate.ServiceBudgetRanges sbr
                INNER JOIN Corporate.Services s ON sbr.ServiceId = s.ServiceId
                WHERE sbr.ServiceId = @ServiceId
                    AND sbr.BusinessTypeId = @BusinessTypeId
                    AND sbr.PlatformId IS NULL
                    AND sbr.IsActive = 1
                    AND @BudgetAmount >= sbr.MinBudgetValue
                    AND (@BudgetAmount < sbr.MaxBudgetValue OR sbr.MaxBudgetValue IS NULL)
                ORDER BY sbr.MinBudgetValue ASC";

            percentageRange = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlPercentageWithoutPlatform,
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, BudgetAmount = budgetAmount });
        }

        if (percentageRange != null)
            return percentageRange;

        // PASO 3: Buscar en ServiceAdBudgetRanges (monto fijo)
        dynamic? fixedRange = null;

        if (platformId.HasValue)
        {
            var sqlFixedWithPlatform = @"
                SELECT TOP 1
                    'fixed' as CalculationType,
                    sabr.ServiceAdBudgetRangeId as RangeId,
                    sabr.ServiceId,
                    sabr.BusinessTypeId,
                    sabr.PlatformId,
                    sabr.MinAdBudgetValue as MinValue,
                    sabr.MaxAdBudgetValue as MaxValue,
                    NULL as Percentage,
                    sabr.FixedQuote,
                    s.ServiceName,
                    s.ServiceCode,
                    s.ServiceDescription
                FROM Corporate.ServiceAdBudgetRanges sabr
                INNER JOIN Corporate.Services s ON sabr.ServiceId = s.ServiceId
                WHERE sabr.ServiceId = @ServiceId
                    AND sabr.BusinessTypeId = @BusinessTypeId
                    AND sabr.PlatformId = @PlatformId
                    AND sabr.IsActive = 1
                    AND @BudgetAmount >= sabr.MinAdBudgetValue
                    AND (@BudgetAmount < sabr.MaxAdBudgetValue OR sabr.MaxAdBudgetValue IS NULL)
                ORDER BY sabr.MinAdBudgetValue ASC";

            fixedRange = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlFixedWithPlatform,
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, PlatformId = platformId, BudgetAmount = budgetAmount });
        }

        if (fixedRange == null)
        {
            var sqlFixedWithoutPlatform = @"
                SELECT TOP 1
                    'fixed' as CalculationType,
                    sabr.ServiceAdBudgetRangeId as RangeId,
                    sabr.ServiceId,
                    sabr.BusinessTypeId,
                    sabr.PlatformId,
                    sabr.MinAdBudgetValue as MinValue,
                    sabr.MaxAdBudgetValue as MaxValue,
                    NULL as Percentage,
                    sabr.FixedQuote,
                    s.ServiceName,
                    s.ServiceCode,
                    s.ServiceDescription
                FROM Corporate.ServiceAdBudgetRanges sabr
                INNER JOIN Corporate.Services s ON sabr.ServiceId = s.ServiceId
                WHERE sabr.ServiceId = @ServiceId
                    AND sabr.BusinessTypeId = @BusinessTypeId
                    AND sabr.PlatformId IS NULL
                    AND sabr.IsActive = 1
                    AND @BudgetAmount >= sabr.MinAdBudgetValue
                    AND (@BudgetAmount < sabr.MaxAdBudgetValue OR sabr.MaxAdBudgetValue IS NULL)
                ORDER BY sabr.MinAdBudgetValue ASC";

            fixedRange = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sqlFixedWithoutPlatform,
                new { ServiceId = serviceId, BusinessTypeId = businessTypeId, BudgetAmount = budgetAmount });
        }

        return fixedRange;
    }

    public async Task<dynamic?> GetBusinessTypeByIdAsync(int businessTypeId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                BusinessTypeId,
                BusinessTypeName,
                BusinessTypeKey,
                Description
            FROM Corporate.BusinessTypes
            WHERE BusinessTypeId = @BusinessTypeId";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { BusinessTypeId = businessTypeId });
    }

    public async Task<dynamic?> GetPlatformByIdAsync(int platformId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                PlatformId,
                BusinessTypeId,
                PlatformName,
                PlatformKey,
                Description
            FROM Corporate.Platforms
            WHERE PlatformId = @PlatformId";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { PlatformId = platformId });
    }

    public async Task<dynamic?> GetServiceByIdAsync(int serviceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                ServiceId,
                ServiceCode,
                ServiceName,
                ServiceDescription
            FROM Corporate.Services
            WHERE ServiceId = @ServiceId";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { ServiceId = serviceId });
    }
}

