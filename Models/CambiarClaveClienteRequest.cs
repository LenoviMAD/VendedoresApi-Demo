namespace VendedoresApi.Models
{
    public class CambiarClaveClienteRequest
    {
        public int ClientesID { get; set; }
        public string NuevaClave { get; set; } = "";
        public int EmpresaID { get; set; } = 0;
    }
}
