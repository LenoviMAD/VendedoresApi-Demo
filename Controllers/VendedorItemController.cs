using EntidadesAppVendedores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//using RepoDb;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using VendedoresApi.Models;
using EntidadesMultieempresas;
using VendedoresApi.SQL.Services;


namespace VendedoresApi.Controllers
{

    // Este controller se llama ANTES de tener sesión (es el login), así que queda
    // exento del requisito global de JWT (RequireAuthorization en Program.cs) pero
    // sigue exigiendo el header x-api-key (ver middleware en Program.cs).
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class VendedorItemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VendedorItemController> _logger;

        public VendedorItemController(IConfiguration configuration, ILogger<VendedorItemController> logger)
        {
            _configuration = configuration;
            _logger        = logger;
        }

        // NOTA DEMO: en el sistema real este endpoint (sin empresaID) validaba contra el
        // ERP legacy de un solo tenant (tablas EntidadesGnr, excluidas de este fork porque
        // son de un módulo single-tenant que no usa la app multiempresa). AppVendedores2025
        // siempre llama a la variante con {empresaID} más abajo, así que este endpoint queda
        // como stub que informa el caso no soportado en la demo.
        [HttpGet("{vd}/{pwd}")]
        public EntidadesAppVendedores.ValidaUsuario ValidarVD(string vd, string pwd)
        {
            List<int> listadDiponibles = new List<int> { 1 };
            return new EntidadesAppVendedores.ValidaUsuario(listadDiponibles)
            {
                CodError = "1",
                VendedorID = 0,
                Mensaje = "Login sin empresaID no soportado en esta demo. Use /VendedorItem/{vd}/{pwd}/{version}/{empresaID}."
            };
        }


        // NOTA DEMO: stub — el ERP legacy de listas de precio por vendedor (sin empresaID)
        // vivía en EntidadesGnr, excluido de este fork. La app real usa la lista de precios
        // que ya viene en ValidaUsuario.ListadDisponibles (ver ValidarVDyVersion multiempresa).
        [HttpGet("Lista/{vdid}")]
        public List<ListadePrecioItem> GetListadePrecioxVD(int vdid)
        {
            return new List<ListadePrecioItem>();
        }



        [HttpGet("{vd}/{pwd}/{versionCall}")]
        public EntidadesAppVendedores.ValidaUsuario ValidarVDyVersion(string vd, string pwd, string versionCall)
        {
            var version = Int32.Parse(ParametrosAppConfig.GetParametros("ApiSettings", "VersionMinimaApp"));
            var flagObligatoria = ParametrosAppConfig.GetParametros("ApiSettings", "ActualizacionObligatoria");
            var UrlGPSpoint = ParametrosAppConfig.GetParametros("ApiSettings", "UrlGPSpoint");




            List<int> listadDiponibles = new List<int>();
            listadDiponibles.Add(1);
            //listadDiponibles.Add(3);
            //listadDiponibles.Add(5);
            //listadDiponibles.Add(6);

            bool esObligatorioActualizar = flagObligatoria.Equals("true", StringComparison.OrdinalIgnoreCase);

            var resu = new EntidadesAppVendedores.ValidaUsuario(listadDiponibles);
            if (Int32.Parse(versionCall) >= version || !esObligatorioActualizar)
            {


                //resu = oVendedores.ValisarUusuario(vd, pwd);
                //Agregado para validadar cliente 02/08/24
                resu = ValidarVD(vd, pwd);

                resu.UrlHomeVendedor = (ParametrosAppConfig.GetParametros("ApiSettings", "UrlHomeVendedor"));
                resu.OcultarBotonEliminar = (ParametrosAppConfig.GetParametros("ApiSettings", "OcultarBotonEliminar") == "1" ? true : false);


                resu.tiempoEnvioGps = int.TryParse(ParametrosAppConfig.GetParametros("ApiSettings", "tiempoEnvioGps"), out var gpsSegs) && gpsSegs > 0
                    ? gpsSegs
                    : 20;

                resu.UrlGPSpoint = UrlGPSpoint;


                return resu;

            }
            else
            {
                resu.CodError = "2";
                resu.VendedorID = 0;
                resu.TiposDeVendedoresID = 0;
                resu.Mensaje = "App desactualizada";
                return resu;
            }




        }


