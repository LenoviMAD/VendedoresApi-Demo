using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendedoresApi.SQL.Services;

namespace VendedoresApi.Controllers;

// Se llama antes de tener sesión (chequeo de versión al arrancar la app), sigue
// exigiendo x-api-key pero no requiere JWT.
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AppVersionController : ControllerBase
{
    private readonly ILogger<AppVersionController> _logger;
    private readonly IConfiguration _configuration;

    public AppVersionController(ILogger<AppVersionController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Devuelve la version minima requerida de la aplicacion movil y los links de descarga.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AppVersionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetVersionRequerida()
    {
        try
        {
            var versionMinima = ParametrosAppConfig.GetParametros("ApiSettings", "VersionMinimaApp") ?? "135";
            var versionActual = ParametrosAppConfig.GetParametros("ApiSettings", "VersionActualApp") ?? "135";
            var flagObligatoria = ParametrosAppConfig.GetParametros("ApiSettings", "ActualizacionObligatoria");
            var mensaje = ParametrosAppConfig.GetParametros("ApiSettings", "MensajeActualizacion")
                          ?? "Para continuar usando la aplicacion, debes actualizarla.";
            var linkAndroid = _configuration["ApiSettings:LinkAndroidApp"]
                              ?? "https://play.google.com/store/apps/details?id=com.empresademo.ventas";
            var linkIos = _configuration["ApiSettings:LinkIosApp"]
                          ?? "https://apps.apple.com/app/empresademo/id0000000000";

            bool esObligatoria = string.IsNullOrWhiteSpace(flagObligatoria)
                || flagObligatoria.Equals("true", StringComparison.OrdinalIgnoreCase);

            var response = new AppVersionResponse
            {
                VersionMinima = versionMinima,
                VersionActual = versionActual,
                ActualizacionObligatoria = esObligatoria,
                Mensaje = mensaje,
                LinkAndroid = linkAndroid,
                LinkIos = linkIos
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la version requerida de la app");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "No se pudo obtener la informacion de version." });
        }
    }
}
