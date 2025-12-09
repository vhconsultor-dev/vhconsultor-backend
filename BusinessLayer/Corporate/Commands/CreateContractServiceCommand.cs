using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para crear un nuevo ContractService usando Entity Framework
/// </summary>
public class CreateContractServiceCommand
{
    private readonly DBcontext _context;

    public CreateContractServiceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la creación de un nuevo ContractService
    /// </summary>
    /// <param name="request">Datos del servicio del contrato</param>
    /// <returns>ID del servicio del contrato creado</returns>
    public async Task<int> ExecuteAsync(CreateContractServiceRequest request)
    {
        var contractService = new ContractService
        {
            ContractId = request.ContractId,
            ServiceId = request.ServiceId,
            ServiceDescription = request.ServiceDescription,
            Regions = request.Regions,
            UnitPrice = request.UnitPrice,
            Quantity = request.Quantity,
            DiscountPercentage = request.DiscountPercentage,
            FinalPrice = request.FinalPrice,
            IsActive = true,
            ServiceOrder = request.ServiceOrder,
            BillingFrequency = request.BillingFrequency,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.ContractServices.Add(contractService);
        await _context.SaveChangesAsync();
        
        return contractService.ContractServiceId;
    }
}

/// <summary>
/// Request para crear un ContractService
/// </summary>
public class CreateContractServiceRequest
{
    public int ContractId { get; set; } // Ahora es int
    public int? ServiceId { get; set; }
    public string? ServiceDescription { get; set; }
    public string? Regions { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? FinalPrice { get; set; }
    public int? ServiceOrder { get; set; }
    public string? BillingFrequency { get; set; }
}

