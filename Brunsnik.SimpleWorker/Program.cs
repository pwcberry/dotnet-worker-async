using Brunsnik.SimpleWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddLog4NetLogger();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
