using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EntidadesAppVendedores;
using Microsoft.AspNetCore.Mvc;
using VendedoresApi.Models;
using VendedoresApi.SQL.Services;

namespace VendedoresApi.Controllers
{
    // NOTA DEMO: reemplazo simple del Controllers/ProductosItemController.cs original
    // (excluido del build — depende de librerías ORM legacy propietarias enormes no
    // forkeadas a este demo). Archivo con nombre distinto a propósito para no pisar la
    // exclusión del .csproj — la ruta HTTP la fija [Route("ProductosItem")], no el nombre
    // del archivo. Solo cubre los 4
    // sub-endpoints que pide el flujo de sincronización post-login (ver
    // AppVendedores2025-Demo/Services/FuncionesSincronizar.cs líneas ~339-425).
    [ApiController]
    [Route("ProductosItem")]
    public class ProductosItemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductosItemController> _logger;

        public ProductosItemController(IConfiguration configuration, ILogger<ProductosItemController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Catálogo completo. La app llama con vdLogueado/0/version/-1/empresaID — los
        // segmentos intermedios (paginación/versión incremental) no aplican a esta demo:
        // siempre se devuelve el catálogo entero vigente para la empresa.
        [HttpGet("productosPorFecha/{vdLogueado}/{p1}/{version}/{p2}/{empresaID:int}")]
        public ActionResult<List<ProductoListaItem>> GetProductosPorFecha(string vdLogueado, string p1, string version, string p2, int empresaID)
        {
            try
            {
                string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                using SqlConnection con = new SqlConnection(connectionString);
                con.Open();
                return Ok(GetProductos(con, empresaID));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProductosPorFecha: error para empresaID={EmpresaID}", empresaID);
                return StatusCode(500, ex.Message);
            }
        }

        // Delta de stock/precios. La demo no maneja sincronización incremental real: si se
        // pide con fechaUltimaSincro distinta de "-1" devuelve vacío (el cliente solo
        // aplica el bloque si Count > 0), y con "-1" (primera sincronización) devuelve el
        // stock/precios actuales completos.
        [HttpGet("StockYPreciosProductos/{fechaUltimaSincro}/{empresaID:int}")]
        public ActionResult<StockYPreciosResponse> GetStockYPrecios(string fechaUltimaSincro, int empresaID)
        {
            var response = new StockYPreciosResponse();
            try
            {
                if (fechaUltimaSincro == "-1")
                {
                    string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                    using SqlConnection con = new SqlConnection(connectionString);
                    con.Open();
                    (response.Stock, response.Precios) = GetStockYPreciosActuales(con, empresaID);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStockYPrecios: error para empresaID={EmpresaID}", empresaID);
                return StatusCode(500, ex.Message);
            }
        }

        // Flags de UI (nuevo ingreso, precio modificado, etc.) — no modelados en esta demo,
        // se devuelve siempre vacío (el cliente lo tolera: solo aplica si Count > 0).
        [HttpGet("ProductosActualItems/{fechaUltimaSincro}/{empresaID:int}")]
        public ActionResult<List<ProductosActualizarItems>> GetProductosActualItems(string fechaUltimaSincro, int empresaID)
        {
            return Ok(new List<ProductosActualizarItems>());
        }

        // Bloqueados por cliente (vacío en la demo) + mapeo producto→subcategoría.
        [HttpGet("bloqueadosYSubcategorias/{vdId:int}/{empresaID:int}")]
        public ActionResult<BloqueadosYSubcategoriasResponse> GetBloqueadosYSubcategorias(int vdId, int empresaID)
        {
            var response = new BloqueadosYSubcategoriasResponse
            {
                Bloqueados = new List<ProductosBloqueadosxCliente>()
            };
            try
            {
                string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                using SqlConnection con = new SqlConnection(connectionString);
                con.Open();
                response.SubCategorias = GetProductosSubCategorias(con, empresaID);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBloqueadosYSubcategorias: error para vdId={VdId}, empresaID={EmpresaID}", vdId, empresaID);
                return StatusCode(500, ex.Message);
            }
        }

        private static List<ProductoListaItem> GetProductos(SqlConnection con, int empresaID)
        {
            var ls = new List<ProductoListaItem>();
            const string sql = @"
                SELECT ProductosID, CodigoDeProducto, Nombre, Marca, Color, StockEnUnidades, UnidadesPorBulto,
                       PrecioUnitarioFinalEspecial, PrecioUnitarioFinalAutoservicio, PrecioUnitarioFinalAlamcene,
                       PrecioUnitarioFinalKiosko, PrecioUnitarioSalon, PrecioUnitarioEcom,
                       PrecioUnitarioFinalEspecialNeto, PrecioUnitarioFinalAutoservicioNeto, PrecioUnitarioFinalAlamceneNeto,
                       PrecioUnitarioFinalKioskoNeto, PrecioUnitarioSalonNeto, PrecioUnitarioEcomNeto,
                       DesactivadoParaLaVenta
                FROM dbo.Productos
                WHERE EmpresaID = @EmpresaID AND Baja = 0
                ORDER BY ProductosID";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ls.Add(new ProductoListaItem
                    {
                        ProductosID = Convert.ToInt32(reader["ProductosID"]),
                        CodigoDeProducto = reader["CodigoDeProducto"]?.ToString() ?? "",
                        Nombre = reader["Nombre"]?.ToString() ?? "",
                        Marca = reader["Marca"]?.ToString(),
                        Color = Convert.ToInt32(reader["Color"]),
                        StockEnUnidades = Convert.ToInt32(reader["StockEnUnidades"]),
                        UnidadesPorBulto = Convert.ToInt32(reader["UnidadesPorBulto"]),
                        PrecioUnitarioFinalEspecial = Convert.ToDecimal(reader["PrecioUnitarioFinalEspecial"]),
                        PrecioUnitarioFinalAutoservicio = Convert.ToDecimal(reader["PrecioUnitarioFinalAutoservicio"]),
                        PrecioUnitarioFinalAlamcene = Convert.ToDecimal(reader["PrecioUnitarioFinalAlamcene"]),
                        PrecioUnitarioFinalKiosko = Convert.ToDecimal(reader["PrecioUnitarioFinalKiosko"]),
                        PrecioUnitarioSalon = Convert.ToDecimal(reader["PrecioUnitarioSalon"]),
                        PrecioUnitarioEcom = Convert.ToDecimal(reader["PrecioUnitarioEcom"]),
                        PrecioUnitarioFinalEspecialNeto = Convert.ToDecimal(reader["PrecioUnitarioFinalEspecialNeto"]),
                        PrecioUnitarioFinalAutoservicioNeto = Convert.ToDecimal(reader["PrecioUnitarioFinalAutoservicioNeto"]),
                        PrecioUnitarioFinalAlamceneNeto = Convert.ToDecimal(reader["PrecioUnitarioFinalAlamceneNeto"]),
                        PrecioUnitarioFinalKioskoNeto = Convert.ToDecimal(reader["PrecioUnitarioFinalKioskoNeto"]),
                        PrecioUnitarioSalonNeto = Convert.ToDecimal(reader["PrecioUnitarioSalonNeto"]),
                        PrecioUnitarioEcomNeto = Convert.ToDecimal(reader["PrecioUnitarioEcomNeto"]),
                        DesactivadoParaLaVenta = Convert.ToBoolean(reader["DesactivadoParaLaVenta"])
                    });
                }
            }
            return ls;
        }

        private static (List<ProductoStockItem> stock, List<ProductoPrecioItems> precios) GetStockYPreciosActuales(SqlConnection con, int empresaID)
        {
            var stock = new List<ProductoStockItem>();
            var precios = new List<ProductoPrecioItems>();
            const string sql = @"
                SELECT ProductosID, StockEnUnidades,
                       PrecioUnitarioFinalAutoservicio, PrecioUnitarioFinalEspecial, PrecioUnitarioFinalAlamcene,
                       PrecioUnitarioFinalKiosko, PrecioUnitarioSalon, PrecioUnitarioEcom,
                       PrecioUnitarioFinalAutoservicioNeto, PrecioUnitarioFinalEspecialNeto, PrecioUnitarioFinalAlamceneNeto,
                       PrecioUnitarioFinalKioskoNeto, PrecioUnitarioSalonNeto, PrecioUnitarioEcomNeto
                FROM dbo.Productos
                WHERE EmpresaID = @EmpresaID AND Baja = 0
                ORDER BY ProductosID";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int productosID = Convert.ToInt32(reader["ProductosID"]);

                    stock.Add(new ProductoStockItem
                    {
                        id = productosID,
                        stk = Convert.ToDecimal(reader["StockEnUnidades"])
                    });

                    precios.Add(new ProductoPrecioItems
                    {
                        ProdID = productosID,
                        PFAut = Convert.ToDecimal(reader["PrecioUnitarioFinalAutoservicio"]),
                        PFEsp = Convert.ToDecimal(reader["PrecioUnitarioFinalEspecial"]),
                        PFAl = Convert.ToDecimal(reader["PrecioUnitarioFinalAlamcene"]),
                        PFFKi = Convert.ToDecimal(reader["PrecioUnitarioFinalKiosko"]),
                        PFSal = Convert.ToDecimal(reader["PrecioUnitarioSalon"]),
                        PFEcom = Convert.ToDecimal(reader["PrecioUnitarioEcom"]),
                        PNAut = Convert.ToDecimal(reader["PrecioUnitarioFinalAutoservicioNeto"]),
                        PNEsp = Convert.ToDecimal(reader["PrecioUnitarioFinalEspecialNeto"]),
                        PNAlm = Convert.ToDecimal(reader["PrecioUnitarioFinalAlamceneNeto"]),
                        PNKio = Convert.ToDecimal(reader["PrecioUnitarioFinalKioskoNeto"]),
                        PNSal = Convert.ToDecimal(reader["PrecioUnitarioSalonNeto"]),
                        PNEcom = Convert.ToDecimal(reader["PrecioUnitarioEcomNeto"])
                    });
                }
            }
            return (stock, precios);
        }

        private static List<ProductosSubCategoriasItem> GetProductosSubCategorias(SqlConnection con, int empresaID)
        {
            var ls = new List<ProductosSubCategoriasItem>();
            const string sql = @"
                SELECT ProductosID, SubCategoriasProductosID
                FROM dbo.Productos
                WHERE EmpresaID = @EmpresaID AND Baja = 0";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ls.Add(new ProductosSubCategoriasItem
                    {
                        ProductosID = Convert.ToInt32(reader["ProductosID"]),
                        SubCategiruasID = Convert.ToInt32(reader["SubCategoriasProductosID"])
                    });
                }
            }
            return ls;
        }
    }
}
