using Microsoft.Extensions.Options;
using ServiceGenerator.Models;
using ServiceGenerator.Repository;
using ServiceGenerator.Services;
using System.IO;

namespace ServiceGenerator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly PdfOptionsRoute _pdfOptionsRoute;
        private SemaphoreSlim _parallel;
        private readonly ComprobanteRepository _comprobanteRepository;
        private readonly GenerarPdfService _generarPdfService;

        public Worker(ILogger<Worker> logger, IOptions<PdfOptionsRoute> pdfOptionsRoute, ComprobanteRepository comprobanteRepository, GenerarPdfService generarPdfService)
        {
            _logger = logger;
            _pdfOptionsRoute = pdfOptionsRoute.Value;
            _parallel = new SemaphoreSlim(_pdfOptionsRoute.MaxParallel);
            _comprobanteRepository = comprobanteRepository;
            _generarPdfService = generarPdfService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Worker iniciado. Esperando comprobantes...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var comprobantes = await _comprobanteRepository.ComprobantesAsync();
                    Console.WriteLine($"Comprobantes obtenidos: {comprobantes.Count}");

                    if (comprobantes == null)
                    {
                        _logger.LogInformation("No hay comprobantes pendientes.");
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }

                    Console.WriteLine($"Procesando {comprobantes.Count} comprobantes...");
                    var lotes = comprobantes
                        .Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / _pdfOptionsRoute.BatchSize)
                        .Select(g => g.Select(x => x.item).ToList())
                        .ToList();

                    foreach (var lote in lotes)
                    {
                        var tasks = lote.Select(c => ProcesarComprobanteAsync(c, stoppingToken));
                        await Task.WhenAll(tasks);
                    }

                    _logger.LogInformation("Proceso finalizado. Total registros: {Total}", comprobantes);
                    Console.WriteLine("Proceso finalizado. Total registros: " + comprobantes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general en el worker");
                    Console.WriteLine("Error general en el worker: " + ex.Message);
                }

                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcesarComprobanteAsync(ComprobantesModel comprobante, CancellationToken stoppingToken)
        {
            Console.WriteLine($"Procesando comprobante: {comprobante.NumeroDocumento}");

            await _parallel.WaitAsync(stoppingToken);
            try
            {
                _logger.LogInformation("Procesando comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);

                DatosComprobanteModel data = await _comprobanteRepository.ObtenerComprobantesDatosAsync(comprobante.TipoDocumento ?? "", comprobante.NumeroDocumento ?? "", comprobante.Sucursal ?? "");
                Console.WriteLine($"Datos obtenidos para comprobante: {comprobante.NumeroDocumento}");

                Console.WriteLine($"Obteniendo el template.html");
                var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "template.html");
                var html = await File.ReadAllTextAsync(templatePath, stoppingToken);

                // Ejemplo manual de reemplazos simples
                //html = html.Replace("{{Alumno}}", data._headerDocument?.Alumno ?? "");
                //html = html.Replace("{{DNI}}", data._headerDocument?.DNI ?? "");
                //html = html.Replace("{{Total}}", data._headerDocument?.Total?.ToString() ?? "");
                //
                var rows = "";
                
                //foreach (var item in data._detailData)
                //{
                //    rows += $"<tr><td>{item.Concepto}</td><td>{item.Monto}</td></tr>";
                //}

                html = html.Replace("{{Rows}}", rows);

                Directory.CreateDirectory(_pdfOptionsRoute.OutputRoot);

                var fileName = data._headerData?.NumeroDocumento ?? $"{comprobante.NumeroDocumento}";

                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                var pdfPath = Path.Combine(_pdfOptionsRoute.OutputRoot, $"{fileName}.pdf");

                Console.WriteLine($"Generando PDF para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");
                
                await _generarPdfService.GenerarPdf(html, pdfPath);
                
                Console.WriteLine($"PDF generado correctamente para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");

                _logger.LogInformation("PDF generado correctamente en: {Ruta}", pdfPath);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                Console.WriteLine($"Error al procesar comprobante Id: {comprobante.NumeroDocumento}. Error: {ex.Message}");
            }
            finally
            {
                _parallel.Release();
                Console.WriteLine($"Finalizado procesamiento de comprobante: {comprobante.NumeroDocumento}");
            }
        }
    }

    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    while (!stoppingToken.IsCancellationRequested)
    //    {
    //        try
    //        {
    //            var data = await _comprobanteRepository.ObtenerComprobantesAsync();

    //            var html = await File.ReadAllTextAsync("Templates/template.html");

    //            //html = html.Replace("{{Alumno}}", data.header.Alumno);
    //            //html = html.Replace("{{DNI}}", data.header.DNI);
    //            //html = html.Replace("{{Total}}", data.header.Total.ToString());

    //            var rows = "";

    //            foreach (var item in data._detailData)
    //            {
    //                //rows += $"<tr><td>{item.Concepto}</td><td>{item.Monto}</td></tr>";
    //            }

    //            html = html.Replace("{{Rows}}", rows);

    //            var pdfPath = _pdfOptionsRoute.OutputRoot;

    //            await _generarPdfService.GenerarPdf(html, pdfPath);

    //            _logger.LogInformation("PDF generado correctamente en: {Ruta}", pdfPath);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error al generar PDF");
    //            throw;
    //        }
    //    }
    //}
}
