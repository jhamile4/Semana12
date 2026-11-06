using Npgsql;

namespace TallerMecanico.Data;

public static class DatabaseConnection
{
    public static string Normalize(string value)
    {
        var raw = value.Trim();
        if (raw.Length >= 2 && ((raw[0] == '\'' && raw[^1] == '\'') ||
            (raw[0] == '"' && raw[^1] == '"')))
            raw = raw[1..^1];

        try
        {
            if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                return new NpgsqlConnectionStringBuilder(raw).ConnectionString;

            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                throw new ArgumentException();
            var credentials = uri.UserInfo.Split(':', 2);
            var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            if (credentials.Length != 2 || credentials[0].Length == 0 ||
                database.Length == 0 || string.IsNullOrWhiteSpace(uri.Host))
                throw new ArgumentException();

            var connection = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port < 1 ? 5432 : uri.Port,
                Database = database,
                Username = Uri.UnescapeDataString(credentials[0]),
                Password = Uri.UnescapeDataString(credentials[1]),
                SslMode = SslMode.VerifyFull,
                Timeout = 30
            };
            connection["Channel Binding"] = "Require";
            return connection.ConnectionString;
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Conexion invalida. Use la URI postgresql:// de Neon o una cadena ADO.NET en ConnectionStrings__DefaultConnection.");
        }
    }
}
