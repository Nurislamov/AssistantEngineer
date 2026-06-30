using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;

namespace AssistantEngineer.Api.Configuration;

internal static class DataProtectionRegistration
{
    internal const string ApplicationName = "AssistantEngineer";
    internal const string KeysPathVariable = "ASSISTANTENGINEER_DATAPROTECTION_KEYS_PATH";
    internal const string CertificatePathVariable = "ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PATH";
    internal const string CertificatePasswordVariable = "ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PASSWORD";

    public static WebApplicationBuilder ConfigureDataProtection(
        this WebApplicationBuilder builder)
    {
        var keysPath = builder.Configuration[KeysPathVariable];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            keysPath = GetDefaultKeysPath();
        }

        var keysDirectory = Directory.CreateDirectory(Path.GetFullPath(keysPath));
        var dataProtection = builder.Services
            .AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(keysDirectory);

        var certificatePath = builder.Configuration[CertificatePathVariable];
        var certificatePassword = builder.Configuration[CertificatePasswordVariable];

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!string.IsNullOrEmpty(certificatePassword))
            {
                throw new InvalidOperationException(
                    $"{CertificatePathVariable} is required when a DataProtection certificate password is configured.");
            }

            return builder;
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            Path.GetFullPath(certificatePath),
            certificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);

        dataProtection.ProtectKeysWithCertificate(certificate);
        return builder;
    }

    private static string GetDefaultKeysPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            userProfile = AppContext.BaseDirectory;
        }

        return Path.Combine(userProfile, ".aspnet", "DataProtection-Keys");
    }
}
