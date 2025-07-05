using Jox.SolutionAnalyzer;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings()
{
    Args = args,
#if DEBUG
    EnvironmentName = Environments.Development,
#endif
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
ConsoleApp.ServiceProvider = scope.ServiceProvider;

var app = ConsoleApp.Create();
app.Add<CliCommands>();
app.Run(args);
