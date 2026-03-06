using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Services
{
    public class GenerarPdfService
    {
        private readonly ILogger<GenerarPdfService> _logger;

        public GenerarPdfService(ILogger<GenerarPdfService> logger)
        {
            _logger = logger;
        }

        public async Task GenerarPdf(string html, string path)
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();

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
                //var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                //{
                //    Headless = true
                //});

                //var page = await browser.NewPageAsync();

                //await page.SetContentAsync(html);

                //await page.PdfAsync(new PagePdfOptions
                //{
                //    Path = path,
                //    Format = "A4",
                //    PrintBackground = true
                //});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF");
                throw;
            }
        }
    }
}
