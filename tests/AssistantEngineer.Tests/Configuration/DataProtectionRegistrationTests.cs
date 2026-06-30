using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AssistantEngineer.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Configuration;

public sealed class DataProtectionRegistrationTests
{
    [Fact]
    public void ConfigurationUsesStableApplicationNameAndConfiguredKeyDirectory()
    {
        using var directory = new TemporaryDirectory();
        var keysPath = Path.Combine(directory.Path, "keys");
        var builder = CreateBuilder(keysPath);

        builder.ConfigureDataProtection();

        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        var provider = services.GetRequiredService<IDataProtectionProvider>();

        _ = provider.CreateProtector("ED-24OPS.3").Protect("probe");

        Assert.Equal(DataProtectionRegistration.ApplicationName, options.ApplicationDiscriminator);
        Assert.True(Directory.Exists(keysPath));
        Assert.Single(Directory.GetFiles(keysPath, "key-*.xml"));
    }

    [Fact]
    public void MissingCertificateConfigurationDoesNotFailStartup()
    {
        using var directory = new TemporaryDirectory();
        var builder = CreateBuilder(Path.Combine(directory.Path, "keys"));

        var exception = Record.Exception(builder.ConfigureDataProtection);

        Assert.Null(exception);
    }

    [Fact]
    public void CertificateConfigurationEncryptsPersistedKeysWithoutWritingPassword()
    {
        using var directory = new TemporaryDirectory();
        const string password = "test-only-password";
        var certificatePath = Path.Combine(directory.Path, "dataprotection-test.pfx");
        File.WriteAllBytes(certificatePath, CreateCertificate(password));

        var keysPath = Path.Combine(directory.Path, "keys");
        var builder = CreateBuilder(keysPath);
        builder.Configuration[DataProtectionRegistration.CertificatePathVariable] = certificatePath;
        builder.Configuration[DataProtectionRegistration.CertificatePasswordVariable] = password;

        builder.ConfigureDataProtection();

        using var services = builder.Services.BuildServiceProvider();
        var provider = services.GetRequiredService<IDataProtectionProvider>();
        _ = provider.CreateProtector("ED-24OPS.3").Protect("probe");

        var keyXml = File.ReadAllText(Assert.Single(Directory.GetFiles(keysPath, "key-*.xml")));
        Assert.Contains("encryptedSecret", keyXml, StringComparison.Ordinal);
        Assert.DoesNotContain(password, keyXml, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordWithoutCertificatePathFailsWithoutExposingPassword()
    {
        using var directory = new TemporaryDirectory();
        const string password = "test-only-password";
        var builder = CreateBuilder(Path.Combine(directory.Path, "keys"));
        builder.Configuration[DataProtectionRegistration.CertificatePasswordVariable] = password;

        var exception = Assert.Throws<InvalidOperationException>(builder.ConfigureDataProtection);

        Assert.Contains(DataProtectionRegistration.CertificatePathVariable, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
    }

    private static WebApplicationBuilder CreateBuilder(string keysPath)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.Configuration[DataProtectionRegistration.KeysPathVariable] = keysPath;
        return builder;
    }

    private static byte[] CreateCertificate(string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=AssistantEngineer DataProtection Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pfx, password);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "AssistantEngineer.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
