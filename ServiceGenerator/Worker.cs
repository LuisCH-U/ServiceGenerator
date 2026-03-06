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
            Console.WriteLine("[INICIO] Worker iniciado. Esperando comprobantes...");
            _logger.LogInformation("[INICIO] Worker iniciado. Esperando comprobantes...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var comprobantes = await _comprobanteRepository.ComprobantesAsync();
                    Console.WriteLine($"[INFO] Comprobantes obtenidos: {comprobantes.Count}");
                    _logger.LogInformation("[INFO] Comprobantes obtenidos: {Count}", comprobantes.Count);

                    if (comprobantes == null)
                    {
                        Console.WriteLine("[ERROR] No se pudieron obtener los comprobantes.");
                        _logger.LogInformation("[ERROR] No hay comprobantes pendientes.");
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }

                    Console.WriteLine($"[INFO] Procesando {comprobantes.Count} comprobantes...");
                   _logger.LogInformation("[INFO] Procesando {Count} comprobantes...", comprobantes.Count);
                    var lotes = comprobantes
                        .Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / _pdfOptionsRoute.BatchSize)
                        .Select(g => g.Select(x => x.item).ToList())
                        .ToList();
                    
                    Console.WriteLine($"[INFO] Comprobantes divididos en {lotes.Count} lotes de tamaño máximo {_pdfOptionsRoute.BatchSize}.");
                    _logger.LogInformation("[INFO] Comprobantes divididos en {LotesCount} lotes de tamaño máximo {BatchSize}.", lotes.Count, _pdfOptionsRoute.BatchSize);

                    foreach (var lote in lotes)
                    {
                        Console.WriteLine($"[INFO] Procesando lote con {lote.Count} comprobantes...");
                        _logger.LogInformation("[INFO] Procesando lote con {Count} comprobantes...", lote.Count);
                        var tasks = lote.Select(c => ProcesarComprobanteAsync(c, stoppingToken));
                        await Task.WhenAll(tasks);
                    }

                    Console.WriteLine("[FINALIZADO] Proceso finalizado. Total registros: " + comprobantes.Count);
                    _logger.LogInformation("[FINALIZADO] Proceso finalizado. Total registros: {Total}", comprobantes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR] Error general en el worker: " + ex.Message);
                    _logger.LogError(ex, "[ERROR] Error general en el worker");
                }

                await Task.Delay(2000, stoppingToken);
            }

            Console.WriteLine("[FINALIZADO] Worker detenido.");
            _logger.LogInformation("[FINALIZADO] Worker detenido.");
        }

        private async Task ProcesarComprobanteAsync(ComprobantesModel comprobante, CancellationToken stoppingToken)
        {
            await _parallel.WaitAsync(stoppingToken);
            try
            {
                Console.WriteLine($"[INFO] Procesando comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Procesando comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);

                DatosComprobanteModel data = await _comprobanteRepository.ObtenerComprobantesDatosAsync(comprobante.TipoDocumento ?? "", comprobante.NumeroDocumento ?? "", comprobante.Sucursal ?? "");
                Console.WriteLine($"[INFO] Datos obtenidos para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Datos obtenidos para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);

                Console.WriteLine($"[INFO] Obteniendo el template.html para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Obteniendo el template.html para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "template.html");
                var html = await File.ReadAllTextAsync(templatePath, stoppingToken);

                Console.WriteLine($"[INFO] Reemplazando datos en el template para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Reemplazando datos en el template para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                //html = html.Replace("{{Alumno}}", data._headerDocument?.Alumno ?? "");
                //html = html.Replace("{{DNI}}", data._headerDocument?.DNI ?? "");
                //html = html.Replace("{{Total}}", data._headerDocument?.Total?.ToString() ?? "");

                var rows = "";
                
                Console.WriteLine($"[INFO] Reemplazando filas para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Reemplazando filas para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                //foreach (var item in data._detailData)
                //{
                //    rows += $"<tr><td>{item.Concepto}</td><td>{item.Monto}</td></tr>";
                //}

                Console.WriteLine($"[INFO] Reemplazando filas para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Reemplazando filas para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                html = html.Replace("{{Rows}}", rows);

                Console.WriteLine($"[INFO] Creando directorio de salida si no existe para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Creando directorio de salida si no existe para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                Directory.CreateDirectory(_pdfOptionsRoute.OutputRoot);

                Console.WriteLine($"[INFO] Generando nombre de archivo para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Generando nombre de archivo para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                var fileName = data._headerData?.NumeroDocumento ?? $"{comprobante.NumeroDocumento}";

                Console.WriteLine($"[INFO] Sanitizando nombre de archivo para comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[INFO] Sanitizando nombre de archivo para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                Console.WriteLine($"[INFO] Ruta final del PDF para comprobante: {comprobante.NumeroDocumento} será: {fileName}.pdf");
                _logger.LogInformation("[INFO] Ruta final del PDF para comprobante: {NumeroDocumento} será: {FileName}.pdf", comprobante.NumeroDocumento, fileName);
                var pdfPath = Path.Combine(_pdfOptionsRoute.OutputRoot, $"{fileName}.pdf");

                Console.WriteLine($"[INFO] Generando PDF para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");
                _logger.LogInformation("[INFO] Generando PDF para comprobante: {NumeroDocumento} en ruta: {Ruta}", comprobante.NumeroDocumento, pdfPath);
                
                await _generarPdfService.GenerarPdf(html, pdfPath);

                Console.WriteLine($"[INFO] PDF generado correctamente para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");
                _logger.LogInformation("[INFO] PDF generado correctamente para comprobante: {NumeroDocumento} en ruta: {Ruta}", comprobante.NumeroDocumento, pdfPath);

                Console.WriteLine($"[INFO] PDF generado correctamente en: {pdfPath}");
                _logger.LogInformation("[INFO] PDF generado correctamente en: {Ruta}", pdfPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al procesar comprobante Id: {comprobante.NumeroDocumento}. Error: {ex.Message}");
                _logger.LogError(ex, "[ERROR] Error al procesar comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
            }
            finally
            {
                _parallel.Release();
                Console.WriteLine($"[FINALIZADO] Finalizado procesamiento de comprobante: {comprobante.NumeroDocumento}");
                _logger.LogInformation("[FINALIZADO] Finalizado procesamiento de comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
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