        [HttpGet("{vd}/{pwd}/{versionCall}/{empresaID:int}")]
        public EntidadesAppVendedores.ValidaUsuario ValidarVDyVersion(string vd, string pwd, string versionCall, int empresaID)
        {
            if (empresaID == 0)
            {
                return ValidarVDyVersion(vd, pwd, versionCall);
            }

            List<int> listasIniciales = new List<int> { 1 };
            EntidadesAppVendedores.ValidaUsuario resu = new EntidadesAppVendedores.ValidaUsuario(listasIniciales);

            // Verificar versión mínima de la app
            string versionMinimaStr = ParametrosAppConfig.GetParametros("ApiSettings", "VersionMinimaApp");
            string flagObligatoria  = ParametrosAppConfig.GetParametros("ApiSettings", "ActualizacionObligatoria");
            bool   esObligatorio    = flagObligatoria.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(versionCall, out int versionCallInt)
                && int.TryParse(versionMinimaStr, out int versionMinima)
                && versionCallInt < versionMinima
                && esObligatorio)
            {
                resu.CodError            = "2";
                resu.VendedorID          = 0;
                resu.TiposDeVendedoresID = 0;
                resu.Mensaje             = "App desactualizada";
                return resu;
            }

            try
            {
                string connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                string clave           = "";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Obtener datos del vendedor
                    using (SqlCommand cmdVd = new SqlCommand("Vendedores_TXValidarAcceso", con))
                    {
                        cmdVd.CommandType = CommandType.StoredProcedure;
                        cmdVd.Parameters.AddWithValue("@Codigo",    vd);
                        cmdVd.Parameters.AddWithValue("@EmpresaID", empresaID);

                        using (SqlDataReader reader = cmdVd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                resu.CodError = "1";
                                resu.Mensaje  = "Usuario o Contraseña Inválido";
                                return resu;
                            }

                            clave                    = reader["Clave"]?.ToString() ?? "";
                            resu.VendedorID          = Convert.ToInt32(reader["VendedoresID"]);
                            resu.NombreDelVendedor    = reader["Nombre"]?.ToString() ?? "";
                            resu.CodigoVendedor       = reader["Codigo"]?.ToString() ?? "";
                            resu.TiposDeVendedoresID  = reader["TiposDeVendedoresID"] != DBNull.Value
                                                       ? Convert.ToInt32(reader["TiposDeVendedoresID"]) : 0;
                            resu.DiaInicioRuta        = reader["DiaInicioRuta"] != DBNull.Value
                                                       ? Convert.ToInt32(reader["DiaInicioRuta"]) : 0;
                            resu.CategoriasComercialesID = reader["CategoriasComercialesID"] != DBNull.Value
                                                       ? Convert.ToInt32(reader["CategoriasComercialesID"]) : (int?)null;
                            resu.VerTodasLasCategorias   = reader["VerTodasLasCategorias"] != DBNull.Value
                                                       && Convert.ToBoolean(reader["VerTodasLasCategorias"]);
                        }
                    }

                    // 2. Verificar clave (la columna Clave guarda un hash BCrypt, no texto plano)
                    bool claveValida;
                    try
                    {
                        claveValida = !string.IsNullOrEmpty(clave) && BCrypt.Net.BCrypt.Verify(pwd, clave);
                    }
                    catch (BCrypt.Net.SaltParseException)
                    {
                        // Defensivo: si la columna tuviera un valor que no es un hash BCrypt
                        // válido (p. ej. dato legado no migrado), se trata como clave inválida
                        // en lugar de tirar una excepción no controlada.
                        claveValida = false;
                    }

                    if (!claveValida)
                    {
                        resu.CodError = "1";
                        resu.Mensaje  = "Usuario o Contraseña Inválido";
                        return resu;
                    }

                    // 3. Listas de precios disponibles
                    List<int> listasDisponibles = new List<int>();
                    using (SqlCommand cmdListas = new SqlCommand("Vendedores_TXListasPrecios", con))
                    {
                        cmdListas.CommandType = CommandType.StoredProcedure;
                        cmdListas.Parameters.AddWithValue("@VendedoresID", resu.VendedorID);
                        cmdListas.Parameters.AddWithValue("@EmpresaID",    empresaID);

                        using (SqlDataReader readerListas = cmdListas.ExecuteReader())
                        {
                            while (readerListas.Read())
                                listasDisponibles.Add(Convert.ToInt32(readerListas["ListasPreciosID"]));
                        }
                    }

                    if (listasDisponibles.Count == 0)
                        listasDisponibles.Add(1);

                    resu.ListadDisponibles = listasDisponibles;

                    // 4. Estado de estrellas del último día disponible
                    List<object> listEstrellas  = new List<object>();
                    int           cantEncendidas = 0;
                    using (SqlCommand cmdEst = new SqlCommand("Vendedores_TXEstrellasDiariasUltimas", con))
                    {
                        cmdEst.CommandType = CommandType.StoredProcedure;
                        cmdEst.Parameters.AddWithValue("@VendedoresID", resu.VendedorID);
                        cmdEst.Parameters.AddWithValue("@EmpresaID",    empresaID);

                        using (SqlDataReader readerEst = cmdEst.ExecuteReader())
                        {
                            while (readerEst.Read())
                            {
                                bool encendida = readerEst["EstaEncendida"] != DBNull.Value
                                                 && Convert.ToBoolean(readerEst["EstaEncendida"]);
                                listEstrellas.Add(encendida);
                                if (encendida) cantEncendidas++;
                            }
                        }
                    }

