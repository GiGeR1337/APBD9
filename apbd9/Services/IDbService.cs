using apbd9.Models;

namespace apbd9.Services;

public interface IDbService
{
    Task<int> AddProductManuallyAsync(Warehouse request);
    Task<int> AddProductWithProcedureAsync(Warehouse request);

}