using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VendedoresApi.SQL.Services;
using VendedoresApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers + JSON (respeta mayúsculas exactas si usás PascalCase) =====
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
    o.JsonSerializerOptions.DictionaryKeyPolicy = null;
});

// ===== Servicios de Dominio =====
builder.Services.AddSingleton<AltaClientePendienteService>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();

// ===== JWT Bearer =====
// Emitido por VendedorItemController tras un login exitoso (header X-Auth-Token).
// El resto de los endpoints exige este token vía RequireAuthorization() más abajo.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException("Falta configurar Jwt:SigningKey en appsettings.json.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// ===== Swagger (con seguridad ApiKey + Bearer) =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vendedores API", Version = "v1" });
    c.CustomSchemaIds(t => t.ToString());

    var headerName = builder.Configuration["ApiKeyAuth:HeaderName"] ?? "x-api-key";
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = $"Ingresá tu API key en el header '{headerName}'.",
        Name = headerName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Usá: Bearer <JWT devuelto por VendedorItem en el header X-Auth-Token>",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    // Inyecta el parámetro x-api-key en cada operación de Swagger UI.
    c.OperationFilter<InjectApiKeyHeaderFilter>();
});

var app = builder.Build();

// Crea la base VendedoresApiDemo (LocalDB), tablas, SPs de login y datos semilla
// si no existen todavía. Sin esto el primer "dotnet run" fallaría contra una base vacía.
var connStrMultiEmpresa = app.Configuration.GetConnectionString("ConnectionMultiEmpresa")!;
await VendedoresApi.Database.SchemaInitializer.EnsureDatabaseAsync(connStrMultiEmpresa, app.Logger);

app.UseMiddleware<ExceptionHandlingMiddleware>();

// ===== Swagger libre (no exige api-key para ver la doc, sí para probar endpoints) =====
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.ConfigObject.AdditionalItems["persistAuthorization"] = true;
});

// ===== Seguridad por API-KEY =====
// Se exige en TODOS los endpoints, incluidos los [AllowAnonymous] (login, versión de app,
// parámetros de soporte) — ahí "anónimo" significa "sin JWT todavía", no "sin api key".
var exactCaseJson = new JsonSerializerOptions { PropertyNamingPolicy = null, DictionaryKeyPolicy = null };
var expectedKey = builder.Configuration["ApiKeyAuth:Key"] ?? "";
var headerNameDefault = builder.Configuration["ApiKeyAuth:HeaderName"] ?? "x-api-key";
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/swagger") || ctx.Request.Path.StartsWithSegments("/api/ping"))
    {
        await next();
        return;
    }

    if (string.IsNullOrEmpty(expectedKey))
    {
        // Sin key configurada: no bloqueamos (entorno de desarrollo suelto).
        await next();
        return;
    }

    if (!ctx.Request.Headers.TryGetValue(headerNameDefault, out var providedKey) ||
        !string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(
            new { error = $"Header '{headerNameDefault}' ausente o inválido." }, exactCaseJson));
        return;
    }

    await next();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Protege todos los controllers por default; los que necesitan ser accesibles antes del
// login llevan [AllowAnonymous] a nivel de controller (VendedorItem, AppVersion, ParametrosApp).
app.MapControllers().RequireAuthorization();

// Endpoint de prueba (sigue accesible sin token ni api-key)
app.MapGet("/api/ping", () =>
    Results.Json(new { Ok = true, at = DateTimeOffset.UtcNow }, options: exactCaseJson)
);

app.Run();