                    // 4b. Coeficiente de comisión según cantidad de estrellas encendidas
                    decimal coeficienteComision = 0m;
                    using (SqlCommand cmdCoef = new SqlCommand(
                        "SELECT ISNULL(CoeficienteComision, 0) FROM VendedorEstrellasCoeficientes " +
                        "WHERE EmpresaID = @EmpresaID AND CantidadEstrellas = @Cant AND Baja = 0", con))
                    {
                        cmdCoef.Parameters.AddWithValue("@EmpresaID", empresaID);
                        cmdCoef.Parameters.AddWithValue("@Cant",      cantEncendidas);
                        object resultCoef = cmdCoef.ExecuteScalar();
                        if (resultCoef != null && resultCoef != DBNull.Value)
                            coeficienteComision = Convert.ToDecimal(resultCoef);
                    }

                    resu.ListEstrellas               = listEstrellas;
                    resu.CantidadEstrellitasVendedor = cantEncendidas;
                    resu.CoheficienteComision        = coeficienteComision;
                }

                // 5. Campos de configuración
                resu.CodError             = "0";
                resu.Mensaje              = "Acceso autorizado";
                resu.EmpresaID            = empresaID;
                resu.Autogestion          = false;
                resu.EsPago               = false;
                resu.UrlHomeVendedor      = ParametrosAppConfig.GetParametros("ApiSettings", "UrlHomeVendedor");
                resu.UrlGPSpoint          = ParametrosAppConfig.GetParametros("ApiSettings", "UrlGPSpointMultiEmpresa");
                resu.GoolgeApiKey         = ParametrosAppConfig.GetParametros("ApiSettings", "GoolgeApiKey");
                resu.DescargarImagen      = ParametrosAppConfig.GetParametros("ApiSettings", "DescargarImagen") == "1";
                resu.OcultarBotonEliminar = ParametrosAppConfig.GetParametros("ApiSettings", "OcultarBotonEliminar") == "1";
                resu.tiempoEnvioGps       = int.TryParse(ParametrosAppConfig.GetParametros("ApiSettings", "tiempoEnvioGps"),
                                               out int gpsSegs) && gpsSegs > 0 ? gpsSegs : 20;

                // Login exitoso: emitir JWT para las llamadas subsiguientes (protegidas por
                // RequireAuthorization en Program.cs). Va en un header propio, no en el body,
                // porque ValidaUsuario es un DTO compartido con la app y no se le agregan campos.
                string jwt = GenerateJwt(resu.VendedorID, empresaID, resu.TiposDeVendedoresID);
                HttpContext.Response.Headers.Append("X-Auth-Token", jwt);

                return resu;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidarVDyVersion MultiEmpresa: error para vd={Vd}, empresaID={EmpresaID}", vd, empresaID);
                resu.CodError = "1";
                resu.Mensaje  = "Error al validar el acceso";
                return resu;
            }
        }

        /// <summary>
        /// Genera el JWT que la app usa en el header Authorization para el resto de los
        /// endpoints (protegidos globalmente vía RequireAuthorization en Program.cs).
        /// Claims mínimos: vendedorId, empresaId, tipoVendedor.
        /// </summary>
        private string GenerateJwt(int vendedorId, int empresaId, int tipoVendedorId)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var signingKey = jwtSection["SigningKey"] ?? "";
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("vendedorId", vendedorId.ToString()),
                new Claim("empresaId", empresaId.ToString()),
                new Claim("tipoVendedor", tipoVendedorId.ToString())
            };

            double expirationHours = double.TryParse(jwtSection["ExpirationHours"], out var h) ? h : 12;

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // NOTA DEMO: variante legacy single-tenant (sin empresaID), dependía de
        // FuncionesAppVendedoresApi sobre el ERP EntidadesGnr excluido de este fork.
        // AppVendedores2025 siempre pasa empresaID, así que usa GetMensajesAVendedorMultiEmpresa.
        [HttpGet("mensajes/{vdidstring}/{pwd}")]
        public List<MensajeAVendedorItem> GetMensajesAVendedor(string vdidstring, string pwd)
        {
            return new List<MensajeAVendedorItem>();
        }

        /// <summary>
        /// Variante multiempresa de GetMensajesAVendedor.
        /// Lee de MensajesVendedor (tabla propia de cada empresa) en lugar del ERP legacy.
        /// Por ahora devuelve lista vacía — esquema listo para implementación futura.
        /// </summary>
        [HttpGet("mensajes/{vdidstring}/{pwd}/{empresaID:int}")]
        public List<MensajeAVendedorItem> GetMensajesAVendedorMultiEmpresa(string vdidstring, string pwd, int empresaID)
        {
            if (empresaID == 0)
                return GetMensajesAVendedor(vdidstring, pwd);

            List<MensajeAVendedorItem> ls = new List<MensajeAVendedorItem>();
            try
            {
                if (!int.TryParse(vdidstring, out int vendedorID) || vendedorID <= 0)
                    return ls;

                string connectionString = VendedoresApi.SQL.Services.MultiEmpresaDb.GetConnectionString(_configuration);
                if (string.IsNullOrWhiteSpace(connectionString))
                    return ls;

                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    con.Open();
                    using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("MensajesVendedor_TXListarPorVendedor", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmpresaID",    empresaID);
                        cmd.Parameters.AddWithValue("@VendedoresID", vendedorID);
                        using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ls.Add(new MensajeAVendedorItem(
                                    Convert.ToInt32(reader["MensajeID"]),
                                    Convert.ToBoolean(reader["Bloqueado"]),
                                    reader["Subject"]?.ToString() ?? "",
                                    reader["Mensage"]?.ToString()  ?? "",
                                    Convert.ToBoolean(reader["Leido"]),
                                    Convert.ToDateTime(reader["Fecha"]),
                                    Convert.ToInt32(reader["VendedorID"])
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetMensajesAVendedorMultiEmpresa: empresaID={EmpresaID} vendedorID={VendedorID}", empresaID, vdidstring);
            }
            return ls;
        }

        //private static string GetParametros(string seeion, string key)
        //{
        //    IConfigurationRoot builder = new ConfigurationBuilder()
        //    .SetBasePath(Directory.GetCurrentDirectory())
        //    .AddJsonFile("appsettings.json", true, true).Build();



        //    var secParametros = builder.GetSection(seeion);
        //    var dato = secParametros[key];

        //    return dato;

        //}

        public int version;// = 4;
        //public List<MensajeAVendedorItem> GetMensajeaVD(int vdID)
        //{
        //    List<MensajeAVendedorItem> lsMsg = new List<MensajeAVendedorItem>();

        //    //string ConnectionString = BasedeDatosParametros.GetConectionStringPorDefeco;

        //    //using (var connection = new SqlConnection(ConnectionString))
        //    //{
        //    //    lsMsg = connection.Query<MensajeAVendedorItem>(vdID).ToList();

        //    //}




        //    return lsMsg;
        //}

        // ═══════════════════════════════════════════════════════════════
        // CAMBIO DE CLAVE — usado al confirmar eliminación de cuenta
        // ═══════════════════════════════════════════════════════════════

        [HttpPost("cambiarClave")]
        public ActionResult CambiarClave([FromBody] CambiarClaveVendedorRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("El body no puede ser nulo.");

                if (request.VendedoresID <= 0)
                    return BadRequest("VendedoresID debe ser mayor que cero.");

                if (string.IsNullOrWhiteSpace(request.NuevaClave))
                    return BadRequest("La nueva clave no puede estar vacía.");

                // Hashear ANTES de mandar a la base — la columna Clave guarda un hash BCrypt,
                // nunca la contraseña en texto plano (mismo criterio que la verificación en
                // ValidarVDyVersion más arriba).
                string claveHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaClave.Trim());

                string connectionString;
                if (request.EmpresaID > 0)
                {
                    connectionString = MultiEmpresaDb.GetConnectionString(_configuration);
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        _logger.LogError("CambiarClave: cadena de conexión MultiEmpresa no configurada.");
                        return StatusCode(500, "Error de configuración del servidor.");
                    }
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand("Vendedores_MActualizarClave", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@VendedoresID", request.VendedoresID);
                            cmd.Parameters.AddWithValue("@EmpresaID",    request.EmpresaID);
                            cmd.Parameters.AddWithValue("@Clave",        claveHash);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    var defaultDb = _configuration["defaultDatabase"] ?? "ConnectionEPL26";
                    connectionString = _configuration.GetConnectionString(defaultDb);
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        _logger.LogError("CambiarClave: cadena de conexión no encontrada para clave '{DefaultDb}'.", defaultDb);
                        return StatusCode(500, "Error de configuración del servidor.");
                    }
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand("Vendedores_MClave", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@VendedoresID", request.VendedoresID);
                            cmd.Parameters.AddWithValue("@Clave", claveHash);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                _logger.LogInformation("CambiarClave: clave actualizada para VendedoresID={ID}.", request.VendedoresID);
                return Ok("Clave actualizada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CambiarClave: error al actualizar clave para VendedoresID={ID}.", request?.VendedoresID);
                return StatusCode(500, "Error al actualizar la clave. Intente nuevamente.");
            }
        }
    }



}
