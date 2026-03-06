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
        public async Task GenerarPdf(string html, string path)
        {
            using var playwright = await Playwright.CreateAsync();

            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();

            await page.SetContentAsync(html);

            await page.PdfAsync(new PagePdfOptions
            {
                Path = path,
                Format = "A4",
                PrintBackground = true
            });
        }
    }
}
