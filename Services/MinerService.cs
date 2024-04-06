using FkitWeBall.Models;
using FkitWeBall.Programs;
using FkitWeBall.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text.Json;

namespace FkitWeBall.Services;
public class MinerService
{
    private readonly ProverService _prover;
    private readonly ILogger _logger;
    private readonly IRpcClient _rpcClient;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TxSender _txSender;

    private readonly int _spamDelay;

    public MinerService(ProverService prover, ILogger<MinerService> logger, IRpcClient rpcClient, IHostApplicationLifetime lifetime,
        IConfiguration configuration, TxSender txSender)
    {
        _prover = prover;
        _logger = logger;
        _rpcClient = rpcClient;
        _lifetime = lifetime;
        _txSender = txSender;

        _spamDelay = int.Parse(configuration["SpamDelay"] ?? "600");
    }

    public async Task RunAsync()
    {
        var keys = Directory.GetFiles("keys")
            .Where(x => x.EndsWith(".json")).ToArray();

        if(keys.Length == 0)
        {
            _logger.LogCritical("No Keys found. Check your keys directory!");
            _lifetime.StopApplication();
            return;
        }

        await Task.WhenAll(keys.Select(async keyfile =>
        {
            var key = File.ReadAllText(keyfile);
            var arr2 = JsonSerializer.Deserialize<int[]>(key);
            var arr = arr2!.Select(x => (byte) x).ToArray();

            var lastSubmitted = DateTimeOffset.MinValue;

            while(true)
            {
                try
                {
                    _logger.LogDebug("Generating proof for {keyfile}", keyfile);
                    var (pubKey, hash, nonce) = await _prover.ProveAsync(keyfile);
                    var account = new Account(arr, pubKey);

                    _logger.LogDebug("Submitting proof for {keyfile} ({pubKey})", keyfile, account.PublicKey.Key);
                    var bus = Accounts.BUSSES[Random.Shared.Next(1, Accounts.BUSSES.Length)];
                    var instruction = OreProgram.Mine(account.PublicKey, bus, hash, nonce);

                    if(lastSubmitted.AddSeconds(40) > DateTimeOffset.UtcNow)
                    {
                        await Task.Delay(lastSubmitted.AddSeconds(40) - DateTimeOffset.UtcNow);
                    }

                    await SendAndConfirmHashAsync(account, keyfile, hash, nonce);
                    lastSubmitted = DateTimeOffset.UtcNow;
                }
                catch(Exception ex)
                {
                    _logger.LogCritical(ex, "Exception occured");
                }
            }
        }));
    }

    private async Task<(byte[], string)> MakeMiningTxAsync(Account account, byte[] hash, ulong nonce,
        string? currentBlockHash = null)
    {
        while(true)
        {
            try
            {
                var bus = Accounts.BUSSES[Random.Shared.Next(1, Accounts.BUSSES.Length)];
                var instruction = OreProgram.Mine(account.PublicKey, bus, hash, nonce);

                var blockHash = currentBlockHash
                    ?? (await _rpcClient.GetLatestBlockHashAsync()).Result.Value.Blockhash;

                var tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash)
                    .SetFeePayer(account.PublicKey)
                    .AddInstruction(ComputeBudgetProgram.SetComputeUnitLimit(3200))
                    .AddInstruction(ComputeBudgetProgram.SetComputeUnitPrice(1200000))
                    .AddInstruction(instruction)
                    .Build(account);

                return (tx, blockHash);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Making mining tx failed");
            }
        }
    }

    private async Task SendAndConfirmHashAsync(Account account, string keyFile, byte[] hash, ulong nonce)
    {
        var (tx, blockHash) = await MakeMiningTxAsync(account, hash, nonce);

        while(true)
        {
            var inputUpdate = await SendTillRequireNewInputAsync(tx, account, keyFile);

            switch(inputUpdate)
            {
                case MineTxInputUpdate.Hash:
                    return;
                case MineTxInputUpdate.Bus:
                    (tx, _) = await MakeMiningTxAsync(account, hash, nonce, blockHash);
                    break;
                case MineTxInputUpdate.Other:
                    (tx, blockHash) = await MakeMiningTxAsync(account, hash, nonce);
                    break;
                case MineTxInputUpdate.Slot:
                    (tx, blockHash) = await MakeMiningTxAsync(account, hash, nonce);
                    break;
            }
        }
    }

    private async Task<MineTxInputUpdate> SendTillRequireNewInputAsync(byte[] tx, Account account, string keyFile)
    {
        while(true)
        {
            var status = await TryPublishMineTransactionAsync(tx);

            switch(status)
            {
                case MineTxStatus.Submitted:
                    await Task.Delay(_spamDelay);
                    break;
                case MineTxStatus.Confirmed:
                    _logger.LogInformation("Block mined! Key: {keyFile} ({pubKey})", keyFile, account.PublicKey.Key);
                    return MineTxInputUpdate.Hash;
                case MineTxStatus.BusBusy:
                    return MineTxInputUpdate.Bus;
                case MineTxStatus.NeedsReset:
                    await Task.Delay(1000);
                    break;
                case MineTxStatus.RateLimited:
                    await Task.Delay(250);
                    break;
                case MineTxStatus.SlotEnded:
                    return MineTxInputUpdate.Slot;
                case MineTxStatus.Error:
                    await Task.Delay(500);
                    return MineTxInputUpdate.Other;
            }
        }
    }

    private async Task<MineTxStatus> TryPublishMineTransactionAsync(byte[] transaction)
    {
        try
        {
            var sendResponse = await _txSender.SendTransactionAsync(transaction, skipPreflight: false, commitment: Solnet.Rpc.Types.Commitment.Confirmed);

            if(sendResponse is null)
            {
                _logger.LogWarning("Rpc did not return response while trying to send mine tx");
                return MineTxStatus.Unknown;
            }

            if(sendResponse is null || sendResponse.RawRpcResponse.Contains("credits limited to") 
                || sendResponse.RawRpcResponse.Contains("429") 
                || sendResponse.RawRpcResponse.Contains("many requests"))
            {
                return MineTxStatus.RateLimited;
            }
            else if(sendResponse.RawRpcResponse.Contains("custom program error: 0x3") || sendResponse.RawRpcResponse.Contains("already"))
            {
                return MineTxStatus.Confirmed;
            }
            else if(sendResponse.RawRpcResponse.Contains("custom program error: 0x1"))
            {
                return MineTxStatus.NeedsReset;
            }
            else if(sendResponse.RawRpcResponse.Contains("custom program error: 0x5")) //Bus does not have enough rewards
            {
                return MineTxStatus.BusBusy;
            }
            else if (sendResponse.RawRpcResponse.Contains("Blockhash not found"))
            {
                return MineTxStatus.SlotEnded;
            }
            else if(sendResponse.RawRpcResponse.Contains("message") || sendResponse.Result is null)
            {
                _logger.LogWarning("{}", sendResponse.RawRpcResponse);
                return MineTxStatus.Error;
            }
            //
            return MineTxStatus.Submitted;
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"There was an exception while sending mine tx");
            return MineTxStatus.Unknown;
        }
    }
}
