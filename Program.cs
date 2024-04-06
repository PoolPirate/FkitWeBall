// To initialize a wallet and have access to the same keys generated in sollet (the default)
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FkitWeBall.Services;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using Solnet.Rpc;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ProverService>();
builder.Services.AddSingleton<MinerService>();
builder.Services.AddSingleton<TxSender>();

builder.Services.AddSingleton(provider =>
{
    var rpcUrl = provider.GetRequiredService<IConfiguration>()["MainRpcUrl"];
    return ClientFactory.GetClient(rpcUrl, provider.GetRequiredService<ILogger<IRpcClient>>());
});

var host = builder.Build();

host.Start();

_ = host.Services.GetRequiredService<MinerService>().RunAsync();   

await host.WaitForShutdownAsync();