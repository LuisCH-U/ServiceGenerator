using RazorLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Services
{
    public class RazorService
    {
        private readonly ILogger<RazorService> _logger;

        private readonly RazorLightEngine _razorLightEngine;

        public RazorService(ILogger<RazorService> logger)
        {
            _logger = logger;
            _razorLightEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<string> RenderAsync<T>(string templatePath, T model)
        {
            try
            {
                return await _razorLightEngine.CompileRenderAsync(templatePath, model);
            }
            catch (Exception)
            {
                _logger.LogError("Error renderizando template Razor: {TemplatePath}", templatePath);
                throw;
            }
        }
    }
}