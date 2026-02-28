using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Corporate.Validators;

public class BulkUploadAmazonAccountAsinsValidator : AbstractValidator<BulkUploadAmazonAccountAsinsRequest>
{
    public BulkUploadAmazonAccountAsinsValidator()
    {
        RuleFor(x => x.AmazonAccountId)
            .GreaterThan(0)
            .WithMessage("AmazonAccountId must be greater than 0");

        RuleFor(x => x.ExcelFile)
            .NotNull()
            .WithMessage("Excel file is required")
            .Must(BeAValidExcelFile)
            .WithMessage("File must be an Excel file (.xlsx or .xls)")
            .Must(file => file.Length > 0)
            .WithMessage("Excel file cannot be empty");
    }

    private bool BeAValidExcelFile(IFormFile? file)
    {
        if (file == null)
            return false;

        var validExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return validExtensions.Contains(extension);
    }
}

public class BulkUploadAmazonAccountAsinsRequest
{
    public int AmazonAccountId { get; set; }
    public IFormFile ExcelFile { get; set; } = null!;
}
