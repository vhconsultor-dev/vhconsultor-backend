using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para generar facturas automáticamente desde un contrato
/// </summary>
public class GenerateInvoicesCommand
{
    private readonly DBcontext _context;

    public GenerateInvoicesCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la generación de facturas para un contrato
    /// </summary>
    /// <param name="request">Datos de la solicitud</param>
    /// <returns>Resultado de la generación</returns>
    public async Task<GenerateInvoicesResponse> ExecuteAsync(GenerateInvoicesRequest request)
    {
        // 1. Obtener contrato con sus servicios
        int contractId = request.ContractId;
        
        // Buscar el contrato sin tracking para evitar problemas de conversión en relaciones
        var contract = await _context.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            throw new KeyNotFoundException($"Contract with ID {contractId} not found");

        // 2. Validar que sea contrato de monto fijo (FeeTypeId = 1)
        if (contract.FeeTypeId != 1)
        {
            throw new InvalidOperationException(
                $"This endpoint is only for fixed amount contracts (FeeTypeId = 1). " +
                $"Contract {contract.ContractNumber} has FeeTypeId = {contract.FeeTypeId}");
        }

        // 3. Contar facturas existentes
        var existingInvoices = await _context.Invoices
            .Where(i => i.ContractId == contractId)
            .ToListAsync();

        int existingCount = existingInvoices.Count;

        // 4. Validar campos requeridos
        await ValidateContractForInvoiceGenerationAsync(contract, contractId);

        // 5. Calcular número de facturas esperadas según el contrato
        int expectedNumberOfInvoices = CalculateNumberOfInvoices(
            contract.StartDate!.Value,
            contract.EndDate!.Value,
            contract.PaymentFrequency!
        );

        if (expectedNumberOfInvoices <= 0)
        {
            int contractMonths = CalculateContractMonths(contract.StartDate!.Value, contract.EndDate!.Value);
            int monthsInFrequency = GetMonthsForFrequency(contract.PaymentFrequency!);
            throw new InvalidOperationException(
                $"Cannot generate invoices. Payment frequency ({monthsInFrequency} months) is greater than contract duration ({contractMonths} months). " +
                $"Please adjust the payment frequency or contract duration.");
        }

        // 6. Validar que no se exceda el número máximo de facturas
        if (existingCount >= expectedNumberOfInvoices)
        {
            return new GenerateInvoicesResponse
            {
                Success = false,
                Message = $"Cannot generate more invoices. The contract already has the maximum number of invoices ({existingCount}/{expectedNumberOfInvoices})",
                InvoicesGenerated = 0,
                ExistingInvoicesCount = existingCount,
                ExpectedInvoicesCount = expectedNumberOfInvoices
            };
        }

        // 7. Calcular cuántas facturas faltan por crear
        int invoicesToCreate = expectedNumberOfInvoices - existingCount;

        // 8. Validar FeeAmount para contratos de monto fijo
        if (!contract.FeeAmount.HasValue || contract.FeeAmount.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Fixed amount contracts must have a valid FeeAmount greater than zero. " +
                $"Contract {contract.ContractNumber} has FeeAmount = {contract.FeeAmount}");
        }

        // 9. Para contratos de monto fijo, cada factura tiene el mismo monto
        decimal amountPerInvoice = contract.FeeAmount.Value;

        // 10. Obtener servicios del contrato (opcional para items)
        var contractServices = await _context.ContractServices
            .Where(cs => cs.ContractId == contractId && cs.IsActive)
            .OrderBy(cs => cs.ServiceOrder)
            .ToListAsync();

        // 11. Generar las facturas que faltan
        var invoices = new List<Invoice>();
        DateTime currentDate = contract.StartDate!.Value;
        int monthsIncrement = GetMonthsForFrequency(contract.PaymentFrequency!);

        // Generar todos los números de factura de una vez para evitar duplicados
        var invoiceNumbers = await GenerateInvoiceNumbersAsync(invoicesToCreate);

        // Determinar desde qué índice comenzar (basado en las facturas existentes)
        int startIndex = existingCount;

