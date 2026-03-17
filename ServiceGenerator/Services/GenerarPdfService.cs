using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Services
{
    public class GenerarPdfService : IAsyncDisposable
    {
        private readonly ILogger<GenerarPdfService> _logger;
        
        private IPlaywright? _playwright;
        
        private IBrowser? _browser;

        public GenerarPdfService(ILogger<GenerarPdfService> logger)
        {
            _logger = logger;
        }

        public async Task InicializarAsync()
        {
            if (_playwright != null && _browser != null)
                return;

            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                }
                });
                _logger.LogInformation("Playwright y Chromium inicializados.");
                Console.WriteLine("Playwright y Chromium inicializados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar Playwright. Intentando instalar navegadores...");

                // Intentar instalar los navegadores automáticamente
                await InstalarNavegadoresAsync();

                // Reintentar después de la instalación
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                    }
                });
                _logger.LogInformation("Playwright y Chromium inicializados después de instalación.");
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error al inicializar Playwright: {ex.Message}");
            //    _logger.LogError(ex, "Error al inicializar Playwright");
            //    throw;
            //}
        }

        public async Task GenerarPdf(string html, string path)
        {
            if (_browser == null)
                throw new InvalidOperationException("El navegador no ha sido inicializado.");

            await using var context = await _browser.NewContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                await page.SetContentAsync(html);
                await page.PdfAsync(new PagePdfOptions
                {
                    Path = path,
                    Format = "A4",
                    PrintBackground = true
                });

                _logger.LogInformation("PDF generado en: {RutaPdf}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF");
                throw;
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
                await _browser.CloseAsync();

            _playwright?.Dispose();
        }

        private async Task InstalarNavegadoresAsync()
        {
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var playwrightScript = Path.Combine(baseDirectory, "playwright.ps1");

                _logger.LogInformation("Instalando navegadores de Playwright desde: {Path}", playwrightScript);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{playwrightScript}\" install",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0)
                        {
                            _logger.LogInformation("Navegadores instalados exitosamente.");
                        }
                        else
                        {
                            _logger.LogError("Error al instalar navegadores: {Error}", error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al intentar instalar navegadores automáticamente.");
                throw;
            }
        }

        [Obsolete]
        public async Task GenerarPdfTwo(string html, string path)
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync();
                await using var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();
                try
                {
                    await page.SetContentAsync(html);
                    await page.PdfAsync(new PagePdfOptions
                    {
                        Path = path,
                        Format = "A4",
                        PrintBackground = true
                    });
                    _logger.LogInformation("PDF generado en: {RutaPdf}", path);
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF");
                throw;
            }
        }

        [Obsolete]
        public async Task GenerarPdfOne(string html, string path)
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                //var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                //{
                //    Headless = true
                //});
                await using var browser = await Playwright.CreateAsync().Result.Chromium.LaunchAsync();
                await using var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();
                await page.SetContentAsync(html);
                await page.PdfAsync(new PagePdfOptions
                {
                    Path = path,
                    Format = "A4",
                    PrintBackground = true
                });

                _logger.LogInformation("PDF generado en: {RutaPdf}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF");
                throw;
            }
        }
    }
}
