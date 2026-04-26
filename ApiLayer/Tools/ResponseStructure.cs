using ModelLayer.Shared;

namespace ApiLayer.Tools;

public class ResponseStructure<T>
{
    public bool Status { get; set; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageES { get; set; }
    public string? ErrorNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTimeService.GetCostaRicaNow();

    public ResponseStructure()
    {
    }

    public ResponseStructure(bool status, int statusCode, T? data, string message, string? errorNumber = null, string? messageES = null)
    {
        Status = status;
        StatusCode = statusCode;
        Data = data;
        Message = message;
        MessageES = messageES;
        ErrorNumber = errorNumber;
    }

    public static ResponseStructure<T> Success(T data, string message = "Operación exitosa", string? messageES = null)
    {
        return new ResponseStructure<T>(true, 200, data, message, null, messageES);
    }

    public static ResponseStructure<T> Success(T data, int statusCode, string message = "Operación exitosa", string? messageES = null)
    {
        return new ResponseStructure<T>(true, statusCode, data, message, null, messageES);
    }

    public static ResponseStructure<T> Error(string message, int statusCode = 500, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, statusCode, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> BadRequest(string message, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 400, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> NotFound(string message = "Recurso no encontrado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 404, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> Unauthorized(string message = "No autorizado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 401, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> Forbidden(string message = "Acceso denegado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 403, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> Conflict(string message = "Conflicto", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 409, default, message, errorNumber, messageES);
    }

    public static ResponseStructure<T> ValidationError(string message, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure<T>(false, 422, default, message, errorNumber, messageES);
    }
}

public class ResponseStructure : ResponseStructure<object>
{
    public ResponseStructure() : base() { }
    
    public ResponseStructure(bool status, int statusCode, object? data, string message, string? errorNumber = null, string? messageES = null) 
        : base(status, statusCode, data, message, errorNumber, messageES) { }

    public static ResponseStructure Success(string message = "Operación exitosa", string? messageES = null)
    {
        return new ResponseStructure(true, 200, null, message, null, messageES);
    }

    public static ResponseStructure Success(int statusCode, string message = "Operación exitosa", string? messageES = null)
    {
        return new ResponseStructure(true, statusCode, null, message, null, messageES);
    }

    public static new ResponseStructure Error(string message, int statusCode = 500, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, statusCode, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure BadRequest(string message, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 400, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure NotFound(string message = "Recurso no encontrado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 404, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure Unauthorized(string message = "No autorizado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 401, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure Forbidden(string message = "Acceso denegado", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 403, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure Conflict(string message = "Conflicto", string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 409, null, message, errorNumber, messageES);
    }

    public static new ResponseStructure ValidationError(string message, string? errorNumber = null, string? messageES = null)
    {
        return new ResponseStructure(false, 422, null, message, errorNumber, messageES);
    }
} 