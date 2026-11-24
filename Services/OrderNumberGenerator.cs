using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IsanDer.Services;

public static class OrderNumberGenerator
{
    public const string Prefix = "ORDER";

    public static string Format(int sequenceNumber) =>
        $"{Prefix}{sequenceNumber:D4}";

    public static async Task<string> AllocateNextAsync(
        IsanDer.Models.IsanDerDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException("Order number allocation requires an active database transaction.");
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT IGNORE INTO order_number_sequences (SequenceID, LastNumber)
            VALUES (1, 0)
            """,
            cancellationToken);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = """
            SELECT LastNumber
            FROM order_number_sequences
            WHERE SequenceID = 1
            FOR UPDATE
            """;

        var lastNumberValue = await command.ExecuteScalarAsync(cancellationToken);
        var lastNumber = Convert.ToInt32(lastNumberValue);
        var nextNumber = lastNumber + 1;

        command.CommandText = """
            UPDATE order_number_sequences
            SET LastNumber = @nextNumber
            WHERE SequenceID = 1
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@nextNumber";
        parameter.Value = nextNumber;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return Format(nextNumber);
    }
}
