namespace VendedoresApi.SQL.Services
{
    public class VendedorTelefonoXVentaItem
    {
        public VendedorTelefonoXVentaItem(string telefono, string nombre, int vendedoresID, int proveedoresID)
        {
            Telefono = telefono;
            Nombre = nombre;
            VendedoresID = vendedoresID;
            ProveedoresID = proveedoresID;
        }

        public string Telefono { get; set; }
        public string Nombre { get; set; }
        public int VendedoresID { get; set; }
        public int ProveedoresID { get; set; }


    }
}