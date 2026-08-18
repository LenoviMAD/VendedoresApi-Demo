# VendedoresApi

Backend para una app de vendedores en ruta (`AppVendedores2025`): login, catálogo, combos, dashboard de cliente. **ASP.NET Core Web API (.NET 8)**, **ADO.NET puro** contra SQL Server (sin ORM), con autenticación en dos capas: API key de aplicación + JWT por usuario.

## Alcance

Implementé el camino crítico que necesita la app cliente — login, dashboard (clientes + catálogo + combos), catálogo de productos, mensajes al vendedor — dejando afuera funcionalidad secundaria para mantener el proyecto enfocado y fácil de recorrer.

## Seguridad

- **API key de aplicación**: header `x-api-key: vendedoresapi-demo-2026`, requerida en todas las rutas.
- **JWT por usuario**: el login (`VendedorItemController`) emite un JWT en el header de respuesta `X-Auth-Token` al validar usuario/clave (hash con BCrypt). El resto de los endpoints están protegidos con `[Authorize]` (`Authorization: Bearer <token>`) — `VendedorItemController`, `AppVersionController` y `ParametrosAppController` quedan `[AllowAnonymous]` por ser los que se llaman antes de tener sesión (siguen pidiendo la API key igual).
- Swagger (`/swagger`) tiene los botones "Authorize" para ambos esquemas.

## API a la que se conecta

Ninguna — es standalone, habla directo con su propia base SQL Server LocalDB vía ADO.NET. Es esta misma API la que consume [`AppVendedores2025-Demo`](../AppVendedores2025-Demo) como backend (ver "Proyecto relacionado" más abajo).

## Requisitos

- .NET 8 SDK
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)

## Cómo correrlo

```bash
dotnet run --project VendedoresApi.csproj --urls http://localhost:5101
```

Al arrancar por primera vez crea la base `VendedoresApiDemo`, las tablas, los stored procedures (`Vendedores_TXValidarAcceso`, `Vendedores_TXListasPrecios`, `Vendedores_TXEstrellasDiariasUltimas`, `Vendedores_MActualizarClave`, `MensajesVendedor_TXListarPorVendedor`) y siembra datos de prueba — sin pasos manuales.

## Credenciales

| Vendedor | Clave | EmpresaID |
|---|---|---|
| `V001` | `Demo123!` | `1` |

## Datos sembrados

- 1 vendedor (`V001`) con listas de precio, estrellas y coeficiente de comisión.
- 8 clientes ubicados en lugares públicos de CABA (Obelisco, Plaza de Mayo, Recoleta, Puerto Madero, Caminito, Teatro Colón, Congreso, Planetario).
- 12 productos de almacén/bebidas/golosinas, con precios por lista (Especial/Autoservicio/Almacén/Kiosko/Salón/E-commerce) y su versión neta.
- 3 categorías, 3 subcategorías.
- 1 combo ("Combo Desayuno": café + galletitas dulces + dulce de leche, 15% off).
- Recomendaciones ("lo que te gusta") por cliente: los primeros 4 productos de la empresa.
- 1 mensaje de bienvenida para el vendedor.

## Endpoints principales

- `GET /VendedorItem/{vd}/{pwd}/{version}/{empresaID}` — login, emite JWT en `X-Auth-Token`.
- `GET /AppVersion` — versión mínima requerida de la app.
- `GET /ParametrosApp/Soporte` — datos de contacto de soporte.
- `GET /DashboardEcom/{vendedorID}/{color}/{empresaID}` — endpoint unificado: clientes, categorías, subcategorías, recomendaciones.
- `GET /ProductosItem/productosPorFecha/{vd}/0/{version}/-1/{empresaID}` — catálogo completo.
- `GET /ProductosItem/StockYPreciosProductos/{fecha}/{empresaID}`
- `GET /ProductosItem/ProductosActualItems/{fecha}/{empresaID}`
- `GET /ProductosItem/bloqueadosYSubcategorias/{vendedorID}/{empresaID}`
- `GET /combosPorFechaMultiEmpresa/{vendedorId}/{fecha}/{empresaID}` — combos del vendedor.

## Cómo probar

```bash
# 1. Login
curl http://localhost:5101/VendedorItem/V001/Demo123!/135/1 -H "x-api-key: vendedoresapi-demo-2026" -D -
# Copiar el valor del header X-Auth-Token de la respuesta.

# 2. Cualquier endpoint protegido
curl http://localhost:5101/DashboardEcom/1/1/1 \
  -H "x-api-key: vendedoresapi-demo-2026" \
  -H "Authorization: Bearer <token>"
```

O directamente con la app cliente: [`AppVendedores2025-Demo`](../AppVendedores2025-Demo), ya apuntada a `http://localhost:5101`.

## Proyecto relacionado

`AppVendedores2025-Demo` es el cliente de esta API en este portfolio. `IntegradorArchivosApi-Demo` es un backend distinto (el de `SincroApp-Demo`) — no confundir los dos.
