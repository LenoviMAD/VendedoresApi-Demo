using System;

namespace VendedoresApi.Models
{
    /// <summary>
    /// Representa un registro de solicitud de alta pendiente de confirmación por WhatsApp.
    /// Mapea la tabla [dbo].[AltaClientePendiente].
    /// </summary>
    public class AltaClientePendienteItem
    {
        // IDENTIFICACIÓN
        public long AltaClientePendienteID { get; set; }
        public DateTime FechaAlta { get; set; }

        // DATOS DEL FORMULARIO
        public int TipoUsuario { get; set; }
        public string LabelDocumento { get; set; } = "";       // "DNI" o "CUIT"
        public string Documento { get; set; } = "";
        public string Email { get; set; } = "";
        public string LabelNombre { get; set; } = "";          // "Nombre" o "Razón Social"
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string TelefonoNormalizado { get; set; } = "";  // "549XXXXXXXXXX"
        public string Provincia { get; set; } = "";
        public string Localidad { get; set; } = "";
        public int LocalidadID { get; set; }
        public string DireccionEnvio { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Latitud { get; set; } = "0";
        public string Longitud { get; set; } = "0";
        public string Password { get; set; } = "";

        // PAYLOAD COMPLETO (JSON del body del POST original)
        public string? PayloadJson { get; set; }

        // ESTADO
        /// <summary>
        /// PendienteConfirmacion | Confirmado | Rechazado | CredencialesEnviadas | ErrorProcesamiento | ErrorEnvioWhatsapp
        /// </summary>
        public string Estado { get; set; } = "PendienteConfirmacion";

        // RESPUESTA DEL CLIENTE
        /// <summary>
        /// "confirmar_si" o "confirmar_no"
        /// </summary>
        public string? RespuestaCliente { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        // WHATSAPP META
        public string? WaMsgIdConfirmacion { get; set; }
        public string? WaMsgIdCredenciales { get; set; }

        // RESULTADO
        public int? ClientesIDGenerado { get; set; }
        public string? UsuarioGenerado { get; set; }
        public DateTime? FechaProcesado { get; set; }
        public string? ErrorInfo { get; set; }

        // CONTROL IDEMPOTENCIA
        public bool YaProcesado { get; set; }
    }

    /// <summary>
    /// Constantes de estado para AltaClientePendiente.
    /// </summary>
    public static class EstadoAltaCliente
    {
        public const string PendienteConfirmacion = "PendienteConfirmacion";
        public const string Confirmado            = "Confirmado";
        public const string Rechazado             = "Rechazado";
        public const string CredencialesEnviadas  = "CredencialesEnviadas";
        public const string ErrorProcesamiento    = "ErrorProcesamiento";
        public const string ErrorEnvioWhatsapp    = "ErrorEnvioWhatsapp";
    }
}
