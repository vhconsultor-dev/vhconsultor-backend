using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para eliminar (soft delete) un ContractService usando Entity Framework
/// </summary>
public class DeleteContractServiceCommand
{
    private readonly DBcontext _context;

    public DeleteContractServiceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la eliminación lógica de un ContractService (soft delete)
    /// </summary>
    /// <param name="contractServiceId">ID del servicio del contrato a eliminar</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int contractServiceId)
    {
        var contractService = await _context.ContractServices.FindAsync(contractServiceId);
        
        if (contractService == null)
            return false;

        // Soft delete: marcar como inactivo
        contractService.IsActive = false;
        contractService.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

