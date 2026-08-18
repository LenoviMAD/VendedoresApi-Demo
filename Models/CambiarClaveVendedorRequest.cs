namespace VendedoresApi.Models
{
    public class CambiarClaveVendedorRequest
    {
        public int VendedoresID { get; set; }
        public string NuevaClave { get; set; } = "";
        public int EmpresaID { get; set; } = 0;
    }
}
