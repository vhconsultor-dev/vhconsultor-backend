using BusinessLayer.Corporate;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para generar facturas automáticamente con monto 0 (para contratos de porcentaje)
/// </summary>
public class GeneratePercentageInvoicesCommand
{
    private readonly DBcontext _context;

    public GeneratePercentageInvoicesCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la generación automática de facturas con monto 0
    /// </summary>
    public async Task<GeneratePercentageInvoicesResponse> ExecuteAsync(GeneratePercentageInvoicesRequest request)
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

        // 3. Validar campos requeridos
        ValidateContractForInvoiceGeneration(contract);

        // 4. Calcular número esperado de facturas
        int expectedInvoices = CalculateNumberOfInvoices(
            contract.StartDate!.Value,
            contract.EndDate!.Value,
            contract.PaymentFrequency!);

        if (expectedInvoices <= 0)
        {
            int contractMonths = CalculateContractMonths(contract.StartDate!.Value, contract.EndDate!.Value);
            int monthsInFrequency = PaymentFrequencyHelper.GetMonthsIncrement(contract.PaymentFrequency!);
            throw new InvalidOperationException(
                $"No se pueden generar facturas. La frecuencia de pago ({monthsInFrequency} meses) es mayor que la duración del contrato ({contractMonths} meses). " +
                $"Ajuste la frecuencia de pago o la duración del contrato.");
        }

        // 5. Contar facturas existentes
        var existingInvoices = await _context.Invoices
            .Where(i => i.ContractId == request.ContractId)
            .ToListAsync();

        int existingCount = existingInvoices.Count;

        // 6. Validar que no se exceda el máximo
        if (existingCount >= expectedInvoices)
        {
            return new GeneratePercentageInvoicesResponse
            {
                Success = false,
                Message = $"Cannot generate more invoices. The contract already has the maximum number of invoices ({existingCount}/{expectedInvoices})",
                InvoicesGenerated = 0,
                ExistingInvoicesCount = existingCount,
                ExpectedInvoicesCount = expectedInvoices
            };
        }

        // 7. Calcular cuántas facturas faltan
        int invoicesToCreate = expectedInvoices - existingCount;

        // 8. Generar las facturas que faltan con monto 0
        var invoices = new List<Invoice>();
        DateTime currentDate = contract.StartDate!.Value;
        int monthsIncrement = PaymentFrequencyHelper.GetMonthsIncrement(contract.PaymentFrequency!);

        var invoiceNumbers = await GenerateInvoiceNumbersAsync(invoicesToCreate);
        int startIndex = existingCount;

        for (int i = 0; i < invoicesToCreate; i++)
        {
            int totalIndex = startIndex + i;
            
            // Calcular fechas
            DateTime invoiceDate = totalIndex == 0 ? currentDate : currentDate.AddMonths(totalIndex * monthsIncrement);
            DateTime dueDate = CalculateDueDate(invoiceDate, contract.PaymentDay);

            // Crear factura con monto 0
            var invoice = new Invoice
            {
                ContractId = request.ContractId,
                InvoiceNumber = invoiceNumbers[i],
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                SubTotal = 0, // Monto en 0
                Tax = 0, // Sin impuestos
                Total = 0, // Total en 0
                CurrencyCode = contract.CurrencyCode!,
                Status = "Draft",
                PaymentStatus = "Unpaid",
                Lang = "es",
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };

            // Crear item con monto 0
            var invoiceItem = new InvoiceItem
            {
                Description = contract.FeeDescription ?? contract.ServiceDescription ?? "Services for the period",
                Quantity = 1,
                UnitPrice = 0,
                Discount = 0,
                LineTotal = 0,
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };

            invoice.InvoiceItems.Add(invoiceItem);
            invoices.Add(invoice);
        }

        // 9. Guardar todas las facturas
        await _context.Invoices.AddRangeAsync(invoices);
        await _context.SaveChangesAsync();

        // 10. Mensaje informativo
        string message = invoicesToCreate == 1
            ? $"Successfully generated 1 invoice with zero amount for contract {contract.ContractNumber}"
            : $"Successfully generated {invoicesToCreate} invoice(s) with zero amount for contract {contract.ContractNumber}";

        if (existingCount > 0)
        {
            message += $". Contract now has {existingCount + invoicesToCreate}/{expectedInvoices} invoices";
        }

        message += ". You can now manually edit each invoice to set the correct amount";

        return new GeneratePercentageInvoicesResponse
        {
            Success = true,
            Message = message,
            InvoicesGenerated = invoicesToCreate,
            InvoiceIds = invoices.Select(i => i.InvoiceId).ToList(),
            ExistingInvoicesCount = existingCount,
            ExpectedInvoicesCount = expectedInvoices
        };
    }

    private void ValidateContractForInvoiceGeneration(Contract contract)
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

    private DateTime CalculateDueDate(DateTime invoiceDate, int? paymentDay)
    {
        DateTime dueDate = invoiceDate.AddDays(30);

        if (paymentDay.HasValue && paymentDay.Value > 0 && paymentDay.Value <= 31)
        {
            int day = paymentDay.Value;
            int lastDayOfMonth = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
            day = Math.Min(day, lastDayOfMonth);
            dueDate = new DateTime(dueDate.Year, dueDate.Month, day);
        }

        return dueDate;
    }

    private async Task<List<string>> GenerateInvoiceNumbersAsync(int count)
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

        var invoiceNumbers = new List<string>();
        for (int i = 0; i < count; i++)
        {
            invoiceNumbers.Add($"INV-{year}-{(nextNumber + i):D3}");
        }

        return invoiceNumbers;
    }
}

/// <summary>
/// Request para generar facturas de porcentaje con monto 0
/// </summary>
public class GeneratePercentageInvoicesRequest
{
    public int ContractId { get; set; }
}

/// <summary>
/// Response de generación de facturas de porcentaje
/// </summary>
public class GeneratePercentageInvoicesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoicesGenerated { get; set; }
    public List<int> InvoiceIds { get; set; } = new();
    public int ExistingInvoicesCount { get; set; }
    public int ExpectedInvoicesCount { get; set; }
}
