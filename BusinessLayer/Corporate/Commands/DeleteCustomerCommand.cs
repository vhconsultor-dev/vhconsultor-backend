using ModelLayer;

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
    public async Task<bool> ExecuteAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        
        if (customer == null)
            return false;

        // Soft delete: marcar como inactivo
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        
        return true;
    }
}