        for (int i = 0; i < invoicesToCreate; i++)
        {
            int totalIndex = startIndex + i;
            
            // Calcular fechas
            DateTime invoiceDate = totalIndex == 0 ? currentDate : currentDate.AddMonths(totalIndex * monthsIncrement);
            DateTime dueDate = CalculateDueDate(invoiceDate, contract.PaymentDay);

            // Para monto fijo: todas las facturas tienen el mismo monto
            decimal invoiceAmount = amountPerInvoice;

            // Crear factura
            var invoice = new Invoice
            {
                ContractId = contractId,
                InvoiceNumber = invoiceNumbers[i], // Usar número pre-generado
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                SubTotal = invoiceAmount,
                Tax = 0, // Sin impuestos
                Total = invoiceAmount,
                CurrencyCode = contract.CurrencyCode!,
                Status = "Draft",
                PaymentStatus = "Unpaid",
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };
            
            // NO establecer la relación Contract para evitar problemas de conversión
            // Entity Framework establecerá la relación automáticamente si es necesario

            // Crear items de la factura
            CreateInvoiceItems(invoice, contract, contractServices, invoiceAmount);

            invoices.Add(invoice);
        }

        // 12. Guardar todas las facturas
        // Asegurar que cada invoice tenga ContractId correctamente asignado
        foreach (var invoice in invoices)
        {
            invoice.ContractId = contractId;
            // NO establecer la relación Contract para evitar que EF intente convertir tipos
            invoice.Contract = null;
        }
        
        await _context.Invoices.AddRangeAsync(invoices);
        await _context.SaveChangesAsync();

        // Construir mensaje informativo
        string message = invoicesToCreate == 1
            ? $"Successfully generated 1 invoice for contract {contract.ContractNumber}"
            : $"Successfully generated {invoicesToCreate} invoice(s) for contract {contract.ContractNumber}";

        if (existingCount > 0)
        {
            message += $". Contract now has {existingCount + invoicesToCreate}/{expectedNumberOfInvoices} invoices";
        }

