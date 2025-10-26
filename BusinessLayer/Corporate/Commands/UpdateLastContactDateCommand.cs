using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para actualizar la fecha de último contacto de un Customer
/// </summary>
public class UpdateLastContactDateCommand
{
    private readonly DBcontext _context;

    public UpdateLastContactDateCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Actualiza la fecha de último contacto de un Customer
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        
        if (customer == null)
            return false;

        customer.LastContactDate = DateTimeService.GetCostaRicaNow();
        customer.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

