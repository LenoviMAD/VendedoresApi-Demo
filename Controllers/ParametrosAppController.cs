using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendedoresApi.SQL.Services;

namespace VendedoresApi.Controllers;

// Se llama antes de tener sesión (config de soporte visible en pantalla de login),
// sigue exigiendo x-api-key pero no requiere JWT.
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class ParametrosAppController : ControllerBase
{
    private readonly ILogger<ParametrosAppController> _logger;

    public ParametrosAppController(ILogger<ParametrosAppController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Devuelve la configuración de soporte de la app (mail y link de WhatsApp).
    /// </summary>
    [HttpGet("Soporte")]
    [ProducesResponseType(typeof(SoporteAppConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetSoporte()
    {
        try
        {
            var mail = ParametrosAppConfig.GetParametros("ApiSettings", "MailSoporte")
                       ?? "soporte@empresademo.example";

            var linkWa = ParametrosAppConfig.GetParametros("ApiSettings", "LinkWhatsappSoporteBase")
                         ?? "https://api.whatsapp.com/send?phone=5490000000000";

            return Ok(new SoporteAppConfigResponse
            {
                MailSoporte = mail,
                LinkWhatsappSoporte = linkWa
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de soporte");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "No se pudo obtener la configuración de soporte." });
        }
    }
}
