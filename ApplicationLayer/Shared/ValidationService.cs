using FluentValidation;

namespace ApplicationLayer.Shared;

public class ValidationService
{
    #region Fields
    private readonly IServiceProvider _serviceProvider;
    #endregion

    #region Constructor
    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    #endregion

    #region Public Methods
    public async Task<ValidationResult> ValidateAsync<T>(T request)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
        
        if (validator == null)
        {
            return new ValidationResult { IsValid = true };
        }

        var result = await validator.ValidateAsync(request);
        
        return new ValidationResult
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(e => e.ErrorMessage).ToList()
        };
    }
    #endregion
}

#region Validation Result
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
#endregion 