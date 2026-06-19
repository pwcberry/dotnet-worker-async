using Brunsnik.SimpleWorker;
using Brunsnik.SimpleWorker.Processing;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddLog4NetLogger();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddWindowsService();
builder.Services.AddSingleton<ProcessorContext>();
builder.Services.AddSingleton<FileProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
