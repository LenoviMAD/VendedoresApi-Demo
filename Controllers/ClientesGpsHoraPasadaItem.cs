

namespace VendedoresApi.Controllers
{
    public class ClientesGpsHoraPasadaItem
    {
        public ClientesGpsHoraPasadaItem(int cliID, DateTime? horaPrimerPunto)
        {
            CliID = cliID;
            HoraPrimerPunto = horaPrimerPunto;
        }

        public int CliID { get; set; }
        public DateTime? HoraPrimerPunto { get; set; }

    }

}
