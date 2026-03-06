using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Models
{
    public class ComprobantesModel
    {
        public int ClienteNumero { get; set; }
        public string? ClienteRUC { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Sucursal { get; set; }
        public string? Nombre { get; set; }
        public string? Moneda { get; set; }
    }

}
