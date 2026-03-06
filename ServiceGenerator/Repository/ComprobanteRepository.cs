using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ServiceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceGenerator.Repository
{
    public class ComprobanteRepository
    {
        private readonly string _sqlConn;
        private readonly ILogger<ComprobanteRepository> _logger;

        public ComprobanteRepository(IOptions<ConnectionStrings> sqlConn, ILogger<ComprobanteRepository> logger)
        {
            _sqlConn = sqlConn.Value.Academico ?? throw new ArgumentNullException(nameof(_sqlConn)); ;
            _logger = logger;
        }

        public async Task<List<ComprobantesModel>> ComprobantesAsync()
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                Console.WriteLine("Conexión a la base de datos establecida exitosamente.");

                Console.WriteLine("Ejecutando el procedimiento almacenado SP_ComprobantesObtener...");
                var res = await conn.QueryAsync<ComprobantesModel>("SP_ComprobantesObtener", commandType: System.Data.CommandType.StoredProcedure);
                
                Console.WriteLine("Comprobantes obtenidos exitosamente: " + res.Count());
                _logger.LogInformation("Comprobantes obtenidos exitosamente: " + res.Count());
                
                return res.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los comprobantes");
                Console.WriteLine("Error al obtener los comprobantes: " + ex.Message);
                throw;
            }
        }
        
        public async Task<DatosComprobanteModel> ObtenerComprobantesDatosAsync(string typeDocument, string numberDocument, string sucursal)
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                Console.WriteLine("Conexión a la base de datos establecida exitosamente.");

                Console.WriteLine("Ejecutando el procedimiento almacenado SP_ObtenerDatosComprobantes...");
                var param = new DynamicParameters();
                param.Add("@TipoDocumento", typeDocument, DbType.String);
                param.Add("@NumeroDocumento", numberDocument, DbType.String);
                param.Add("@Sucursal", sucursal, DbType.String);
                using var res = await conn.QueryMultipleAsync("SP_ObtenerDatosComprobantes", param, commandType: System.Data.CommandType.StoredProcedure);
                
                DatosComprobanteModel classOutComprobantes = new DatosComprobanteModel();
                classOutComprobantes._companyData = await res.ReadFirstOrDefaultAsync<CompanyModel>();
                classOutComprobantes._headerData = await res.ReadFirstOrDefaultAsync<HeaderDocumentModel>();
                classOutComprobantes._detailData = (await res.ReadAsync<DetailDocumentModel>()).ToList();
                
                Console.WriteLine("Se obtuvieron los datos completos");
                _logger.LogInformation("Se obtuvieron lso datos completos");
                
                return classOutComprobantes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener los datos completos: " + ex.Message);
                _logger.LogError(ex, "Error al obtener los comprobantes");
                throw;
            }
        }
    }
}
