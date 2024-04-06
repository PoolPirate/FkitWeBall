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

    private readonly IReadOnlyList<IRpcClient> _clients;

    public TxSender(IConfiguration configuration, IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<TxSender> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _logger = logger;

        var txUrlSection = _configuration.GetSection("TxRpcUrls");

        if(txUrlSection is null)
        {
            _logger.LogCritical("No TxRpcUrls configured!");
            _lifetime.StopApplication();
            return;
        }

        var clients = new List<IRpcClient>();
        foreach(var rpcUrl in txUrlSection.AsEnumerable().Where(x => x.Value is not null).Select(x => x.Value))
        {
            clients.Add(ClientFactory.GetClient(rpcUrl, _serviceProvider.GetRequiredService<ILogger<IRpcClient>>()));
        }

        _clients = clients;
    }

    public Task<RequestResult<string>> SendTransactionAsync(byte[] transaction, int rpcIndex, bool skipPreflight = false, Commitment commitment = Commitment.Finalized)
    {
        var rpc = _clients[rpcIndex % _clients.Count];
        return rpc.SendTransactionAsync(transaction, skipPreflight, commitment);
    }
}