        return new GenerateInvoicesResponse
        {
            Success = true,
            Message = message,
            InvoicesGenerated = invoicesToCreate,
            TotalAmount = amountPerInvoice * invoicesToCreate,
            InvoiceIds = invoices.Select(i => i.InvoiceId).ToList(),
            ExistingInvoicesCount = existingCount,
            ExpectedInvoicesCount = expectedNumberOfInvoices
        };
    }

    private async Task ValidateContractForInvoiceGenerationAsync(Contract contract, int contractId)
    {
        var errors = new List<string>();

        if (!contract.StartDate.HasValue)
            errors.Add("Contract must have a StartDate");

        if (!contract.EndDate.HasValue)
            errors.Add("Contract must have an EndDate");

        if (string.IsNullOrEmpty(contract.PaymentFrequency))
            errors.Add("Contract must have a PaymentFrequency");

        if (string.IsNullOrEmpty(contract.CurrencyCode))
            errors.Add("Contract must have a CurrencyCode");

        if (errors.Any())
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", errors)}");
    }

    /// <summary>
    /// Calcula el número de meses entre dos fechas de forma precisa
    /// </summary>
    private int CalculateContractMonths(DateTime startDate, DateTime endDate)
    {
        int months = ((endDate.Year - startDate.Year) * 12) + (endDate.Month - startDate.Month);
        
        // Si el día final es mayor o igual al día inicial, contar ese mes completo
        if (endDate.Day >= startDate.Day)
            months++;
        
        return months;
    }

    /// <summary>
    /// Calcula cuántas facturas se deben generar según la frecuencia de pago
    /// </summary>
    private int CalculateNumberOfInvoices(DateTime startDate, DateTime endDate, string paymentFrequency)
    {
        int contractMonths = CalculateContractMonths(startDate, endDate);
        int monthsInFrequency = GetMonthsForFrequency(paymentFrequency);

        // Si la frecuencia es mayor que la duración del contrato, solo generar 1 factura
        if (monthsInFrequency > contractMonths)
        {
            return 1; // Una sola factura al inicio
        }

        // Calcular número de facturas según la frecuencia
        int numberOfInvoices = paymentFrequency switch
        {
            "Monthly" => contractMonths,
            "Quarterly" => (int)Math.Ceiling(contractMonths / 3.0),
            "SemiAnnual" => (int)Math.Ceiling(contractMonths / 6.0),
            "Annual" => (int)Math.Ceiling(contractMonths / 12.0),
            _ => throw new ArgumentException($"Frecuencia de pago no válida: {paymentFrequency}")
        };

        // Asegurar que siempre sea al menos 1 factura
        return Math.Max(1, numberOfInvoices);
    }

    private int GetMonthsForFrequency(string paymentFrequency)
    {
        return paymentFrequency switch
        {
            "Monthly" => 1,
            "Quarterly" => 3,
            "SemiAnnual" => 6,
            "Annual" => 12,
            _ => throw new ArgumentException($"Frecuencia no válida: {paymentFrequency}")
        };
    }

    private DateTime CalculateDueDate(DateTime invoiceDate, int? paymentDay)
    {
        // Por defecto: 30 días después de la fecha de factura
        DateTime dueDate = invoiceDate.AddDays(30);

        // Si hay PaymentDay especificado, ajustar al día del mes
        if (paymentDay.HasValue && paymentDay.Value > 0 && paymentDay.Value <= 31)
        {
            int day = paymentDay.Value;
            int lastDayOfMonth = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
            day = Math.Min(day, lastDayOfMonth);
            dueDate = new DateTime(dueDate.Year, dueDate.Month, day);
        }

        return dueDate;
    }

    /// <summary>
    /// Genera múltiples números de factura únicos secuencialmente
    /// </summary>
    private async Task<List<string>> GenerateInvoiceNumbersAsync(int count)
    {
        var year = DateTime.Now.Year;
        
        // Obtener el último número usado del año actual
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

        // Generar todos los números de factura secuencialmente
        var invoiceNumbers = new List<string>();
        for (int i = 0; i < count; i++)
        {
            invoiceNumbers.Add($"INV-{year}-{(nextNumber + i):D3}");
        }

        return invoiceNumbers;
    }

    /// <summary>
    /// Genera un número de factura único en formato INV-{YEAR}-{CONSECUTIVE}
    /// (OBSOLETO: Usar GenerateInvoiceNumbersAsync para múltiples facturas)
    /// </summary>
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

    private void CreateInvoiceItems(Invoice invoice, Contract contract, List<ContractService> contractServices, decimal invoiceAmount)
    {
        // Opción A: Si el contrato tiene FeeAmount, crear 1 item genérico
        if (contract.FeeAmount.HasValue)
        {
            var item = new InvoiceItem
            {
                Description = contract.FeeDescription ?? contract.ServiceDescription ?? "Servicios del contrato",
                Quantity = 1,
                UnitPrice = invoiceAmount,
                Discount = 0,
                LineTotal = invoiceAmount,
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };
            invoice.InvoiceItems.Add(item);
        }
        // Opción B: Si usa ContractServices, crear 1 item por servicio
        else if (contractServices.Any())
        {
            decimal totalServicesAmount = contractServices.Sum(cs => cs.FinalPrice ?? 0);
            
            foreach (var contractService in contractServices)
            {
                // Calcular proporción del monto de este servicio
                decimal serviceFinalPrice = contractService.FinalPrice ?? 0;
                decimal proportion = totalServicesAmount > 0 ? serviceFinalPrice / totalServicesAmount : 0;
                decimal itemAmount = invoiceAmount * proportion;

                // Calcular descuento
                decimal unitPrice = contractService.UnitPrice ?? 0;
                decimal quantity = contractService.Quantity ?? 1;
                decimal subtotal = unitPrice * quantity;
                decimal discountPercentage = contractService.DiscountPercentage ?? 0;
                decimal discount = subtotal * (discountPercentage / 100);

                var item = new InvoiceItem
                {
                    ContractServiceId = contractService.ContractServiceId,
                    Description = contractService.ServiceDescription ?? $"Servicio {contractService.ServiceId}",
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Discount = discount,
                    LineTotal = itemAmount,
                    ServiceOrder = contractService.ServiceOrder,
                    CreatedAt = DateTimeService.GetCostaRicaNow()
                };
                invoice.InvoiceItems.Add(item);
            }

            // Ajustar última línea por redondeo
            if (invoice.InvoiceItems.Any())
            {
                decimal sumItems = invoice.InvoiceItems.Sum(i => i.LineTotal);
                decimal difference = invoiceAmount - sumItems;
                if (difference != 0)
                {
                    var lastItem = invoice.InvoiceItems.Last();
                    lastItem.LineTotal += difference;
                }
            }
        }
    }
}

/// <summary>
/// Request para generar facturas
/// </summary>
public class GenerateInvoicesRequest
{
    public int ContractId { get; set; }
}

/// <summary>
/// Response de generación de facturas
/// </summary>
public class GenerateInvoicesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoicesGenerated { get; set; }
    public int ExistingInvoicesCount { get; set; }
    public int ExpectedInvoicesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<int> InvoiceIds { get; set; } = new();
}

