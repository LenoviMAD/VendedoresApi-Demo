namespace VendedoresApi.SQL.Services
{
    public class CombosClientesYaCompraronItem
    {
        public CombosClientesYaCompraronItem(int clientesID, int combosID)
        {
            ClientesID = clientesID;
            CombosID = combosID;
        }

        public int ClientesID { get; set; }
        public int CombosID { get; set; }
    }
}