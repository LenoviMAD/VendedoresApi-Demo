using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EntidadesAppVendedores;
using Microsoft.AspNetCore.Mvc;
using VendedoresApi.SQL.Services;

namespace VendedoresApi.Controllers
{
    // NOTA DEMO: reemplazo simple del Controllers/CombosController.cs original (excluido
    // del build en VendedoresApi.csproj — depende de librerías legacy no forkeadas).
    // Cubre únicamente el endpoint de combos por vendedor que pide el flujo de
    // sincronización post-login (ver AppVendedores2025-Demo/Services/FuncionesSincronizar.cs
    // línea ~396): GET combosPorFechaMultiEmpresa/{vdId}/{fecha}/{empresaID}.
    // Ruta absoluta (sin prefijo de controller) para replicar exactamente la URL que arma
    // el cliente.
    [ApiController]
    [Route("")]
    public class CombosMultiEmpresaController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CombosMultiEmpresaController> _logger;

        public CombosMultiEmpresaController(IConfiguration configuration, ILogger<CombosMultiEmpresaController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("combosPorFechaMultiEmpresa/{vendedorId:int}/{fecha}/{empresaID:int}")]
        public ActionResult<IEnumerable<CombosItemJson>> GetCombosPorFechaMultiEmpresa(int vendedorId, string fecha, int empresaID)
        {
            try
            {
                string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                using SqlConnection con = new SqlConnection(connectionString);
                con.Open();
                return Ok(GetCombos(con, empresaID));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCombosPorFechaMultiEmpresa: error para vendedorId={VdId}, empresaID={EmpresaID}", vendedorId, empresaID);
                return StatusCode(500, ex.Message);
            }
        }

        // Una fila por producto del combo (mismo shape "plano" que el ERP real: el header
        // del combo se repite en cada fila, comb_productosid cambia). El cliente agrupa por
        // comb_combosid al reconstruir CombosHeadItem/CombosProductosItem — ver
        // CombosItemService.Sincronizar en AppVendedores2025-Demo.
        private static List<CombosItemJson> GetCombos(SqlConnection con, int empresaID)
        {
            var ls = new List<CombosItemJson>();
            const string sql = @"
                SELECT c.CombosID, c.Nombre, c.Codigo, c.CantidadMaximaPorCliente, c.CantidadMaximaPorFactura,
                       c.RubroComb, c.Intro, c.ImporteProductosFueraDeCombo,
                       cp.ProductosID, cp.Cantidad, cp.ComboTipo, cp.Descuento1, cp.Descuento2, cp.DpcID, cp.NrGrupoDinamico
                FROM dbo.Combos c
                JOIN dbo.CombosProductos cp ON cp.CombosID = c.CombosID
                WHERE c.EmpresaID = @EmpresaID AND c.Baja = 0
                ORDER BY c.CombosID, cp.Id";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ls.Add(new CombosItemJson
                    {
                        comb_combosid = Convert.ToInt32(reader["CombosID"]),
                        comb_nombre = reader["Nombre"]?.ToString() ?? "",
                        comb_codigo = reader["Codigo"]?.ToString() ?? "",
                        comb_cantmax = Convert.ToInt32(reader["CantidadMaximaPorCliente"]),
                        comb_CantMaxXFact = Convert.ToInt32(reader["CantidadMaximaPorFactura"]),
                        comb_RubroComb = reader["RubroComb"]?.ToString(),
                        comb_Intro = reader["Intro"]?.ToString(),
                        comb_ImporteProdFComb = Convert.ToDecimal(reader["ImporteProductosFueraDeCombo"]),
                        comb_productosid = Convert.ToInt32(reader["ProductosID"]),
                        comb_cantidad = Convert.ToInt32(reader["Cantidad"]),
                        comb_tipo = Convert.ToInt32(reader["ComboTipo"]),
                        comb_descuento1 = reader["Descuento1"]?.ToString(),
                        comb_descuento2 = reader["Descuento2"]?.ToString(),
                        comb_dpcID = Convert.ToInt32(reader["DpcID"]),
                        comb_NrGpDin = Convert.ToInt32(reader["NrGrupoDinamico"]),
                        comb_grp1 = 0,
                        comb_grp2 = 0,
                        comb_grp3 = 0,
                        comb_grp4 = 0,
                        comb_grp5 = 0,
                        comb_grp6 = 0,
                        comb_grp7 = 0,
                        comb_CantDinam = 0,
                        comb_CantSCargo = 0,
                        TotalVtaParaOrden = 0
                    });
                }
            }
            return ls;
        }
    }
}
