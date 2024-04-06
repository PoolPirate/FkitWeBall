using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solnet.Wallet.Utilities;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace FkitWeBall.Services;
public class ProverService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    public ProverService(ILogger<ProverService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<(byte[], byte[], ulong)> ProveAsync(string keyFilePath)
    {
        var processInfo = new ProcessStartInfo(OperatingSystem.IsWindows() ? "ore.exe" : "ore")
        {
            Arguments = $"--keypair {keyFilePath} --rpc {_config["MainRpcUrl"]} busses",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(processInfo)!;

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogCritical("Unexpected exit. Output={output}", await process.StandardError.ReadToEndAsync());
            throw new Exception("Process Exited unexpectedly");
        }

        var encoder = new Base58Encoder();

        byte[] pubKey;
        byte[] hash;
        ulong nonce;

        string?[] lines = [
            await process.StandardOutput.ReadLineAsync(),
            await process.StandardOutput.ReadLineAsync(),
            await process.StandardOutput.ReadLineAsync()
        ];

        try
        {
            pubKey = encoder.DecodeData(lines[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed parsing Pubkey {pubKey}", lines[0]);
            throw;
        }

        try
        {
            hash = encoder.DecodeData(lines[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed parsing Proof {pubKey}", lines[1]);
            throw;
        }

        try
        {
            nonce = ulong.Parse(lines[2]!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed parsing Nonce {pubKey}", lines[2]);
            throw;
        }

        return (pubKey, hash, nonce);
    }
}
