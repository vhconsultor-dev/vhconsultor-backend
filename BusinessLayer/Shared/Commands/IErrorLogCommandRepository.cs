using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Commands;

public interface IErrorLogCommandRepository
{
    Task<int> CreateAsync(ErrorLog errorLog);
    Task<bool> UpdateAsync(ErrorLog errorLog);
    Task<bool> DeleteAsync(int id);
} 