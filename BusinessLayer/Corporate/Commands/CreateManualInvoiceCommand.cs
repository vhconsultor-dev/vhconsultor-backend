using BusinessLayer.Corporate;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para crear una factura manualmente (para contratos de porcentaje)
/// </summary>
public class CreateManualInvoiceCommand
{
    private readonly DBcontext _context;

    public CreateManualInvoiceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la creación manual de una factura
    /// </summary>
    public async Task<CreateManualInvoiceResponse> ExecuteAsync(CreateManualInvoiceRequest request)
    {
        // 1. Obtener contrato
        var contract = await _context.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContractId == request.ContractId);

        if (contract == null)
            throw new KeyNotFoundException($"Contract with ID {request.ContractId} not found");

        // 2. Validar que sea contrato de porcentaje (FeeTypeId = 2)
        if (contract.FeeTypeId != 2)
        {
            throw new InvalidOperationException(
                $"This endpoint is only for percentage contracts (FeeTypeId = 2). " +
                $"Contract {contract.ContractNumber} has FeeTypeId = {contract.FeeTypeId}");
        }

        // 3. Validar campos requeridos del contrato
        ValidateContractForInvoiceCreation(contract);

        // 4. Calcular número esperado de facturas
        int expectedInvoices = CalculateNumberOfInvoices(
            contract.StartDate!.Value,
            contract.EndDate!.Value,
            contract.PaymentFrequency!);

        // 5. Contar facturas existentes
        int existingCount = await _context.Invoices
            .Where(i => i.ContractId == request.ContractId)
            .CountAsync();

        // 6. Validar que no se exceda el máximo
        if (existingCount >= expectedInvoices)
        {
            throw new InvalidOperationException(
                $"No se pueden crear más facturas. El contrato ya tiene el número máximo de facturas " +
                $"({existingCount}/{expectedInvoices}) basado en su frecuencia de pago y duración. " +
                "No se pueden agregar facturas adicionales sin modificar los términos del contrato.");
        }

        // 7. Generar número de factura
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        // 8. Crear la factura
        var invoice = new Invoice
        {
            ContractId = request.ContractId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            SubTotal = request.Amount,
            Tax = 0, // Sin impuestos
            Total = request.Amount,
            CurrencyCode = contract.CurrencyCode!,
            Status = "Draft",
            PaymentStatus = "Unpaid",
            Notes = request.Notes,
            Lang = !string.IsNullOrWhiteSpace(request.Lang) ? request.Lang.Trim() : "es",
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        // 9. Crear item de la factura
        var invoiceItem = new InvoiceItem
        {
            Description = request.Description ?? "Services for the period",
            Quantity = 1,
            UnitPrice = request.Amount,
            Discount = 0,
            LineTotal = request.Amount,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        invoice.InvoiceItems.Add(invoiceItem);

        // 10. Guardar
        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync();

        return new CreateManualInvoiceResponse
        {
            Success = true,
            Message = $"Invoice '{invoiceNumber}' created successfully for contract {contract.ContractNumber}. " +
                      $"Contract now has {existingCount + 1}/{expectedInvoices} invoices",
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoiceNumber,
            CurrentInvoiceCount = existingCount + 1,
            ExpectedInvoiceCount = expectedInvoices
        };
    }

    private void ValidateContractForInvoiceCreation(Contract contract)
    {
        var errors = new List<string>();

        if (!contract.StartDate.HasValue)
            errors.Add("El contrato debe tener una fecha de inicio (StartDate)");
        else if (contract.StartDate.Value.Date > DateTime.Now.Date)
            errors.Add($"La fecha de inicio del contrato ({contract.StartDate.Value:yyyy-MM-dd}) no puede ser futura");

        if (!contract.EndDate.HasValue)
            errors.Add("El contrato debe tener una fecha de finalización (EndDate)");
        else if (contract.StartDate.HasValue && contract.EndDate.Value <= contract.StartDate.Value)
            errors.Add("La fecha de finalización debe ser posterior a la fecha de inicio");

        if (string.IsNullOrEmpty(contract.PaymentFrequency))
            errors.Add("El contrato debe tener una frecuencia de pago (PaymentFrequency)");
        else if (!PaymentFrequencyHelper.IsAllowed(contract.PaymentFrequency))
            errors.Add($"Frecuencia de pago inválida: '{contract.PaymentFrequency}'. Valores válidos: {PaymentFrequencyHelper.AllowedValuesDescription}");

        if (string.IsNullOrEmpty(contract.CurrencyCode))
            errors.Add("El contrato debe tener un código de moneda (CurrencyCode)");
        else if (contract.CurrencyCode.Length != 3)
            errors.Add($"Código de moneda inválido: '{contract.CurrencyCode}'. Debe ser de 3 caracteres (ej: USD, EUR)");

        if (errors.Any())
            throw new InvalidOperationException(string.Join("; ", errors));
    }

    private int CalculateNumberOfInvoices(DateTime startDate, DateTime endDate, string paymentFrequency)
    {
        int contractMonths = CalculateContractMonths(startDate, endDate);
        return PaymentFrequencyHelper.CalculateInvoiceCount(contractMonths, paymentFrequency);
    }

    private int CalculateContractMonths(DateTime startDate, DateTime endDate)
    {
        int months = ((endDate.Year - startDate.Year) * 12) + (endDate.Month - startDate.Month);
        
        if (endDate.Day >= startDate.Day)
            months++;
        
        return months;
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.Now.Year;
        var lastInvoice = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"INV-{year}-{nextNumber:D3}";
    }
}

/// <summary>
/// Request para crear una factura manualmente
/// </summary>
public class CreateManualInvoiceRequest
{
    public int ContractId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    /// <summary>Language for PDF: "en" (English) or "es" (Spanish). Defaults to "es" if not provided.</summary>
    public string? Lang { get; set; }
}

/// <summary>
/// Response de creación manual de factura
/// </summary>
public class CreateManualInvoiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CurrentInvoiceCount { get; set; }
    public int ExpectedInvoiceCount { get; set; }
}
