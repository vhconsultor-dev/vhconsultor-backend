using BusinessLayer.BrandPartner.Commands;
using FluentValidation;

namespace BusinessLayer.BrandPartner.Validators;

public class BulkUploadInventoryValidator : AbstractValidator<BulkUploadInventoryRequest>
{
    public BulkUploadInventoryValidator()
    {
        RuleFor(x => x.AmazonAccountId)
            .GreaterThan(0)
            .WithMessage("Amazon Account ID is required and must be greater than zero.");

        RuleFor(x => x.ExcelFile)
            .NotNull()
            .WithMessage("Excel file is required.");

        RuleFor(x => x.ExcelFile)
            .Must(file => file != null && file.Length > 0)
            .When(x => x.ExcelFile != null)
            .WithMessage("Excel file cannot be empty.");

        RuleFor(x => x.ExcelFile)
            .Must(BeAValidExcelFile)
            .When(x => x.ExcelFile != null)
            .WithMessage("File must be an Excel file (.xlsx or .xls).");

        RuleFor(x => x.ExcelFile)
            .Must(file => file != null && file.Length <= 10 * 1024 * 1024)
            .When(x => x.ExcelFile != null)
            .WithMessage("File size must not exceed 10 MB.");
    }

    private bool BeAValidExcelFile(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            return false;

        var allowedContentTypes = new[]
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
        };

        return allowedContentTypes.Contains(file.ContentType);
    }
}
