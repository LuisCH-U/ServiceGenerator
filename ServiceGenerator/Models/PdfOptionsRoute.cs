using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Models
{
    public class PdfOptionsRoute
    {
        public string? OutputRoot { get; set; }
        public int MaxParallel { get; set; }
        public int BatchSize { get; set; }
    }
}
