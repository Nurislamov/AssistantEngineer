using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.SharedKernel.Primitives;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Infrastructure.Services.Benchmarks;

public sealed class EnergyPlusBenchmarkRunner : IEnergyPlusBenchmarkRunner
{
    private readonly EnergyPlusBenchmarkOptions _options;
    private readonly ILogger<EnergyPlusBenchmarkRunner> _logger;
    private readonly IDockerClient? _dockerClient;

    public EnergyPlusBenchmarkRunner(
        IOptions<EnergyPlusBenchmarkOptions> options,
        ILogger<EnergyPlusBenchmarkRunner> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.UseDocker)
        {
            var dockerUri = GetDockerUri();
            _dockerClient = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
        }
    }

    private string GetDockerUri()
    {
        if (!string.IsNullOrWhiteSpace(_options.DockerUri))
            return _options.DockerUri;

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";
    }

    public async Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequest(request);
        if (validation.IsFailure)
            return Result<EnergyPlusBenchmarkResult>.Failure(validation);

        return _options.UseDocker
            ? await RunWithDockerAsync(request, cancellationToken)
            : await RunLocallyAsync(request, cancellationToken);
    }

    private async Task<Result<EnergyPlusBenchmarkResult>> RunWithDockerAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        if (_dockerClient == null)
            return Result<EnergyPlusBenchmarkResult>.Failure("Docker client is not initialized.");

        var dockerClient = _dockerClient;
        var modelPath = Path.GetFullPath(request.ModelPath);
        var weatherPath = Path.GetFullPath(request.WeatherFilePath);
        var outputPath = Path.GetFullPath(request.OutputDirectory);
        var modelDir = Path.GetDirectoryName(modelPath)!;
        var weatherDir = Path.GetDirectoryName(weatherPath)!;
        var modelFileName = Path.GetFileName(modelPath);
        var weatherFileName = Path.GetFileName(weatherPath);

        Directory.CreateDirectory(outputPath);

        var args = new List<string>
        {
            "-w", $"/weather/{weatherFileName}",
            "-d", "/output"
        };
        args.AddRange(request.AdditionalArguments);
        args.Add($"/model/{modelFileName}");

        var createParameters = new CreateContainerParameters
        {
            Image = _options.DockerImage,
            Cmd = args.ToArray(),
            AttachStdout = true,
            AttachStderr = true,
            HostConfig = new HostConfig
            {
                Binds =
                [
                    $"{modelDir}:/model:ro",
                    $"{weatherDir}:/weather:ro",
                    $"{outputPath}:/output"
                ],
                AutoRemove = false
            }
        };

        var container = await dockerClient.Containers.CreateContainerAsync(createParameters, cancellationToken);
        var containerId = container.ID;
        _logger.LogInformation("Created EnergyPlus Docker container {ContainerId}", containerId);

        try
        {
            await dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);

            var (exitCode, stdout, stderr) = await WaitForContainerAsync(
                dockerClient,
                containerId,
                cancellationToken);

            _logger.LogInformation(
                "EnergyPlus container {ContainerId} finished with exit code {ExitCode}.",
                containerId,
                exitCode);

            return Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
            {
                Succeeded = exitCode == 0,
                ExitCode = (int)exitCode,
                OutputDirectory = request.OutputDirectory,
                StandardOutput = stdout,
                StandardError = stderr
            });
        }
        finally
        {
            await RemoveContainerIfExistsAsync(dockerClient, containerId);
        }
    }

    private async Task<(long exitCode, string stdout, string stderr)> WaitForContainerAsync(
        IDockerClient dockerClient,
        string containerId,
        CancellationToken cancellationToken)
    {
        await using var stdout = new BoundedUtf8LogStream(_options.MaxCapturedLogCharacters);
        await using var stderr = new BoundedUtf8LogStream(_options.MaxCapturedLogCharacters);
        using var logStream = await dockerClient.Containers.GetContainerLogsAsync(
            containerId,
            false,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true },
            cancellationToken);

        await logStream.CopyOutputToAsync(Stream.Null, stdout, stderr, cancellationToken);
        var inspect = await dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
        return (inspect.State.ExitCode, stdout.GetText(), stderr.GetText());
    }

    private async Task RemoveContainerIfExistsAsync(IDockerClient dockerClient, string containerId)
    {
        try
        {
            await dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true },
                CancellationToken.None);
        }
        catch (DockerContainerNotFoundException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to remove EnergyPlus Docker container {ContainerId}.",
                containerId);
        }
    }

    private async Task<Result<EnergyPlusBenchmarkResult>> RunLocallyAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var arguments = new List<string>
        {
            "-w", request.WeatherFilePath,
            "-d", request.OutputDirectory
        };
        arguments.AddRange(request.AdditionalArguments);
        arguments.Add(request.ModelPath);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            return Result<EnergyPlusBenchmarkResult>.Failure(
                $"EnergyPlus executable '{_options.ExecutablePath}' could not be started: {exception.Message}");
        }

        var stdoutTask = ReadTextWithLimitAsync(process.StandardOutput, cancellationToken);
        var stderrTask = ReadTextWithLimitAsync(process.StandardError, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
        {
            Succeeded = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            OutputDirectory = request.OutputDirectory,
            StandardOutput = stdout,
            StandardError = stderr
        });
    }

    private async Task<string> ReadTextWithLimitAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        var captured = new StringBuilder(capacity: Math.Min(_options.MaxCapturedLogCharacters, 4096));
        var omittedCharacters = 0;

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;

            var remaining = _options.MaxCapturedLogCharacters - captured.Length;
            if (remaining > 0)
                captured.Append(buffer, 0, Math.Min(read, remaining));

            omittedCharacters += Math.Max(0, read - Math.Max(remaining, 0));
        }

        if (omittedCharacters > 0)
            captured.AppendLine().Append("[truncated ").Append(omittedCharacters).Append(" characters]");

        return captured.ToString();
    }

    private Result ValidateRequest(EnergyPlusBenchmarkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ModelPath))
            return Result.Validation("EnergyPlus model path is required.");
        if (string.IsNullOrWhiteSpace(request.WeatherFilePath))
            return Result.Validation("EnergyPlus weather file path is required.");
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            return Result.Validation("EnergyPlus output directory is required.");
        if (!_options.UseDocker && string.IsNullOrWhiteSpace(_options.ExecutablePath))
            return Result.Validation("EnergyPlus executable path is required for local execution.");
        if (_options.MaxCapturedLogCharacters <= 0)
            return Result.Validation("EnergyPlus max captured log characters must be positive.");

        if (!File.Exists(request.ModelPath))
            return Result.NotFound($"EnergyPlus model file '{request.ModelPath}' was not found.");
        if (!File.Exists(request.WeatherFilePath))
            return Result.NotFound($"EnergyPlus weather file '{request.WeatherFilePath}' was not found.");

        return Result.Success();
    }

    private sealed class BoundedUtf8LogStream : Stream
    {
        private readonly MemoryStream _buffer = new();
        private readonly int _maxBytes;
        private long _omittedBytes;

        public BoundedUtf8LogStream(int maxBytes)
        {
            _maxBytes = maxBytes;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _buffer.Length;

        public override long Position
        {
            get => _buffer.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            var remaining = _maxBytes - (int)_buffer.Length;
            if (remaining > 0)
                _buffer.Write(buffer, offset, Math.Min(count, remaining));

            _omittedBytes += Math.Max(0, count - Math.Max(remaining, 0));
        }

        public string GetText()
        {
            var text = Encoding.UTF8.GetString(_buffer.ToArray());
            return _omittedBytes > 0
                ? $"{text}{Environment.NewLine}[truncated {_omittedBytes} bytes]"
                : text;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _buffer.Dispose();
            base.Dispose(disposing);
        }
    }
}
