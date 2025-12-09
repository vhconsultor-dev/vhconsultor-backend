using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para eliminar (soft delete) un Contract usando Entity Framework
/// </summary>
public class DeleteContractCommand
{
    private readonly DBcontext _context;

    public DeleteContractCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la eliminación lógica de un Contract
    /// Cambia el estado a "Cancelado" o "Inactivo"
    /// </summary>
    /// <param name="contractId">ID del contrato a eliminar</param>
    /// <param name="deletedBy">Usuario que elimina el contrato</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int contractId, string? deletedBy = null) // Ahora es int
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        
        if (contract == null)
            return false;

        // Soft delete: cambiar el estado a "Cancelado"
        contract.Status = "Cancelado";
        contract.UpdatedAt = DateTimeService.GetCostaRicaNow();
        contract.LastModifiedBy = deletedBy ?? contract.LastModifiedBy;

        await _context.SaveChangesAsync();
        
        return true;
    }
}

