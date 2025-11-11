using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace ApplicationLayer.Corporate;

public class PricingService
{
    private readonly PricingCommandRepository _commandRepository;
    private readonly PricingQueryRepository _queryRepository;

    public PricingService(
        PricingCommandRepository commandRepository,
        PricingQueryRepository queryRepository)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
    }

    // =====================================================
    // ServiceBudgetRange Operations
    // =====================================================

    public async Task<IEnumerable<dynamic>> GetServiceBudgetRangesAsync(
        int? serviceId = null,
        int? businessTypeId = null,
        int? platformId = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetServiceBudgetRangesAsync(
            serviceId, businessTypeId, platformId, isActive);
    }

    public async Task<dynamic?> GetServiceBudgetRangeByIdAsync(int id)
    {
        return await _queryRepository.GetServiceBudgetRangeByIdAsync(id);
    }

    public async Task<CreateRangeResult> CreateServiceBudgetRangeAsync(CreateServiceBudgetRangeCommand command)
    {
        // Validar que el servicio existe
        if (!await _commandRepository.ServiceExistsAsync(command.ServiceId))
        {
            return new CreateRangeResult
            {
                Success = false,
                Message = $"El servicio con ID {command.ServiceId} no existe"
            };
        }

        // Validar solapamiento de rangos
        if (await _commandRepository.HasOverlappingBudgetRangeAsync(
            command.ServiceId, command.BusinessTypeId, command.PlatformId,
            command.MinBudgetValue, command.MaxBudgetValue))
        {
            return new CreateRangeResult
            {
                Success = false,
                Message = "Ya existe un rango que se solapa con los valores proporcionados"
            };
        }

        var range = new ServiceBudgetRange
        {
            ServiceId = command.ServiceId,
            BusinessTypeId = command.BusinessTypeId,
            PlatformId = command.PlatformId,
            MinBudgetValue = command.MinBudgetValue,
            MaxBudgetValue = command.MaxBudgetValue,
            Percentage = command.Percentage,
            IsActive = command.IsActive
        };

        var rangeId = await _commandRepository.CreateServiceBudgetRangeAsync(range);

        return new CreateRangeResult
        {
            Success = true,
            RangeId = rangeId,
            Message = "Rango de presupuesto creado exitosamente"
        };
    }

    public async Task<UpdateRangeResult> UpdateServiceBudgetRangeAsync(int id, UpdateServiceBudgetRangeCommand command)
    {
        try
        {
            // Validar que el rango existe
            var existing = await _queryRepository.GetServiceBudgetRangeByIdAsync(id);
            if (existing == null)
            {
                return new UpdateRangeResult
                {
                    Success = false,
                    Message = $"Rango con ID {id} no encontrado"
                };
            }

            // Si se están actualizando los valores de rango, validar solapamiento
            if (command.MinBudgetValue.HasValue || command.MaxBudgetValue.HasValue)
            {
                var minValue = command.MinBudgetValue ?? existing.MinBudgetValue;
                var maxValue = command.MaxBudgetValue ?? existing.MaxBudgetValue;
                var serviceId = command.ServiceId ?? existing.ServiceId;
                var businessTypeId = command.BusinessTypeId ?? existing.BusinessTypeId;
                var platformId = command.PlatformId ?? existing.PlatformId;

                if (await _commandRepository.HasOverlappingBudgetRangeAsync(
                    serviceId, businessTypeId, platformId, minValue, maxValue, id))
                {
                    return new UpdateRangeResult
                    {
                        Success = false,
                        Message = "Los valores actualizados se solapan con un rango existente"
                    };
                }
            }

            await _commandRepository.UpdateServiceBudgetRangeAsync(id, command);

            return new UpdateRangeResult
            {
                Success = true,
                Message = "Rango de presupuesto actualizado exitosamente"
            };
        }
        catch (KeyNotFoundException ex)
        {
            return new UpdateRangeResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<DeleteRangeResult> DeleteServiceBudgetRangeAsync(int id)
    {
        try
        {
            await _commandRepository.DeleteServiceBudgetRangeAsync(id);
            return new DeleteRangeResult
            {
                Success = true,
                Message = "Rango de presupuesto desactivado exitosamente"
            };
        }
        catch (KeyNotFoundException ex)
        {
            return new DeleteRangeResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =====================================================
    // ServiceAdBudgetRange Operations
    // =====================================================

    public async Task<IEnumerable<dynamic>> GetServiceAdBudgetRangesAsync(
        int? serviceId = null,
        int? businessTypeId = null,
        int? platformId = null,
        bool? isActive = null)
    {
        return await _queryRepository.GetServiceAdBudgetRangesAsync(
            serviceId, businessTypeId, platformId, isActive);
    }

    public async Task<dynamic?> GetServiceAdBudgetRangeByIdAsync(int id)
    {
        return await _queryRepository.GetServiceAdBudgetRangeByIdAsync(id);
    }

    public async Task<CreateRangeResult> CreateServiceAdBudgetRangeAsync(CreateServiceAdBudgetRangeCommand command)
    {
        // Validar que el servicio existe
        if (!await _commandRepository.ServiceExistsAsync(command.ServiceId))
        {
            return new CreateRangeResult
            {
                Success = false,
                Message = $"El servicio con ID {command.ServiceId} no existe"
            };
        }

        // Validar solapamiento de rangos
        if (await _commandRepository.HasOverlappingAdBudgetRangeAsync(
            command.ServiceId, command.BusinessTypeId, command.PlatformId,
            command.MinAdBudgetValue, command.MaxAdBudgetValue))
        {
            return new CreateRangeResult
            {
                Success = false,
                Message = "Ya existe un rango que se solapa con los valores proporcionados"
            };
        }

        var range = new ServiceAdBudgetRange
        {
            ServiceId = command.ServiceId,
            BusinessTypeId = command.BusinessTypeId,
            PlatformId = command.PlatformId,
            MinAdBudgetValue = command.MinAdBudgetValue,
            MaxAdBudgetValue = command.MaxAdBudgetValue,
            FixedQuote = command.FixedQuote,
            IsActive = command.IsActive
        };

        var rangeId = await _commandRepository.CreateServiceAdBudgetRangeAsync(range);

        return new CreateRangeResult
        {
            Success = true,
            RangeId = rangeId,
            Message = "Rango de presupuesto de publicidad creado exitosamente"
        };
    }

    public async Task<UpdateRangeResult> UpdateServiceAdBudgetRangeAsync(int id, UpdateServiceAdBudgetRangeCommand command)
    {
        try
        {
            // Validar que el rango existe
            var existing = await _queryRepository.GetServiceAdBudgetRangeByIdAsync(id);
            if (existing == null)
            {
                return new UpdateRangeResult
                {
                    Success = false,
                    Message = $"Rango con ID {id} no encontrado"
                };
            }

            // Si se están actualizando los valores de rango, validar solapamiento
            if (command.MinAdBudgetValue.HasValue || command.MaxAdBudgetValue.HasValue)
            {
                var minValue = command.MinAdBudgetValue ?? existing.MinAdBudgetValue;
                var maxValue = command.MaxAdBudgetValue ?? existing.MaxAdBudgetValue;
                var serviceId = command.ServiceId ?? existing.ServiceId;
                var businessTypeId = command.BusinessTypeId ?? existing.BusinessTypeId;
                var platformId = command.PlatformId ?? existing.PlatformId;

                if (await _commandRepository.HasOverlappingAdBudgetRangeAsync(
                    serviceId, businessTypeId, platformId, minValue, maxValue, id))
                {
                    return new UpdateRangeResult
                    {
                        Success = false,
                        Message = "Los valores actualizados se solapan con un rango existente"
                    };
                }
            }

            await _commandRepository.UpdateServiceAdBudgetRangeAsync(id, command);

            return new UpdateRangeResult
            {
                Success = true,
                Message = "Rango de presupuesto de publicidad actualizado exitosamente"
            };
        }
        catch (KeyNotFoundException ex)
        {
            return new UpdateRangeResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<DeleteRangeResult> DeleteServiceAdBudgetRangeAsync(int id)
    {
        try
        {
            await _commandRepository.DeleteServiceAdBudgetRangeAsync(id);
            return new DeleteRangeResult
            {
                Success = true,
                Message = "Rango de presupuesto de publicidad desactivado exitosamente"
            };
        }
        catch (KeyNotFoundException ex)
        {
            return new DeleteRangeResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =====================================================
    // Calculation Operations
    // =====================================================

    public async Task<CalculatePercentageResult> CalculatePercentageAsync(CalculatePercentageCommand command)
    {
        var range = await _queryRepository.FindBudgetRangeForCalculationAsync(
            command.ServiceId, command.BusinessTypeId, command.PlatformId, command.AnnualBudget);

        if (range == null)
        {
            return new CalculatePercentageResult
            {
                Success = false,
                Message = "No se encontró un rango de presupuesto activo que contenga el valor proporcionado"
            };
        }

        var calculatedCommission = command.AnnualBudget * (range.Percentage / 100);

        return new CalculatePercentageResult
        {
            Success = true,
            RangeId = range.ServiceBudgetRangeId,
            MinBudgetValue = range.MinBudgetValue,
            MaxBudgetValue = range.MaxBudgetValue,
            Percentage = range.Percentage,
            AnnualBudget = command.AnnualBudget,
            CalculatedCommission = calculatedCommission,
            Message = "Comisión calculada exitosamente"
        };
    }

    public async Task<CalculateFixedResult> CalculateFixedAsync(CalculateFixedCommand command)
    {
        var range = await _queryRepository.FindAdBudgetRangeForCalculationAsync(
            command.ServiceId, command.BusinessTypeId, command.PlatformId, command.AnnualAdBudget);

        if (range == null)
        {
            return new CalculateFixedResult
            {
                Success = false,
                Message = "No se encontró un rango de presupuesto de publicidad activo que contenga el valor proporcionado"
            };
        }

        return new CalculateFixedResult
        {
            Success = true,
            RangeId = range.ServiceAdBudgetRangeId,
            MinAdBudgetValue = range.MinAdBudgetValue,
            MaxAdBudgetValue = range.MaxAdBudgetValue,
            AnnualAdBudget = command.AnnualAdBudget,
            FixedQuote = range.FixedQuote,
            Message = "Valor fijo obtenido exitosamente"
        };
    }

    // =====================================================
    // Calculate Pricing Operation
    // =====================================================

    public async Task<CalculatePricingResult> CalculatePricingAsync(CalculatePricingCommand command)
    {
        // Validar que el BusinessType existe
        var businessType = await _queryRepository.GetBusinessTypeByIdAsync(command.BusinessTypeId);
        if (businessType == null)
        {
            return new CalculatePricingResult
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = $"BusinessType con ID {command.BusinessTypeId} no existe"
            };
        }

        // Validar que el Service existe
        var service = await _queryRepository.GetServiceByIdAsync(command.ServiceId);
        if (service == null)
        {
            return new CalculatePricingResult
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = $"Service con ID {command.ServiceId} no existe"
            };
        }

        // Validar Platform si se proporciona
        dynamic? platform = null;
        if (command.PlatformId.HasValue)
        {
            platform = await _queryRepository.GetPlatformByIdAsync(command.PlatformId.Value);
            if (platform == null)
            {
                return new CalculatePricingResult
                {
                    Success = false,
                    ErrorCode = "VALIDATION_ERROR",
                    ErrorMessage = $"Platform con ID {command.PlatformId} no existe"
                };
            }

            // Validar que la plataforma pertenece al BusinessType
            if (platform.BusinessTypeId != command.BusinessTypeId)
            {
                return new CalculatePricingResult
                {
                    Success = false,
                    ErrorCode = "VALIDATION_ERROR",
                    ErrorMessage = $"Platform {command.PlatformId} no pertenece al BusinessType {command.BusinessTypeId}"
                };
            }
        }

        // Buscar rango y calcular
        var range = await _queryRepository.CalculatePricingAsync(
            command.BusinessTypeId,
            command.PlatformId,
            command.ServiceId,
            command.BudgetAmount);

        if (range == null)
        {
            return new CalculatePricingResult
            {
                Success = false,
                ErrorCode = "RANGE_NOT_FOUND",
                ErrorMessage = "No se encontró un rango de pricing activo para los parámetros proporcionados",
                ErrorDetails = new
                {
                    businessTypeId = command.BusinessTypeId,
                    platformId = command.PlatformId,
                    serviceId = command.ServiceId,
                    budgetAmount = command.BudgetAmount
                }
            };
        }

        // Calcular el monto final según el tipo
        decimal finalAmount;
        decimal calculationBase = range.MinValue;
        string calculationType = range.CalculationType;

        if (calculationType == "percentage")
        {
            // IMPORTANTE: El porcentaje se aplica sobre MinBudgetValue, NO sobre el budgetAmount ingresado
            finalAmount = range.MinValue * (range.Percentage / 100);
        }
        else // fixed
        {
            finalAmount = range.FixedQuote;
        }

        // Construir rangeDisplay
        string rangeDisplay;
        if (range.MaxValue == null)
        {
            rangeDisplay = $"${range.MinValue:N0}+";
        }
        else
        {
            rangeDisplay = $"${range.MinValue:N0} - ${range.MaxValue:N0}";
        }

        return new CalculatePricingResult
        {
            Success = true,
            CalculationType = calculationType,
            BusinessType = new BusinessTypeInfo
            {
                BusinessTypeId = businessType.BusinessTypeId,
                BusinessTypeName = businessType.BusinessTypeName,
                BusinessTypeKey = businessType.BusinessTypeKey
            },
            Platform = platform != null ? new PlatformInfo
            {
                PlatformId = platform.PlatformId,
                PlatformName = platform.PlatformName,
                PlatformKey = platform.PlatformKey
            } : null,
            Service = new ServiceInfo
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                ServiceCode = service.ServiceCode
            },
            InputBudget = command.BudgetAmount,
            Range = new RangeInfo
            {
                RangeId = range.RangeId,
                MinValue = range.MinValue,
                MaxValue = range.MaxValue,
                RangeDisplay = rangeDisplay
            },
            Pricing = new PricingInfo
            {
                Percentage = calculationType == "percentage" ? range.Percentage : null,
                FixedQuote = calculationType == "fixed" ? range.FixedQuote : null,
                CalculationBase = calculationBase
            },
            Result = new ResultInfo
            {
                FinalAmount = finalAmount,
                Currency = "USD",
                FormattedAmount = $"${finalAmount:N2}"
            },
            Metadata = new MetadataInfo
            {
                CalculatedAt = DateTimeService.GetCostaRicaNow(),
                RangeFound = true
            }
        };
    }
}

// Result classes for Calculate Pricing
public class CalculatePricingResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public object? ErrorDetails { get; set; }
    public string? CalculationType { get; set; }
    public BusinessTypeInfo? BusinessType { get; set; }
    public PlatformInfo? Platform { get; set; }
    public ServiceInfo? Service { get; set; }
    public decimal InputBudget { get; set; }
    public RangeInfo? Range { get; set; }
    public PricingInfo? Pricing { get; set; }
    public ResultInfo? Result { get; set; }
    public MetadataInfo? Metadata { get; set; }
}

public class BusinessTypeInfo
{
    public int BusinessTypeId { get; set; }
    public string BusinessTypeName { get; set; } = string.Empty;
    public string BusinessTypeKey { get; set; } = string.Empty;
}

public class PlatformInfo
{
    public int PlatformId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformKey { get; set; } = string.Empty;
}

public class ServiceInfo
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
}

public class RangeInfo
{
    public int RangeId { get; set; }
    public decimal MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string RangeDisplay { get; set; } = string.Empty;
}

public class PricingInfo
{
    public decimal? Percentage { get; set; }
    public decimal? FixedQuote { get; set; }
    public decimal CalculationBase { get; set; }
}

public class ResultInfo
{
    public decimal FinalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string FormattedAmount { get; set; } = string.Empty;
}

public class MetadataInfo
{
    public DateTime CalculatedAt { get; set; }
    public bool RangeFound { get; set; }
}

// Result classes
public class CreateRangeResult
{
    public bool Success { get; set; }
    public int RangeId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateRangeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DeleteRangeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CalculatePercentageResult
{
    public bool Success { get; set; }
    public int RangeId { get; set; }
    public decimal MinBudgetValue { get; set; }
    public decimal? MaxBudgetValue { get; set; }
    public decimal Percentage { get; set; }
    public decimal AnnualBudget { get; set; }
    public decimal CalculatedCommission { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CalculateFixedResult
{
    public bool Success { get; set; }
    public int RangeId { get; set; }
    public decimal MinAdBudgetValue { get; set; }
    public decimal? MaxAdBudgetValue { get; set; }
    public decimal AnnualAdBudget { get; set; }
    public decimal FixedQuote { get; set; }
    public string Message { get; set; } = string.Empty;
}

