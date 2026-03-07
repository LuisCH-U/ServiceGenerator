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
                Console.WriteLine("[INFO] Conexión a la base de datos establecida exitosamente.");
                _logger.LogInformation("[INFO] Conexión a la base de datos establecida exitosamente.");

                Console.WriteLine("[INFO] Ejecutando el procedimiento almacenado SP_ComprobantesObtener...");
                _logger.LogInformation("[INFO] Ejecutando el procedimiento almacenado SP_ComprobantesObtener...");
                var res = await conn.QueryAsync<ComprobantesModel>("SP_ComprobantesObtener", commandType: System.Data.CommandType.StoredProcedure);
                
                //Console.WriteLine("[INFO] Comprobantes obtenidos exitosamente: " + res.Count());
                //_logger.LogInformation("[INFO] Comprobantes obtenidos exitosamente: " + res.Count());
                
                return res.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Error al obtener los comprobantes: " + ex.Message);
                _logger.LogError(ex, "[ERROR] Error al obtener los comprobantes");
                throw;
            }
        }
        
        public async Task<DatosComprobanteModel> ObtenerComprobantesDatosAsync(string typeDocument, string numberDocument, string sucursal)
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                //Console.WriteLine("[INFO] Conexión a la base de datos establecida exitosamente.");
                //_logger.LogInformation("[INFO] Conexión a la base de datos establecida exitosamente.");

                //Console.WriteLine("[INFO] Ejecutando el procedimiento almacenado SP_ObtenerDatosComprobantes...");
                //_logger.LogInformation("[INFO] Ejecutando el procedimiento almacenado SP_ObtenerDatosComprobantes...");
                var param = new DynamicParameters();
                param.Add("@TipoDocumento", typeDocument, DbType.String);
                param.Add("@NumeroDocumento", numberDocument, DbType.String);
                param.Add("@Sucursal", sucursal, DbType.String);
                using var res = await conn.QueryMultipleAsync("SP_ObtenerDatosComprobantes", param, commandType: System.Data.CommandType.StoredProcedure);
                
                DatosComprobanteModel classOutComprobantes = new DatosComprobanteModel();
                classOutComprobantes.companyData = await res.ReadFirstOrDefaultAsync<CompanyModel>();
                classOutComprobantes.headerData = await res.ReadFirstOrDefaultAsync<HeaderDocumentModel>();
                classOutComprobantes.detailData = (await res.ReadAsync<DetailDocumentModel>()).ToList();
                
                //Console.WriteLine("[INFO] Se obtuvieron los datos completos");
                //_logger.LogInformation("[INFO] Se obtuvieron lso datos completos");
                
                return classOutComprobantes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Error al obtener los datos completos: " + ex.Message);
                _logger.LogError(ex, "[ERROR] Error al obtener los comprobantes");
                throw;
            }
        }

        public async Task<int> ActualizaComprobantesLoteAsync(List<ComprobantesModel> comprobantes)
        {
            int actualizadosTotales = 0;

            foreach (var comprobante in comprobantes)
            {
                try
                {
                    var resultado = await ActualizaComprobantesGeneradosAsync(comprobante.TipoDocumento ?? "", comprobante.NumeroDocumento ?? "", comprobante.Sucursal ?? "");
                    actualizadosTotales += resultado;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ERROR] Error actualizando comprobante: {NumeroDocumento}", comprobante.NumeroDocumento);
                }
            }

            return actualizadosTotales;
        }

        public async Task<int> ActualizaComprobantesGeneradosAsync(string typeDocument, string numberDocument, string sucursal) 
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                //Console.WriteLine("[INFO] Conexión a la base de datos establecida exitosamente.");
                //_logger.LogInformation("[INFO] Conexión a la base de datos establecida exitosamente.");
                //Console.WriteLine("[INFO] Ejecutando el procedimiento almacenado SP_ActualizarComprobantesGenerados...");
                //_logger.LogInformation("[INFO] Ejecutando el procedimiento almacenado SP_ActualizarComprobantesGenerados...");
                var param = new DynamicParameters();
                param.Add("@TipoDocumento", typeDocument, DbType.String);
                param.Add("@NumeroDocumento", numberDocument, DbType.String);
                param.Add("@Sucursal", sucursal, DbType.String);
                param.Add("@RetVal", dbType: DbType.Int32, direction: ParameterDirection.Output);
                var res = await conn.ExecuteAsync("SP_ActualizarComprobantesGenerados", param, commandType: System.Data.CommandType.StoredProcedure);
                res = param.Get<int>("@RetVal");
                //Console.WriteLine("[INFO] Se actualizaron los comprobantes generados correctamente");
                //_logger.LogInformation("[INFO] Se actualizaron los comprobantes generados correctamente");

                return res;

            }
            catch (Exception)
            {
                Console.WriteLine("[ERROR] Error al actualizar los comprobantes generados {numberDocument}: ", numberDocument);
                _logger.LogError("[ERROR] Error al actualizar los comprobantes generados: " + numberDocument);
                throw;
            }
        }
    }
}
