using System.IO.Pipelines;
using cw9cwcw.Models.DTo;
using Microsoft.Data.SqlClient;
namespace cw9cwcw.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;
    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    public async Task<int> AddProductToWarehouseAsync(WarehouseProductDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            //sprawdzanie czy produkt istnieje
            var doesProductExist = new SqlCommand("SELECT COUNT(*) FROM Products WHERE IdProduct = @Id", connection, transaction);
            doesProductExist.Parameters.AddWithValue("@Id", dto.IdProduct);
            var productCount = (int)(await doesProductExist.ExecuteScalarAsync());
            if (productCount == 0)
            {
                throw new Exception("Product doesn't exist");
            }
            
            //sprawdzamy czy storage istnieje
            var doesStorageExists = new SqlCommand("SELECT COUNT(*) FROM Storage WHERE IdWarehouse = @Id", connection, transaction);
            doesStorageExists.Parameters.AddWithValue("@Id", dto.IdWarehouse);
            if ((await doesStorageExists.ExecuteScalarAsync()) == null)
            {
                throw new Exception("Storage doesn't exist");
            }

            if (dto.amount <= 0)
            {
                throw new Exception("Amount of product doesn't exist");
            }
            
            var query = new SqlCommand(@"
    SELECT IdOrder, Price
    FROM [Order]
    JOIN Product ON [Order].ProductId = Product.IdProduct
    WHERE [Order].ProductId = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt
    AND FulfilledAt IS NULL", connection, transaction);
            query.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            query.Parameters.AddWithValue("@Amount", dto.amount);
            query.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            
            //spraw
            var reader = await query.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                throw new Exception("No products found");
            }
            await reader.ReadAsync();
            var orderId = reader.GetInt32(0);
            var price = reader.GetInt32(1);
            await reader.CloseAsync();

            try
            {
                var fulfilledCheck = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder",
                    connection, transaction);
                fulfilledCheck.Parameters.AddWithValue("@IdOrder", orderId);
                if ((await fulfilledCheck.ExecuteScalarAsync()) != null)
                    throw new InvalidOperationException("Zamówienie już zostało zrealizowane");
            }
            catch
            {
                Console.WriteLine("nie dziala 1");
            }

            var updateOrder = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", connection, transaction);
            updateOrder.Parameters.AddWithValue("@IdOrder", orderId);
            await updateOrder.ExecuteNonQueryAsync();
            
            var insertCmd = new SqlCommand(@"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                OUTPUT INSERTED.IdProductWarehouse
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE())", connection, transaction);
            Console.WriteLine("nie dziala 2");
            try
            {
                insertCmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
                insertCmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                insertCmd.Parameters.AddWithValue("@IdOrder", orderId);
                insertCmd.Parameters.AddWithValue("@Amount", dto.amount);
                insertCmd.Parameters.AddWithValue("@Price", price * dto.amount);
                var insertedId = (int)await insertCmd.ExecuteScalarAsync();
                transaction.Commit(); // Zatwierdzenie
                return insertedId;
            }catch
            {
                Console.WriteLine("nie dziala 2");
                transaction.Rollback(); // Wycofanie
                throw;
            }

        }
        catch
        {
            transaction.Rollback(); // Wycofanie
            throw;
        }

    }
    
    


    
}