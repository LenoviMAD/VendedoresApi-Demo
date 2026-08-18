namespace VendedoresApi.Models
{
    /// <summary>
    /// Payload enviado por GrabarV2 (GnmDisEntidades) luego de crear exitosamente un cliente nuevo.
    /// Permite que la API de vendedores actualice AltaClientePendiente y notifique por WhatsApp.
    /// </summary>
    public class NotificarNuevoClienteRequest
    {
        /// <summary>ID del cliente recién grabado en la tabla Clientes.</summary>
        public int ClientesID { get; set; }

        /// <summary>Código de acceso al portal (campo UsiarioWeb). Puede ser vacío si el caller no lo genera.</summary>
        public string Codigo { get; set; } = "";

        /// <summary>Contraseña del portal (campo ClaveWeb). Puede ser vacía si el caller no la genera.</summary>
        public string Password { get; set; } = "";

        /// <summary>Nombre o razón social del cliente.</summary>
        public string Nombre { get; set; } = "";

        /// <summary>Email del cliente.</summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Teléfono limpio (puede venir en formato local o normalizado 549XXXXXXXXXX).
        /// La API lo normaliza internamente antes de buscar en AltaClientePendiente.
        /// </summary>
        public string Telefono { get; set; } = "";

        /// <summary>ID del usuario (operador) que realizó el alta.</summary>
        public int UsuarioID { get; set; }
    }
}
