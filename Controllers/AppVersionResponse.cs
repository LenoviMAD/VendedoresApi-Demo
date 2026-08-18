namespace VendedoresApi.Controllers;

public class AppVersionResponse
{
    public string VersionMinima { get; set; } = "";
    public string VersionActual { get; set; } = "";
    public bool ActualizacionObligatoria { get; set; }
    public string Mensaje { get; set; } = "";
    public string LinkAndroid { get; set; } = "";
    public string LinkIos { get; set; } = "";
}
