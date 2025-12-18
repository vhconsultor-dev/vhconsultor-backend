using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para actualizar un ContractService usando Entity Framework
/// </summary>
public class UpdateContractServiceCommand
{
    private readonly DBcontext _context;

    public UpdateContractServiceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la actualización de un ContractService
    /// </summary>
    /// <param name="contractServiceId">ID del servicio del contrato a actualizar</param>
    /// <param name="request">Datos actualizados del servicio del contrato</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int contractServiceId, UpdateContractServiceRequest request)
    {
        var contractService = await _context.ContractServices.FindAsync(contractServiceId);
        
        if (contractService == null)
            return false;

        // Actualizar propiedades
        contractService.ContractId = request.ContractId;
        contractService.ServiceId = request.ServiceId;
        contractService.ServiceDescription = request.ServiceDescription;
        contractService.Regions = request.Regions;
        contractService.UnitPrice = request.UnitPrice;
        contractService.Quantity = request.Quantity;
        contractService.DiscountPercentage = request.DiscountPercentage;
        contractService.FinalPrice = request.FinalPrice;
        contractService.ServiceOrder = request.ServiceOrder;
        contractService.BillingFrequency = request.BillingFrequency;
        contractService.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

/// <summary>
/// Request para actualizar un ContractService
/// </summary>
public class UpdateContractServiceRequest
{
    public int ContractId { get; set; }
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

