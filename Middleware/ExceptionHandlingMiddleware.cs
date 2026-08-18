using System.Data.SqlClient;
using System.Net;
using System.Text.Json;

namespace VendedoresApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "La solicitud fue cancelada porque el cliente desconecto la conexion.");
        }
        catch (Exception ex)
        {
            var (statusCode, message) = MapException(ex);

            _logger.LogError(ex, "Se produjo una excepcion no controlada.");

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                codigo = statusCode,
                error = message,
                detalle = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }

    private static (int StatusCode, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            SqlException sqlEx when sqlEx.Number == -2 =>
                ((int)HttpStatusCode.GatewayTimeout, "Sin respuesta de la base de datos."),

            SqlException =>
                ((int)HttpStatusCode.ServiceUnavailable, "Base de datos no disponible."),

            TimeoutException =>
                ((int)HttpStatusCode.GatewayTimeout, "Sin respuesta de un servicio interno."),

            TaskCanceledException taskCanceledEx when taskCanceledEx.InnerException is TimeoutException =>
                ((int)HttpStatusCode.GatewayTimeout, "Sin respuesta de un servicio interno."),

            TaskCanceledException =>
                ((int)HttpStatusCode.RequestTimeout, "La solicitud fue cancelada por demora."),

            HttpRequestException =>
                ((int)HttpStatusCode.BadGateway, "Error al comunicarse con un servicio externo."),

            ArgumentNullException =>
                ((int)HttpStatusCode.BadRequest, "La solicitud contiene datos incorrectos."),

            ArgumentException =>
                ((int)HttpStatusCode.BadRequest, "La solicitud contiene datos incorrectos."),

            _ =>
                ((int)HttpStatusCode.InternalServerError, "Error interno del servidor.")
        };
    }
}
