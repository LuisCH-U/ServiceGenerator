using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using ServiceGenerator.Models;
using ServiceGenerator.Repository;
using ServiceGenerator.Services;
using System.Diagnostics;
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
        private readonly RazorService _razorService;
        private int contadorlote = 0;

        public Worker(ILogger<Worker> logger, IOptions<PdfOptionsRoute> pdfOptionsRoute, ComprobanteRepository comprobanteRepository, GenerarPdfService generarPdfService, RazorService razorService)
        {
            _logger = logger;
            _pdfOptionsRoute = pdfOptionsRoute.Value;
            _parallel = new SemaphoreSlim(_pdfOptionsRoute.MaxParallel);
            _comprobanteRepository = comprobanteRepository;
            _generarPdfService = generarPdfService;
            _razorService = razorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[INICIO] Worker iniciado. Esperando comprobantes..." + " -> " + DateTime.Now);
            _logger.LogInformation("[INICIO] Worker iniciado. Esperando comprobantes..." + " -> " + DateTime.Now);

            // Inicializar el servicio de generación de PDF antes de entrar al ciclo principal para evitar inicializaciones repetidas
            await _generarPdfService.InicializarAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[INFO] Obteniendo comprobantes pendientes...");
                    _logger.LogInformation("[INFO] Obteniendo comprobantes pendientes...");
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

                        var Ok = lote.Where(c => c.ProcesoOK).ToList();
                        var Error = lote.Where(c => !c.ProcesoOK).ToList();

                        Console.WriteLine($"[INFO] {Ok.Count} comprobantes exitosos, {Error.Count} con error.");
                        _logger.LogInformation("[INFO] {Exitosos} exitosos, {Errores} con error.", Ok.Count, Error.Count);

                        if (Ok.Count > 0)
                        {
                            Console.WriteLine($"[INFO] Actualizando {Ok.Count} comprobantes del lote...");
                            _logger.LogInformation("[INFO] Actualizando {Count} comprobantes del lote...", Ok.Count);
                            
                            var actualizados = await _comprobanteRepository.ActualizaComprobantesLoteAsync(lote);
                            
                            Console.WriteLine($"[INFO] {actualizados} comprobantes actualizados correctamente.");
                            _logger.LogInformation("[INFO] {Count} comprobantes actualizados correctamente.", actualizados);
                        }

                        contadorlote += Ok.Count;
                    }

                    Console.WriteLine("[FINALIZADO] Proceso finalizado. Total registros: " + contadorlote + " -> " + DateTime.Now); 
                    _logger.LogInformation("[FINALIZADO] Proceso finalizado. Total registros: {Total} -> {Fecha}", contadorlote, DateTime.Now);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[INFO] Worker cancelado.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR] Error general en el worker: " + ex.Message + " -> " + DateTime.Now);
                    _logger.LogError(ex, "[ERROR] Error general en el worker" + " -> " + DateTime.Now);
                }

                try
                {
                    Console.WriteLine("[INFO] Esperando 1 Hora antes de la siguiente consulta..." + " -> " + DateTime.Now);
                    _logger.LogInformation("[INFO] Esperando 1 Hora antes de la siguiente consulta... -> {Fecha}", DateTime.Now);
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
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

                //Console.WriteLine($"[INFO] Datos obtenidos para comprobante: {comprobante.NumeroDocumento}");
                //_logger.LogInformation("[INFO] Datos obtenidos para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);

                //Console.WriteLine($"[INFO] Obteniendo el template.html para comprobante: {comprobante.NumeroDocumento}");
                //_logger.LogInformation("[INFO] Obteniendo el template.html para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                //var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "template.cshtml");
                //var html = await File.ReadAllTextAsync(templatePath, stoppingToken);
                
                switch (comprobante.TipoDocumento) {
                    case "BV":
                        data.headerData.TipoDocumento = "BOLETA DE VENTA";
                        break;
                    case "FC":
                        data.headerData.TipoDocumento = "FACTURA DE VENTA";
                        break; 
                    case "NC":
                        data.headerData.TipoDocumento = "NOTA DE CRÉDITO";
                        break;
                    case "ND":
                        data.headerData.TipoDocumento = "NOTA DE DÉDITO";
                        break;
                }
                var html = await _razorService.RenderAsync("Templates/template.cshtml", data);

                //Console.WriteLine($"[INFO] Creando directorio de salida si no existe para comprobante: {comprobante.NumeroDocumento}");
                //_logger.LogInformation("[INFO] Creando directorio de salida si no existe para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                Directory.CreateDirectory(_pdfOptionsRoute.OutputRoot);

                //Console.WriteLine($"[INFO] Generando nombre de archivo para comprobante: {comprobante.NumeroDocumento}");
                //_logger.LogInformation("[INFO] Generando nombre de archivo para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                var fileName = data?.headerData?.NumeroDocumento ?? $"{comprobante.NumeroDocumento}";

                //Console.WriteLine($"[INFO] Sanitizando nombre de archivo para comprobante: {comprobante.NumeroDocumento}");
                //_logger.LogInformation("[INFO] Sanitizando nombre de archivo para comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                //Console.WriteLine($"[INFO] Ruta final del PDF para comprobante: {comprobante.NumeroDocumento} será: {fileName}.pdf");
                //_logger.LogInformation("[INFO] Ruta final del PDF para comprobante: {NumeroDocumento} será: {FileName}.pdf", comprobante.NumeroDocumento, fileName);
                var pdfPath = Path.Combine(_pdfOptionsRoute.OutputRoot, $"{fileName.Trim()}.pdf");

                //Console.WriteLine($"[INFO] Generando PDF para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");
                //_logger.LogInformation("[INFO] Generando PDF para comprobante: {NumeroDocumento} en ruta: {Ruta}", comprobante.NumeroDocumento, pdfPath);
                
                await _generarPdfService.GenerarPdf(html, pdfPath);

                comprobante.ProcesoOK = true;

                //Console.WriteLine($"[INFO] PDF generado correctamente para comprobante: {comprobante.NumeroDocumento} en ruta: {pdfPath}");
                //_logger.LogInformation("[INFO] PDF generado correctamente para comprobante: {NumeroDocumento} en ruta: {Ruta}", comprobante.NumeroDocumento, pdfPath);

                Console.WriteLine($"[INFO] PDF generado correctamente en: {pdfPath}" + " -> " + DateTime.Now);
                _logger.LogInformation("[INFO] PDF generado correctamente en: {Ruta}", pdfPath + " -> " + DateTime.Now);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al procesar comprobante: {comprobante.NumeroDocumento}. Error: {ex.Message}");
                _logger.LogError(ex, "[ERROR] Error al procesar comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                comprobante.ProcesoOK = false;
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
