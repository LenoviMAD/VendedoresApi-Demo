namespace VendedoresApi.Models
{
    public class ParametroApi
    {
        public ParametroApi(string tiempoEnvioGps, string GPSLiberado, string version )
        {
            this.tiempoEnvioGps = tiempoEnvioGps;
            this.GPSLiberado = GPSLiberado;
            this.version = version;
        }

        public string tiempoEnvioGps { get; set; }
        public string GPSLiberado { get; set; }
        public string version { get; set; }
    }
}


