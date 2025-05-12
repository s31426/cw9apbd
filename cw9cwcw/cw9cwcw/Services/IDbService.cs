using cw9cwcw.Models.DTo;

namespace cw9cwcw.Services;

public interface IDbService
{
    Task<int> AddProductToWarehouseAsync(WarehouseProductDto dto);
}