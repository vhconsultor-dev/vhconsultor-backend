using ModelLayer;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para eliminar (soft delete) un Customer usando Entity Framework
/// </summary>
public class DeleteCustomerCommand
{
    private readonly DBcontext _context;

    public DeleteCustomerCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la eliminación lógica de un Customer (soft delete)
    /// </summary>
    /// <param name="customerId">ID del customer a eliminar</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    /// <exception cref="InvalidOperationException">Si el cliente tiene contratos asociados (mensaje incluye números de contratos)</exception>
    public async Task<bool> ExecuteAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        
        if (customer == null)
            return false;

        // Obtener contratos asociados al cliente
        var contracts = await _context.Contracts
            .Where(c => c.CustomerId == customerId)
            .Select(c => c.ContractNumber)
            .ToListAsync();

        if (contracts.Any())
        {
            // Crear mensaje amigable con los números de contratos
            var contractList = string.Join(", ", contracts);
            var contractWord = contracts.Count == 1 ? "contract" : "contracts";
            
            throw new InvalidOperationException(
                $"Cannot delete the customer because it has {contracts.Count} associated {contractWord}: {contractList}. " +
                "Please delete or cancel these contracts first before deleting the customer.");
        }

        // Soft delete: marcar como inactivo
        customer.IsActive = false;
        customer.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

