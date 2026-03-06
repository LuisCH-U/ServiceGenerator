using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Models
{
    public class PdfOptionsRoute
    {
        public string OutputRoot { get; set; } = @"D:\PDF_Comprobantes\EVA";
        public int MaxParallel { get; set; } = 4;
        public int BatchSize { get; set; } = 50;
    }
}
