using System;

namespace VendedoresApi.Models
{
    public class AltaClienteAPPItem
    {
        public int TipoUsuario { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string DNI { get; set; } = "";
        public string DireccionEnvio { get; set; } = "";
        public string DireccionEnvio_CodigoPostal { get; set; } = "";
        public string DireccionEnvio_Latitud { get; set; } = "0";
        public string DireccionEnvio_Longitud { get; set; } = "0";
        public string Provincia { get; set; } = "";
        public string Localidad { get; set; } = "";
        public int localidadSeleccionadaId { get; set; }
        public string Telefono { get; set; } = "";
        public string Altura { get; set; } = "";
    }
}
