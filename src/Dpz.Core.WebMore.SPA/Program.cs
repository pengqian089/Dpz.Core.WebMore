using Dpz.Core.WebMore;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<WebMoreApp>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddWebMoreUi(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

await builder.Build().RunAsync();
