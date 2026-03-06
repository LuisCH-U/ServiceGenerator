using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Models
{
    public class DatosComprobanteModel
    {
        public CompanyModel? _companyData { get; set; }
        public HeaderDocumentModel? _headerData { get; set; }
        public List<DetailDocumentModel>? _detailData { get; set; }
    }

    public class CompanyModel
    {
        public string? Razon_Social { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Ruc { get; set; }
        public string? Url_Qr { get; set; }
    }

    public class HeaderDocumentModel
    {
        public string? Sede { get; set; }
        public string? Sededireccion { get; set; }
        public string? SedeTelefono { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? FechaDocumento { get; set; }
        public string? FechaVencimiento { get; set; }
        public string? IGV { get; set; }
        public string? MontoTotal { get; set; }
        public string? Letras { get; set; }
        public string? DocumentoReferencia { get; set; }
        public string? NroDocumentoReferencia { get; set; }
        public string? RazonSocial { get; set; }
        public string? Ruc { get; set; }
        public string? Direccion { get; set; }
        public string? Comentarios { get; set; }
        public string? FEHashCode { get; set; }
        public string? LinkComprobanteElectronico { get; set; }
        public string? QrComprobanteElectronico { get; set; }
        public string? LinkManualUsuarioComprobante { get; set; }
        public string? Moneda { get; set; }
        public string? EsIquitos { get; set; }
        public string? EsNuevaVersion { get; set; }
    }

    public class DetailDocumentModel
    {
        public string? ItemCodigo { get; set; }
        public string? Descripcion { get; set; }
        public decimal CantidadPedida { get; set; }
        public decimal Monto { get; set; }

        public DetailDocumentModel(string ItemCodigo, string Descripcion, decimal CantidadPedida, decimal Monto)
        {
            this.ItemCodigo = ItemCodigo;
            this.Descripcion = Descripcion;
            this.CantidadPedida = CantidadPedida;
            this.Monto = Monto;
        }
    }
}
