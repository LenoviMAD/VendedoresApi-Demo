using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Microsoft.Extensions.Configuration;
using VendedoresApi.Models;

namespace VendedoresApi.SQL.Services
{
    /// <summary>
    /// Servicio de acceso a datos para la tabla AltaClientePendiente.
    /// Todos los SP usan el prefijo AltaClientePendiente_ (sin sp_).
    /// Reutiliza el patrón de conexión SQL del resto del proyecto
    /// (System.Data.SqlClient + cadena de conexión desde appsettings.json).
    /// </summary>
    public class AltaClientePendienteService
    {
        private readonly string _connectionString;

        public AltaClientePendienteService(IConfiguration configuration)
        {
            // Usa la misma clave defaultDatabase que el resto del proyecto
            var defaultDb = configuration["defaultDatabase"] ?? "ConnectionEPL26";
            _connectionString = configuration.GetConnectionString(defaultDb)
                ?? throw new InvalidOperationException($"No se encontró la cadena de conexión '{defaultDb}'.");
        }

        // ──────────────────────────────────────────────────────────────
        // INSERT
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Inserta una solicitud temporal. Retorna el ID generado.
        /// SP: AltaClientePendiente_Insertar
        /// </summary>
        public long InsertarSolicitud(AltaClientePendienteItem item)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand("AltaClientePendiente_Insertar", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@TipoUsuario",         item.TipoUsuario);
                    cmd.Parameters.AddWithValue("@LabelDocumento",      item.LabelDocumento);
                    cmd.Parameters.AddWithValue("@Documento",           item.Documento);
                    cmd.Parameters.AddWithValue("@Email",               item.Email);
                    cmd.Parameters.AddWithValue("@LabelNombre",         item.LabelNombre);
                    cmd.Parameters.AddWithValue("@Nombre",              item.Nombre);
                    cmd.Parameters.AddWithValue("@Telefono",            item.Telefono);
                    cmd.Parameters.AddWithValue("@TelefonoNormalizado", item.TelefonoNormalizado);
                    cmd.Parameters.AddWithValue("@Provincia",           item.Provincia);
                    cmd.Parameters.AddWithValue("@Localidad",           item.Localidad);
                    cmd.Parameters.AddWithValue("@LocalidadID",         item.LocalidadID);
                    cmd.Parameters.AddWithValue("@DireccionEnvio",      item.DireccionEnvio);
                    cmd.Parameters.AddWithValue("@CodigoPostal",        item.CodigoPostal);
                    cmd.Parameters.AddWithValue("@Altura",              item.Altura ?? "");
                    cmd.Parameters.AddWithValue("@Latitud",             item.Latitud);
                    cmd.Parameters.AddWithValue("@Longitud",            item.Longitud);
                    cmd.Parameters.AddWithValue("@Password",            item.Password);
                    cmd.Parameters.AddWithValue("@PayloadJson",
                        (object)item.PayloadJson ?? DBNull.Value);

                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt64(result) : 0;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        // QUERIES
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Busca la solicitud pendiente más reciente para un teléfono normalizado.
        /// Devuelve null si no existe.
        /// SP: AltaClientePendiente_TraerPendiente
        /// </summary>
        public AltaClientePendienteItem GetPorTelefono(string telefonoNormalizado)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand("AltaClientePendiente_TraerPendiente", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TelefonoNormalizado", telefonoNormalizado);

                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                            return MapearFila(dr);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Busca una solicitud por el MessageID del primer WA enviado.
        /// Devuelve null si no existe.
        /// SP: AltaClientePendiente_TraerPorMsgId
        /// </summary>
        public AltaClientePendienteItem GetPorMsgId(string waMsgIdConfirmacion)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand("AltaClientePendiente_TraerPorMsgId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@WaMsgIdConfirmacion", waMsgIdConfirmacion);

                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                            return MapearFila(dr);
                    }
                }
            }
            return null;
        }

        // ──────────────────────────────────────────────────────────────
        // UPDATES
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Guarda el WaMsgId del mensaje de confirmación enviado.
        /// SP: AltaClientePendiente_GuardarMsgIdConfirmacion
        /// </summary>
        public void GuardarMsgIdConfirmacion(long id, string msgId)
        {
            EjecutarSp("AltaClientePendiente_GuardarMsgIdConfirmacion", cmd =>
            {
                cmd.Parameters.AddWithValue("@AltaClientePendienteID", id);
                cmd.Parameters.AddWithValue("@WaMsgIdConfirmacion",     msgId);
            });
        }

        /// <summary>
        /// Marca la solicitud como Confirmada (cliente respondió Sí).
        /// Retorna true si se actualizó (no estaba ya procesada).
        /// SP: AltaClientePendiente_MarcarConfirmada
        /// </summary>
        public bool MarcarConfirmado(long id)
        {
            return EjecutarSpConRowCount("AltaClientePendiente_MarcarConfirmada", cmd =>
            {
                cmd.Parameters.AddWithValue("@AltaClientePendienteID", id);
            }) > 0;
        }

        /// <summary>
        /// Marca la solicitud como Rechazada (cliente respondió No).
        /// Retorna true si se actualizó.
        /// SP: AltaClientePendiente_MarcarRechazada
        /// </summary>
        public bool MarcarRechazado(long id)
        {
            return EjecutarSpConRowCount("AltaClientePendiente_MarcarRechazada", cmd =>
            {
                cmd.Parameters.AddWithValue("@AltaClientePendienteID", id);
            }) > 0;
        }

        /// <summary>
        /// Marca la solicitud como Procesada/CredencialesEnviadas.
        /// SP: AltaClientePendiente_MarcarProcesada
        /// </summary>
        public void MarcarProcesado(long id, int clientesID, string usuario, string msgIdCredenciales)
        {
            EjecutarSp("AltaClientePendiente_MarcarProcesada", cmd =>
            {
                cmd.Parameters.AddWithValue("@AltaClientePendienteID", id);
                cmd.Parameters.AddWithValue("@ClientesIDGenerado",      clientesID);
                cmd.Parameters.AddWithValue("@UsuarioGenerado",         usuario);
                cmd.Parameters.AddWithValue("@WaMsgIdCredenciales",
                    (object)msgIdCredenciales ?? DBNull.Value);
            });
        }

        /// <summary>
        /// Guarda información de error con el estado correspondiente.
        /// Estados válidos: ErrorProcesamiento, ErrorEnvioWhatsapp.
        /// SP: AltaClientePendiente_GuardarError
        /// </summary>
        public void GuardarError(long id, string estado, string errorInfo)
        {
            EjecutarSp("AltaClientePendiente_GuardarError", cmd =>
            {
                cmd.Parameters.AddWithValue("@AltaClientePendienteID", id);
                cmd.Parameters.AddWithValue("@Estado",                  estado);
                cmd.Parameters.AddWithValue("@ErrorInfo",               errorInfo);
            });
        }

        // ──────────────────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ──────────────────────────────────────────────────────────────

        private void EjecutarSp(string spName, Action<SqlCommand> parametros)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(spName, con) { CommandType = CommandType.StoredProcedure })
                {
                    parametros(cmd);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Ejecuta un SP que retorna FilasAfectadas como SELECT @@ROWCOUNT.
        /// </summary>
        private int EjecutarSpConRowCount(string spName, Action<SqlCommand> parametros)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(spName, con) { CommandType = CommandType.StoredProcedure })
                {
                    parametros(cmd);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                            return Convert.ToInt32(dr["FilasAfectadas"]);
                    }
                }
            }
            return 0;
        }

        private static AltaClientePendienteItem MapearFila(SqlDataReader dr)
        {
            T Val<T>(string col)
            {
                var v = dr[col];
                return v == DBNull.Value ? default(T) : (T)Convert.ChangeType(v, typeof(T));
            }

            DateTime? NullableDate(string col)
            {
                var v = dr[col];
                return v == DBNull.Value ? null : Convert.ToDateTime(v);
            }

            int? NullableInt(string col)
            {
                var v = dr[col];
                return v == DBNull.Value ? null : Convert.ToInt32(v);
            }

            string NullableStr(string col)
            {
                var v = dr[col];
                return v == DBNull.Value ? null : v.ToString();
            }

            return new AltaClientePendienteItem
            {
                AltaClientePendienteID = Val<long>("AltaClientePendienteID"),
                FechaAlta              = Val<DateTime>("FechaAlta"),
                TipoUsuario            = Val<int>("TipoUsuario"),
                LabelDocumento         = Val<string>("LabelDocumento"),
                Documento              = Val<string>("Documento"),
                Email                  = Val<string>("Email"),
                LabelNombre            = Val<string>("LabelNombre"),
                Nombre                 = Val<string>("Nombre"),
                Telefono               = Val<string>("Telefono"),
                TelefonoNormalizado    = Val<string>("TelefonoNormalizado"),
                Provincia              = Val<string>("Provincia"),
                Localidad              = Val<string>("Localidad"),
                LocalidadID            = Val<int>("LocalidadID"),
                DireccionEnvio         = Val<string>("DireccionEnvio"),
                CodigoPostal           = Val<string>("CodigoPostal"),
                Altura                 = Val<string>("Altura") ?? "",
                Latitud                = Val<string>("Latitud"),
                Longitud               = Val<string>("Longitud"),
                Password               = Val<string>("Password"),
                PayloadJson            = NullableStr("PayloadJson"),
                Estado                 = Val<string>("Estado"),
                RespuestaCliente       = NullableStr("RespuestaCliente"),
                FechaRespuesta         = NullableDate("FechaRespuesta"),
                WaMsgIdConfirmacion    = NullableStr("WaMsgIdConfirmacion"),
                WaMsgIdCredenciales    = NullableStr("WaMsgIdCredenciales"),
                ClientesIDGenerado     = NullableInt("ClientesIDGenerado"),
                UsuarioGenerado        = NullableStr("UsuarioGenerado"),
                FechaProcesado         = NullableDate("FechaProcesado"),
                ErrorInfo              = NullableStr("ErrorInfo"),
                YaProcesado            = Val<bool>("YaProcesado")
            };
        }
    }
}
