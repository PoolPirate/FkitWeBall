using FkitWeBall.Config;
using FkitWeBall.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Solnet.Rpc;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Types;

namespace FkitWeBall.Services;
public class TxSender
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger _logger;

    private readonly IReadOnlyList<(IRpcClient, int)> _clients;
    private readonly int _maxIndex;

    public TxSender(IConfiguration configuration, IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<TxSender> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _logger = logger;

        var rpcUrls = _configuration.GetSection("TxRpcUrls").Get<WeightedRPC[]>();

        if (rpcUrls is null || rpcUrls.Length == 0)
        {
            _logger.LogCritical("No TxRpcUrls configured!");
            _lifetime.StopApplication();
            return;
        }

        var clients = new List<(IRpcClient, int)>();
        foreach(var rpcUrl in rpcUrls)
        {
            clients.Add(
                (ClientFactory.GetClient(rpcUrl.Url, _serviceProvider.GetRequiredService<ILogger<IRpcClient>>()),
                rpcUrl.Weight) 
            );
            _maxIndex += rpcUrl.Weight;
        }

        _clients = clients;
    }

    private IRpcClient SelectClient()
    {
        int index = Random.Shared.Next(0, _maxIndex);

        foreach(var (client, weight) in _clients)
        {
            if (index < weight)
            {
                return client;
            }

            index -= weight;
        }

        return _clients[^1].Item1;
    }

    public async Task<RequestResult<string>> SendTransactionAsync(byte[] transaction, bool skipPreflight = false, Commitment commitment = Commitment.Finalized)
    {
        var rpc = SelectClient();
        var response = await rpc.SendTransactionAsync(transaction, skipPreflight, commitment);

        if(response.RawRpcResponse.Contains("credits limited to") || response.RawRpcResponse.Contains("429") || response.RawRpcResponse.Contains("many requests"))
        {
            _logger.LogDebug("Rate limit hit on rpc {rpcUrl}", rpc.NodeAddress);
        }

        return response;
    }
}
