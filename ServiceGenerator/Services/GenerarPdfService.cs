using Microsoft.Playwright;
using System;
using System.Collections.Generic;
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

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--no-sandbox"
                }
            });
            _logger.LogInformation("Playwright y Chromium inicializados.");
            Console.WriteLine("Playwright y Chromium inicializados.");
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
