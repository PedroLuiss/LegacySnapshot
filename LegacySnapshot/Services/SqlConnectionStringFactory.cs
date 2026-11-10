using LegacySnapshot.Models;
using Microsoft.Data.SqlClient;

namespace LegacySnapshot.Services;

public static class SqlConnectionStringFactory
{
    public static string Create(ConnectionModel connection, string? password)
    {
        var usesWindowsAuthentication = connection.AuthenticationType == "Windows";
        if (connection.TrustServerCertificate && connection.Environment != "Dev")
        {
            throw new InvalidOperationException(
                "Confiar en un certificado no validado solo se permite para conexiones de desarrollo.");
        }

        if (usesWindowsAuthentication && DeviceInfo.Platform != DevicePlatform.WinUI)
        {
            throw new InvalidOperationException("La autenticación integrada de Windows solo está disponible en Windows.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = connection.Server,
            InitialCatalog = connection.Database,
            IntegratedSecurity = usesWindowsAuthentication,
            Encrypt = true,
            TrustServerCertificate = connection.TrustServerCertificate,
            ConnectTimeout = 10,
            PersistSecurityInfo = false
        };

        if (!usesWindowsAuthentication)
        {
            builder.UserID = connection.UserName;
            builder.Password = password ?? string.Empty;
        }

        return builder.ConnectionString;
    }
}
