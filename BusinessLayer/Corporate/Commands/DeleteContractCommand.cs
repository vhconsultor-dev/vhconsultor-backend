using ModelLayer;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

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
    /// <exception cref="InvalidOperationException">Si el contrato tiene facturas asociadas (mensaje incluye números de facturas)</exception>
    public async Task<bool> ExecuteAsync(int contractId, string? deletedBy = null)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        
        if (contract == null)
            return false;

        // Obtener facturas asociadas al contrato
        var invoices = await _context.Invoices
            .Where(i => i.ContractId == contractId)
            .Select(i => i.InvoiceNumber)
            .ToListAsync();

        if (invoices.Any())
        {
            // Crear mensaje amigable con los números de facturas
            var invoiceList = string.Join(", ", invoices);
            var invoiceWord = invoices.Count == 1 ? "invoice" : "invoices";
            
            throw new InvalidOperationException(
                $"Cannot delete the contract because it has {invoices.Count} associated {invoiceWord}: {invoiceList}. " +
                "Please delete these invoices first before deleting the contract.");
        }

        // Soft delete: cambiar el estado a "Cancelado"
        contract.Status = "Cancelled";
        contract.UpdatedAt = DateTimeService.GetCostaRicaNow();
        contract.LastModifiedBy = deletedBy ?? contract.LastModifiedBy;

        await _context.SaveChangesAsync();
        
        return true;
    }
}

