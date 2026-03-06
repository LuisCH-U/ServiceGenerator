using Serilog;
using ServiceGenerator;
using ServiceGenerator.Models;
using ServiceGenerator.Repository;
using ServiceGenerator.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<PdfOptionsRoute>(builder.Configuration.GetSection("PdfOptionsRoute"));
//builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("Academico"));
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddSingleton<ComprobanteRepository>();
builder.Services.AddSingleton<GenerarPdfService>();
builder.Services.AddHostedService<Worker>();

//Serilog Configuration
var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var logPath = System.IO.Path.Combine(baseDirectory, "App_data", "logs", "log.txt");

Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var host = builder.Build();

try
{
    Log.Information("Iniciando el servicio");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servicio falló al iniciarse");
}
finally
{
    Log.CloseAndFlush();
}