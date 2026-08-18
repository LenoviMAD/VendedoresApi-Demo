using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EntidadesAppVendedores;
using Microsoft.AspNetCore.Mvc;
using VendedoresApi.Models;
using VendedoresApi.SQL.Services;

namespace VendedoresApi.Controllers
{
    // NOTA DEMO: reemplazo simple del Controllers/DashboardEcomController.cs original
    // (excluido del build por depender de librerías ORM legacy propietarias no forkeadas
    // a este demo). Archivo con nombre distinto a propósito para no pisar la exclusión
    // del .csproj — la ruta HTTP la fija el atributo [Route], no el nombre del archivo.
    //
    // Sirve el endpoint unificado que la app pide justo después del login (ver
    // AppVendedores2025-Demo/Services/FuncionesSincronizar.cs línea ~239):
    //   GET DashboardEcom/{vendedorID}/{color}/{empresaID}
    [ApiController]
    [Route("DashboardEcom")]
    public class DashboardEcomController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardEcomController> _logger;

        public DashboardEcomController(IConfiguration configuration, ILogger<DashboardEcomController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("{vendedorID:int}/{color:int}/{empresaID:int}")]
        public ActionResult<DashboardEcomResponse> Get(int vendedorID, int color, int empresaID)
        {
            var response = new DashboardEcomResponse();

            try
            {
                string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    response.Clientes = GetClientes(con, vendedorID, empresaID);
                    response.Categorias = GetCategorias(con, empresaID);
                    response.SubCategorias = GetSubCategorias(con, empresaID);

                    // "Lo que más te gusta" es la sección que VistaProductos.razor muestra
                    // por default al entrar desde un cliente (Clientes.razor navega a
                    // /vistaProductos/100002) — si viene vacía, el catálogo se ve vacío
                    // aunque haya productos sincronizados. Se siembra con los primeros
                    // productos de la empresa para cada cliente del vendedor.
                    response.MasTeGusta = GetMasTeGusta(con, vendedorID, empresaID);
                }

                // Sin datos de venta histórica en la demo: se dejan vacíos tal cual permite
                // el cliente (dashboard?.X ?? new List<...>()). CombosXCliente es para
                // clientes en autogestión — no aplica al flujo de vendedor de esta demo.
                response.RecomendadosTop = new List<LomasvendidoItem>();
                response.NuevosIngresos = new List<int>();
                response.CombosXCliente = new List<CombosXClientesItem>();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardEcom.Get: error para vendedorID={VdID}, empresaID={EmpresaID}", vendedorID, empresaID);
                return StatusCode(500, ex.Message);
            }
        }

        private static List<ClienteItem> GetClientes(SqlConnection con, int vendedorID, int empresaID)
        {
            var ls = new List<ClienteItem>();
            const string sql = @"
                SELECT ClientesID, Codigo, Nombre, Direccion, Telefono, Latitud, Longitud, CUIT, MCE, VendedoresID
                FROM dbo.Clientes
                WHERE EmpresaID = @EmpresaID AND VendedoresID = @VendedoresID AND Baja = 0
                ORDER BY Codigo";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                cmd.Parameters.AddWithValue("@VendedoresID", vendedorID);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ls.Add(new ClienteItem
                        {
                            cli_clientesid = Convert.ToInt32(reader["ClientesID"]),
                            cli_codigo = reader["Codigo"]?.ToString() ?? "",
                            cli_nombre = reader["Nombre"]?.ToString() ?? "",
                            cli_direccion = reader["Direccion"]?.ToString() ?? "",
                            cli_telefono = reader["Telefono"]?.ToString() ?? "",
                            cli_latitud = Convert.ToDouble(reader["Latitud"]),
                            cli_longitud = Convert.ToDouble(reader["Longitud"]),
                            cli_CUIT = reader["CUIT"]?.ToString(),
                            cli_MCE = Convert.ToInt32(reader["MCE"]),
                            VendedoresID = Convert.ToInt32(reader["VendedoresID"])
                        });
                    }
                }
            }
            return ls;
        }

        private static List<LoQueMasTeGustaItem> GetMasTeGusta(SqlConnection con, int vendedorID, int empresaID)
        {
            var ls = new List<LoQueMasTeGustaItem>();

            var clienteIds = new List<int>();
            const string sqlClientes = "SELECT ClientesID FROM dbo.Clientes WHERE EmpresaID = @EmpresaID AND VendedoresID = @VendedoresID AND Baja = 0";
            using (SqlCommand cmd = new SqlCommand(sqlClientes, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                cmd.Parameters.AddWithValue("@VendedoresID", vendedorID);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) clienteIds.Add(Convert.ToInt32(reader["ClientesID"]));
                }
            }

            var productoIds = new List<int>();
            const string sqlProductos = "SELECT TOP 4 ProductosID FROM dbo.Productos WHERE EmpresaID = @EmpresaID AND Baja = 0 ORDER BY ProductosID";
            using (SqlCommand cmd = new SqlCommand(sqlProductos, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) productoIds.Add(Convert.ToInt32(reader["ProductosID"]));
                }
            }

            foreach (var clienteId in clienteIds)
            {
                int orden = 1;
                foreach (var productoId in productoIds)
                {
                    ls.Add(new LoQueMasTeGustaItem { ClienteID = clienteId, ProductoID = productoId, Orden = orden++ });
                }
            }

            return ls;
        }

        private static List<CategoriaItem> GetCategorias(SqlConnection con, int empresaID)
        {
            var ls = new List<CategoriaItem>();
            const string sql = "SELECT CategoriasProductosID, Nombre FROM dbo.CategoriasProductos WHERE EmpresaID = @EmpresaID ORDER BY CategoriasProductosID";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ls.Add(new CategoriaItem
                        {
                            categoriaID = Convert.ToInt32(reader["CategoriasProductosID"]),
                            categoria = reader["Nombre"]?.ToString() ?? ""
                        });
                    }
                }
            }
            return ls;
        }

        private static List<SubCategoriasApiEcomItem> GetSubCategorias(SqlConnection con, int empresaID)
        {
            var ls = new List<SubCategoriasApiEcomItem>();
            const string sql = @"
                SELECT SubCategoriasProductosID, Nombre, CategoriasProductosID
                FROM dbo.SubCategoriasProductos
                WHERE EmpresaID = @EmpresaID
                ORDER BY SubCategoriasProductosID";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpresaID", empresaID);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ls.Add(new SubCategoriasApiEcomItem
                        {
                            subCategoriasEcomItemID = Convert.ToInt32(reader["SubCategoriasProductosID"]),
                            subCategoria = reader["Nombre"]?.ToString() ?? "",
                            imagen = null,
                            categoriasID = new List<int> { Convert.ToInt32(reader["CategoriasProductosID"]) }
                        });
                    }
                }
            }
            return ls;
        }
    }
}
