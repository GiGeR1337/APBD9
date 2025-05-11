using System.Data;
using apbd9.Models;
using Microsoft.Data.SqlClient;

namespace apbd9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductManuallyAsync(Warehouse warehouse)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        await using var command = new SqlCommand();
        command.Connection = connection;

        using var transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            
            command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            var priceObj = await command.ExecuteScalarAsync();
            if (priceObj == null)
                throw new ArgumentException("Product not found");

            decimal price = (decimal)priceObj;

            
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
            var warehouseExists = await command.ExecuteScalarAsync();
            if (warehouseExists == null)
                throw new ArgumentException("Warehouse not found");

           
            command.Parameters.Clear();
            command.CommandText = @"
                SELECT TOP 1 o.IdOrder FROM [Order] o
                LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
                WHERE o.IdProduct = @IdProduct AND o.Amount = @Amount
                      AND pw.IdProductWarehouse IS NULL AND o.CreatedAt < @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedAt);
            var orderIdObj = await command.ExecuteScalarAsync();
            if (orderIdObj == null)
                throw new ArgumentException("No valid order found");

            int idOrder = (int)orderIdObj;

            
            command.Parameters.Clear();
            command.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedAt);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            await command.ExecuteNonQueryAsync();

            
            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            command.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            command.Parameters.AddWithValue("@Price", warehouse.Amount * price);
            command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedAt);
            var newId = (int)await command.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AddProductWithProcedureAsync(Warehouse warehouse)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", warehouse.Amount);
        command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedAt);

        await connection.OpenAsync();

        try
        {
            var result = await command.ExecuteScalarAsync();
            if (result == null)
                throw new ArgumentException("Procedure did not return an ID.");

            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            throw new ArgumentException(ex.Message);
        }
    }
}
