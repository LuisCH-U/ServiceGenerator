using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Models
{
    public class DatosComprobanteModel
    {
        public CompanyModel? companyData { get; set; }
        public HeaderDocumentModel? headerData { get; set; }
        public List<DetailDocumentModel>? detailData { get; set; }
    }

    public class CompanyModel
    {
        public string? Razon_Social { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Ruc { get; set; }
        public string? Url_Qr { get; set; }
        public string? Marca { get; set; }
    }

    public class HeaderDocumentModel
    {
        public string? Sede { get; set; }
        public string? Sededireccion { get; set; }
        public string? SedeTelefono { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? FechaDocumento { get; set; }
        public string? FechaVencimiento { get; set; }
        public decimal IGV { get; set; }
        public decimal MontoTotal { get; set; }
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
        public string? TipoDocumento { get; set; }
        public string IGVFormateado => IGV.ToString("F2");
        public string MontoTotalFormateado => MontoTotal.ToString("F2");
    }

    public class DetailDocumentModel
    {
        public string? ItemCodigo { get; set; }
        public string? Descripcion { get; set; }
        public int CantidadPedida { get; set; }
        public decimal Monto { get; set; }
        public string MontoFormateado => Monto.ToString("F0");

        public DetailDocumentModel(string ItemCodigo, string Descripcion, int CantidadPedida, decimal Monto)
        {
            this.ItemCodigo = ItemCodigo;
            this.Descripcion = Descripcion;
            this.CantidadPedida = CantidadPedida;
            this.Monto = Monto;
        }
    }
}
