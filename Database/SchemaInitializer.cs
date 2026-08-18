using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using BCrypt.Net;

namespace VendedoresApi.Database;

/// <summary>
/// Crea (si hace falta) la base VendedoresApiDemo en LocalDB, sus tablas mínimas, los 3
/// stored procedures que usa el login multiempresa (VendedorItemController.ValidarVDyVersion)
/// y algunos datos semilla. Pensado para que "dotnet run" deje todo listo sin pasos manuales,
/// mismo criterio que IntegradorArchivosApi-Demo/Database/SchemaInitializer.cs y
/// WebMultiempresa-Demo.
/// </summary>
public static class SchemaInitializer
{
    public static async Task EnsureDatabaseAsync(string connectionString, ILogger logger)
    {
        var csBuilder = new SqlConnectionStringBuilder(connectionString);
        string databaseName = csBuilder.InitialCatalog;
        csBuilder.InitialCatalog = "master";

        // 1. Crear la base si no existe (CREATE DATABASE no puede correr dentro de una
        //    conexión ya abierta contra la base destino).
        await using (var masterConn = new SqlConnection(csBuilder.ConnectionString))
        {
            await masterConn.OpenAsync();

            const string sqlExiste = "SELECT database_id FROM sys.databases WHERE name = @Nombre";
            await using var cmdExiste = new SqlCommand(sqlExiste, masterConn);
            cmdExiste.Parameters.AddWithValue("@Nombre", databaseName);
            object? existe = await cmdExiste.ExecuteScalarAsync();

            if (existe == null || existe == DBNull.Value)
            {
                logger.LogInformation("Base {DatabaseName} no existe. Creándola...", databaseName);
                // El nombre de la base viene de nuestro propio appsettings.json (no de
                // input de usuario), así que se arma inline sin parametrizar.
                await using var cmdCreate = new SqlCommand($"CREATE DATABASE [{databaseName}]", masterConn);
                await cmdCreate.ExecuteNonQueryAsync();
            }
        }

        // 2. Conectar ya contra VendedoresApiDemo y crear tablas/SPs si hace falta.
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        const string sqlTablaExiste = "SELECT OBJECT_ID('dbo.Vendedores')";
        bool coreSchemaExiste;
        await using (var cmdChk = new SqlCommand(sqlTablaExiste, conn))
        {
            object? tablaExiste = await cmdChk.ExecuteScalarAsync();
            coreSchemaExiste = tablaExiste != null && tablaExiste != DBNull.Value;
        }

        if (coreSchemaExiste)
        {
            logger.LogInformation("Esquema de {DatabaseName} ya existe. No se vuelve a crear.", databaseName);
        }
        else
        {
            await CreateCoreSchemaAsync(conn, logger);
        }

        // ── Esquema de sincronización post-login (Clientes, Productos, Categorías, Combos) ──
        // Chequeo INDEPENDIENTE del de arriba: así corre también contra una base que ya
        // tenía el esquema "core" (Vendedores, etc.) de una sesión anterior pero todavía no
        // tiene estas tablas nuevas — evita que el usuario tenga que borrar la base a mano.
        const string sqlClientesExiste = "SELECT OBJECT_ID('dbo.Clientes')";
        bool syncSchemaExiste;
        await using (var cmdChk = new SqlCommand(sqlClientesExiste, conn))
        {
            object? tablaExiste = await cmdChk.ExecuteScalarAsync();
            syncSchemaExiste = tablaExiste != null && tablaExiste != DBNull.Value;
        }

        if (syncSchemaExiste)
        {
            logger.LogInformation("Esquema de sincronización (Clientes/Productos/Combos) ya existe. No se vuelve a crear.");
        }
        else
        {
            await CreateSyncSchemaAsync(conn, logger);
        }
    }

    private static async Task CreateCoreSchemaAsync(SqlConnection conn, ILogger logger)
    {
        logger.LogInformation("Creando esquema y sembrando datos de demo...");

        const string sqlSchema = @"
            CREATE TABLE dbo.Vendedores
            (
                VendedoresID            INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID                INT           NOT NULL,
                Codigo                   NVARCHAR(30)  NOT NULL,
                Clave                    NVARCHAR(100) NOT NULL, -- hash BCrypt (60 chars), no texto plano
                Nombre                   NVARCHAR(150) NOT NULL,
                TiposDeVendedoresID      INT           NOT NULL DEFAULT 1,
                DiaInicioRuta            INT           NOT NULL DEFAULT 1,
                CategoriasComercialesID  INT           NULL,
                VerTodasLasCategorias    BIT           NOT NULL DEFAULT 1,
                Baja                     BIT           NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.VendedorListasPrecios
            (
                Id             INT IDENTITY(1,1) PRIMARY KEY,
                VendedoresID   INT NOT NULL REFERENCES dbo.Vendedores(VendedoresID),
                EmpresaID      INT NOT NULL,
                ListasPreciosID INT NOT NULL
            );

            CREATE TABLE dbo.VendedorEstrellasDiarias
            (
                Id             INT IDENTITY(1,1) PRIMARY KEY,
                VendedoresID   INT           NOT NULL REFERENCES dbo.Vendedores(VendedoresID),
                EmpresaID      INT           NOT NULL,
                Fecha          DATE          NOT NULL,
                NumeroEstrella INT           NOT NULL,
                EstaEncendida  BIT           NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.VendedorEstrellasCoeficientes
            (
                Id                 INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID          INT           NOT NULL,
                CantidadEstrellas  INT           NOT NULL,
                CoeficienteComision DECIMAL(9,4) NOT NULL,
                Baja               BIT           NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.MensajesVendedor
            (
                MensajeID   INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID   INT           NOT NULL,
                VendedorID  INT           NOT NULL REFERENCES dbo.Vendedores(VendedoresID),
                Subject     NVARCHAR(200) NOT NULL,
                Mensage     NVARCHAR(MAX) NOT NULL,
                Leido       BIT           NOT NULL DEFAULT 0,
                Bloqueado   BIT           NOT NULL DEFAULT 0,
                Fecha       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
            );
        ";
        await using (var cmdSchema = new SqlCommand(sqlSchema, conn))
            await cmdSchema.ExecuteNonQueryAsync();

        // ── Stored procedures usados por VendedorItemController.ValidarVDyVersion ──

        const string spValidarAcceso = @"
            CREATE PROCEDURE dbo.Vendedores_TXValidarAcceso
                @Codigo    NVARCHAR(30),
                @EmpresaID INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT TOP 1
                    Clave, VendedoresID, Nombre, Codigo, TiposDeVendedoresID,
                    DiaInicioRuta, CategoriasComercialesID, VerTodasLasCategorias
                FROM dbo.Vendedores
                WHERE Codigo = @Codigo AND EmpresaID = @EmpresaID AND Baja = 0;
            END
        ";
        await using (var cmd = new SqlCommand(spValidarAcceso, conn)) await cmd.ExecuteNonQueryAsync();

        const string spListasPrecios = @"
            CREATE PROCEDURE dbo.Vendedores_TXListasPrecios
                @VendedoresID INT,
                @EmpresaID    INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT ListasPreciosID
                FROM dbo.VendedorListasPrecios
                WHERE VendedoresID = @VendedoresID AND EmpresaID = @EmpresaID;
            END
        ";
        await using (var cmd = new SqlCommand(spListasPrecios, conn)) await cmd.ExecuteNonQueryAsync();

        const string spEstrellasDiariasUltimas = @"
            CREATE PROCEDURE dbo.Vendedores_TXEstrellasDiariasUltimas
                @VendedoresID INT,
                @EmpresaID    INT
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @UltimaFecha DATE = (
                    SELECT MAX(Fecha) FROM dbo.VendedorEstrellasDiarias
                    WHERE VendedoresID = @VendedoresID AND EmpresaID = @EmpresaID
                );

                SELECT NumeroEstrella, EstaEncendida
                FROM dbo.VendedorEstrellasDiarias
                WHERE VendedoresID = @VendedoresID AND EmpresaID = @EmpresaID AND Fecha = @UltimaFecha
                ORDER BY NumeroEstrella;
            END
        ";
        await using (var cmd = new SqlCommand(spEstrellasDiariasUltimas, conn)) await cmd.ExecuteNonQueryAsync();

        const string spActualizarClave = @"
            CREATE PROCEDURE dbo.Vendedores_MActualizarClave
                @VendedoresID INT,
                @EmpresaID    INT,
                @Clave        NVARCHAR(100) -- hash BCrypt ya calculado en C#, no texto plano
            AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE dbo.Vendedores
                SET Clave = @Clave
                WHERE VendedoresID = @VendedoresID AND EmpresaID = @EmpresaID;
            END
        ";
        await using (var cmd = new SqlCommand(spActualizarClave, conn)) await cmd.ExecuteNonQueryAsync();

        const string spMensajesPorVendedor = @"
            CREATE PROCEDURE dbo.MensajesVendedor_TXListarPorVendedor
                @EmpresaID    INT,
                @VendedoresID INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT MensajeID, Bloqueado, Subject, Mensage, Leido, Fecha, VendedorID
                FROM dbo.MensajesVendedor
                WHERE EmpresaID = @EmpresaID AND VendedorID = @VendedoresID
                ORDER BY Fecha DESC;
            END
        ";
        await using (var cmd = new SqlCommand(spMensajesPorVendedor, conn)) await cmd.ExecuteNonQueryAsync();

        await SeedAsync(conn);

        logger.LogInformation("Esquema y datos de demo listos.");
    }

    private static async Task SeedAsync(SqlConnection conn)
    {
        // EmpresaID=1: flujo real de producción (login normal de la app ya corregida).
        int vendedoresID1 = await SeedVendedorAsync(conn, empresaId: 1);

        // EmpresaID=0: workaround de un bug ya corregido en AppVendedores2025-Demo
        // (Components/Pages/Index.razor.cs mandaba EmpresaID=0 en vez de 1). El fix ya está
        // aplicado del lado del cliente, pero se siembra igual por si alguien todavía prueba
        // con una build vieja de la app contra esta API — mismo Codigo/Clave que EmpresaID=1.
        await SeedVendedorAsync(conn, empresaId: 0);

        // Un mensaje de ejemplo para GetMensajesAVendedorMultiEmpresa (solo EmpresaID=1, el
        // flujo real de producción).
        const string sqlMensaje = @"
            INSERT INTO dbo.MensajesVendedor (EmpresaID, VendedorID, Subject, Mensage, Leido, Bloqueado, Fecha)
            VALUES (@EmpresaID, @VendedorID, @Subject, @Mensage, 0, 0, SYSUTCDATETIME())";
        await using (var cmd = new SqlCommand(sqlMensaje, conn))
        {
            cmd.Parameters.AddWithValue("@EmpresaID", 1);
            cmd.Parameters.AddWithValue("@VendedorID", vendedoresID1);
            cmd.Parameters.AddWithValue("@Subject", "Bienvenido");
            cmd.Parameters.AddWithValue("@Mensage", "Este es un mensaje de ejemplo para la demo.");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // Siembra el vendedor demo V001/Demo123! para un EmpresaID puntual, con sus listas de
    // precios, estrellas del día y coeficientes de comisión. Devuelve el VendedoresID
    // generado (usado para el mensaje de bienvenida de EmpresaID=1).
    private static async Task<int> SeedVendedorAsync(SqlConnection conn, int empresaId)
    {
        // La columna Clave guarda un hash BCrypt, nunca la contraseña en texto plano.
        // VendedorItemController.ValidarVDyVersion verifica con BCrypt.Verify(pwd, clave).
        // La contraseña de prueba documentada en el README ("Demo123!") sigue siendo la
        // misma — solo cambia cómo se guarda y se compara.
        const string sqlVendedor = @"
            INSERT INTO dbo.Vendedores
                (EmpresaID, Codigo, Clave, Nombre, TiposDeVendedoresID, DiaInicioRuta, CategoriasComercialesID, VerTodasLasCategorias, Baja)
            OUTPUT INSERTED.VendedoresID
            VALUES (@EmpresaID, @Codigo, @Clave, @Nombre, @TiposDeVendedoresID, @DiaInicioRuta, NULL, 1, 0)";

        int vendedoresID;
        await using (var cmd = new SqlCommand(sqlVendedor, conn))
        {
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Codigo", "V001");
            cmd.Parameters.AddWithValue("@Clave", BCrypt.Net.BCrypt.HashPassword("Demo123!"));
            cmd.Parameters.AddWithValue("@Nombre", "Vendedor Demo");
            cmd.Parameters.AddWithValue("@TiposDeVendedoresID", 1);
            cmd.Parameters.AddWithValue("@DiaInicioRuta", 1);
            vendedoresID = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // Listas de precios disponibles para el vendedor demo.
        foreach (int listaId in new[] { 1, 2 })
        {
            const string sqlLista = @"
                INSERT INTO dbo.VendedorListasPrecios (VendedoresID, EmpresaID, ListasPreciosID)
                VALUES (@VendedoresID, @EmpresaID, @ListaID)";
            await using var cmd = new SqlCommand(sqlLista, conn);
            cmd.Parameters.AddWithValue("@VendedoresID", vendedoresID);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@ListaID", listaId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Estrellas del día de hoy: 3 de 5 encendidas.
        var hoy = DateTime.UtcNow.Date;
        for (int numero = 1; numero <= 5; numero++)
        {
            const string sqlEstrella = @"
                INSERT INTO dbo.VendedorEstrellasDiarias (VendedoresID, EmpresaID, Fecha, NumeroEstrella, EstaEncendida)
                VALUES (@VendedoresID, @EmpresaID, @Fecha, @Numero, @Encendida)";
            await using var cmd = new SqlCommand(sqlEstrella, conn);
            cmd.Parameters.AddWithValue("@VendedoresID", vendedoresID);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Fecha", hoy);
            cmd.Parameters.AddWithValue("@Numero", numero);
            cmd.Parameters.AddWithValue("@Encendida", numero <= 3);
            await cmd.ExecuteNonQueryAsync();
        }

        // Coeficiente de comisión por cantidad de estrellas encendidas (0 a 5).
        (int cantidad, decimal coeficiente)[] coeficientes =
        {
            (0, 0.00m), (1, 0.01m), (2, 0.02m), (3, 0.035m), (4, 0.045m), (5, 0.06m)
        };
        foreach (var (cantidad, coeficiente) in coeficientes)
        {
            const string sqlCoef = @"
                INSERT INTO dbo.VendedorEstrellasCoeficientes (EmpresaID, CantidadEstrellas, CoeficienteComision, Baja)
                VALUES (@EmpresaID, @Cantidad, @Coeficiente, 0)";
            await using var cmd = new SqlCommand(sqlCoef, conn);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Coeficiente", coeficiente);
            await cmd.ExecuteNonQueryAsync();
        }

        return vendedoresID;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // Esquema de sincronización post-login: Clientes, Categorías/SubCategorías de Productos,
    // Productos y Combos. Alimenta los endpoints nuevos (DashboardEcom, ProductosItem,
    // combosPorFechaMultiEmpresa) que la app pide justo después de loguearse — ver
    // AppVendedores2025-Demo/Services/FuncionesSincronizar.cs líneas ~236-423 para el
    // contrato exacto que consumen.
    // ═══════════════════════════════════════════════════════════════════════════════════

    private static async Task CreateSyncSchemaAsync(SqlConnection conn, ILogger logger)
    {
        logger.LogInformation("Creando esquema de sincronización (Clientes/Productos/Combos) y sembrando datos de demo...");

        const string sqlSchema = @"
            CREATE TABLE dbo.Clientes
            (
                ClientesID   INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID    INT           NOT NULL,
                Codigo       NVARCHAR(30)  NOT NULL,
                Nombre       NVARCHAR(150) NOT NULL,
                Direccion    NVARCHAR(200) NOT NULL,
                Telefono     NVARCHAR(30)  NOT NULL,
                Latitud      FLOAT         NOT NULL,
                Longitud     FLOAT         NOT NULL,
                CUIT         NVARCHAR(20)  NULL,
                MCE          INT           NOT NULL DEFAULT 0,
                VendedoresID INT           NOT NULL,
                Baja         BIT           NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.CategoriasProductos
            (
                CategoriasProductosID INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID              INT           NOT NULL,
                Nombre                 NVARCHAR(100) NOT NULL
            );

            CREATE TABLE dbo.SubCategoriasProductos
            (
                SubCategoriasProductosID INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID                 INT           NOT NULL,
                Nombre                    NVARCHAR(100) NOT NULL,
                CategoriasProductosID     INT           NOT NULL REFERENCES dbo.CategoriasProductos(CategoriasProductosID)
            );

            CREATE TABLE dbo.Productos
            (
                ProductosID              INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID                INT            NOT NULL,
                CodigoDeProducto         NVARCHAR(30)   NOT NULL,
                Nombre                   NVARCHAR(150)  NOT NULL,
                Marca                    NVARCHAR(80)   NULL,
                CategoriasProductosID    INT            NOT NULL REFERENCES dbo.CategoriasProductos(CategoriasProductosID),
                SubCategoriasProductosID INT            NOT NULL REFERENCES dbo.SubCategoriasProductos(SubCategoriasProductosID),
                MarcasProductosID        INT            NOT NULL DEFAULT 1,
                FamiliaProductosID       INT            NOT NULL DEFAULT 1,
                Color                    INT            NOT NULL DEFAULT 0,
                StockEnUnidades          INT            NOT NULL DEFAULT 0,
                UnidadesPorBulto         INT            NOT NULL DEFAULT 1,

                PrecioUnitarioFinalEspecial          DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalAutoservicio      DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalAlamcene          DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalKiosko            DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioSalon                  DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioEcom                   DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalEspecialNeto       DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalAutoservicioNeto   DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalAlamceneNeto       DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioFinalKioskoNeto         DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioSalonNeto               DECIMAL(12,2) NOT NULL DEFAULT 0,
                PrecioUnitarioEcomNeto                DECIMAL(12,2) NOT NULL DEFAULT 0,

                Baja                     BIT NOT NULL DEFAULT 0,
                DesactivadoParaLaVenta   BIT NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.Combos
            (
                CombosID                     INT IDENTITY(1,1) PRIMARY KEY,
                EmpresaID                    INT            NOT NULL,
                Nombre                       NVARCHAR(150)  NOT NULL,
                Codigo                       NVARCHAR(30)   NOT NULL,
                CantidadMaximaPorCliente     INT            NOT NULL DEFAULT 0,
                CantidadMaximaPorFactura     INT            NOT NULL DEFAULT 0,
                RubroComb                    INT            NOT NULL DEFAULT 0,
                Intro                        NVARCHAR(200)  NULL,
                ImporteProductosFueraDeCombo DECIMAL(12,2)  NOT NULL DEFAULT 0,
                ProductoIDdeReferencia       INT            NULL,
                Baja                         BIT            NOT NULL DEFAULT 0
            );

            CREATE TABLE dbo.CombosProductos
            (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                CombosID        INT NOT NULL REFERENCES dbo.Combos(CombosID),
                ProductosID     INT NOT NULL REFERENCES dbo.Productos(ProductosID),
                Cantidad        INT NOT NULL DEFAULT 1,
                ComboTipo       INT NOT NULL DEFAULT 0,
                Descuento1      NVARCHAR(20) NULL,
                Descuento2      NVARCHAR(20) NULL,
                DpcID           INT NOT NULL DEFAULT 0,
                NrGrupoDinamico INT NOT NULL DEFAULT 0
            );
        ";
        await using (var cmdSchema = new SqlCommand(sqlSchema, conn))
            await cmdSchema.ExecuteNonQueryAsync();

        await SeedSyncDataAsync(conn);

        logger.LogInformation("Esquema y datos de demo de sincronización listos.");
    }

    private static async Task SeedSyncDataAsync(SqlConnection conn)
    {
        const int empresaId = 1; // Flujo real de producción (ver nota de EmpresaID=0 más arriba).

        // VendedoresID del vendedor demo V001 para esta empresa (ya sembrado por
        // SeedVendedorAsync, corrió antes que este método dentro de EnsureDatabaseAsync
        // — pero por si este bloque corre solo en una base que ya tenía Vendedores de una
        // sesión previa, se busca en vez de asumir el ID).
        int vendedoresID;
        await using (var cmdVd = new SqlCommand(
            "SELECT TOP 1 VendedoresID FROM dbo.Vendedores WHERE Codigo = @Codigo AND EmpresaID = @EmpresaID", conn))
        {
            cmdVd.Parameters.AddWithValue("@Codigo", "V001");
            cmdVd.Parameters.AddWithValue("@EmpresaID", empresaId);
            object? result = await cmdVd.ExecuteScalarAsync();
            vendedoresID = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        // ── Categorías (rubro: almacén/golosinas) ──────────────────────────────────────
        var categoriaIds = new Dictionary<string, int>();
        foreach (string nombre in new[] { "Almacén", "Bebidas", "Golosinas" })
        {
            const string sql = @"
                INSERT INTO dbo.CategoriasProductos (EmpresaID, Nombre)
                OUTPUT INSERTED.CategoriasProductosID
                VALUES (@EmpresaID, @Nombre)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            categoriaIds[nombre] = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // ── SubCategorías (una por categoría, alcanza para el demo) ────────────────────
        var subCategoriaIds = new Dictionary<string, int>();
        (string nombre, string categoria)[] subCategorias =
        {
            ("Almacén Seco", "Almacén"),
            ("Gaseosas y Aguas", "Bebidas"),
            ("Galletitas", "Golosinas")
        };
        foreach (var (nombre, categoria) in subCategorias)
        {
            const string sql = @"
                INSERT INTO dbo.SubCategoriasProductos (EmpresaID, Nombre, CategoriasProductosID)
                OUTPUT INSERTED.SubCategoriasProductosID
                VALUES (@EmpresaID, @Nombre, @CategoriasProductosID)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@CategoriasProductosID", categoriaIds[categoria]);
            subCategoriaIds[nombre] = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // ── Productos (12, catálogo de almacén/golosinas de la demo) ───────────────────
        // PrecioUnitarioFinalXxx: variaciones razonables por lista de precio a partir del
        // precio "Especial" base. XxxNeto = Final / 1.21, simulando el IVA descontado.
        (string codigo, string nombre, string marca, string subCategoria, int stock, int bulto, decimal precioBase)[] productosSeed =
        {
            ("ALM001", "Galletitas dulces surtidas 300g", "Dulzura",        "Galletitas",       120, 12, 1200m),
            ("ALM002", "Galletitas de agua 300g",          "CrocanTina",     "Galletitas",       110, 12,  950m),
            ("BEB001", "Gaseosa cola 2.25L",                "Cola Sur",       "Gaseosas y Aguas",  80,  6, 2200m),
            ("BEB002", "Gaseosa lima-limón 2.25L",          "Cítrica",        "Gaseosas y Aguas",  75,  6, 2100m),
            ("BEB003", "Agua mineral 1.5L",                 "Fuente Azul",    "Gaseosas y Aguas", 200,  6,  800m),
            ("ALM003", "Aceite de girasol 900ml",           "Girasol Dorado", "Almacén Seco",      60, 12, 2600m),
            ("ALM004", "Fideos tallarín 500g",              "Pastas del Sur", "Almacén Seco",     150, 24, 1100m),
            ("ALM005", "Arroz largo fino 1kg",               "Campo Fino",     "Almacén Seco",     140, 12, 1400m),
            ("ALM006", "Yerba mate 1kg",                     "Monte Verde",    "Almacén Seco",      90,  6, 2900m),
            ("ALM007", "Azúcar 1kg",                         "Dulce Campo",    "Almacén Seco",     160, 12,  900m),
            ("ALM008", "Café molido 250g",                   "Café Real",      "Almacén Seco",      70, 12, 2800m),
            ("ALM009", "Dulce de leche 400g",                "La Cremosa",     "Almacén Seco",     100, 12, 1700m),
        };

        var productoIds = new Dictionary<string, int>();
        int marcaId = 1;
        foreach (var p in productosSeed)
        {
            int subCategoriaId = subCategoriaIds[p.subCategoria];
            string categoriaDe = subCategorias.First(s => s.nombre == p.subCategoria).categoria;
            int categoriaId = categoriaIds[categoriaDe];

            decimal final = p.precioBase;
            decimal autoservicio = Math.Round(final * 1.05m, 2);
            decimal almacen = Math.Round(final * 0.98m, 2);
            decimal kiosko = Math.Round(final * 1.10m, 2);
            decimal salon = Math.Round(final * 1.15m, 2);
            decimal ecom = Math.Round(final * 1.02m, 2);

            decimal Neto(decimal precioFinal) => Math.Round(precioFinal / 1.21m, 2);

            const string sql = @"
                INSERT INTO dbo.Productos
                    (EmpresaID, CodigoDeProducto, Nombre, Marca, CategoriasProductosID, SubCategoriasProductosID,
                     MarcasProductosID, FamiliaProductosID, Color, StockEnUnidades, UnidadesPorBulto,
                     PrecioUnitarioFinalEspecial, PrecioUnitarioFinalAutoservicio, PrecioUnitarioFinalAlamcene,
                     PrecioUnitarioFinalKiosko, PrecioUnitarioSalon, PrecioUnitarioEcom,
                     PrecioUnitarioFinalEspecialNeto, PrecioUnitarioFinalAutoservicioNeto, PrecioUnitarioFinalAlamceneNeto,
                     PrecioUnitarioFinalKioskoNeto, PrecioUnitarioSalonNeto, PrecioUnitarioEcomNeto,
                     Baja, DesactivadoParaLaVenta)
                OUTPUT INSERTED.ProductosID
                VALUES
                    (@EmpresaID, @Codigo, @Nombre, @Marca, @CategoriasProductosID, @SubCategoriasProductosID,
                     @MarcasProductosID, @FamiliaProductosID, @Color, @Stock, @Bulto,
                     @Especial, @Autoservicio, @Almacen, @Kiosko, @Salon, @Ecom,
                     @EspecialNeto, @AutoservicioNeto, @AlmacenNeto, @KioskoNeto, @SalonNeto, @EcomNeto,
                     0, 0)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Codigo", p.codigo);
            cmd.Parameters.AddWithValue("@Nombre", p.nombre);
            cmd.Parameters.AddWithValue("@Marca", p.marca);
            cmd.Parameters.AddWithValue("@CategoriasProductosID", categoriaId);
            cmd.Parameters.AddWithValue("@SubCategoriasProductosID", subCategoriaId);
            cmd.Parameters.AddWithValue("@MarcasProductosID", marcaId);
            cmd.Parameters.AddWithValue("@FamiliaProductosID", categoriaId);
            // Color es un segmento de vendedor (se deriva del último dígito del código de
            // vendedor, ver Index.razor.cs: oParam.Color = txtVD[^1]), NO la categoría del
            // producto — VistaProductos.razor solo muestra productos con Color==0 (visible
            // para cualquier vendedor) o Color==<color del vendedor logueado>. 0 = visible
            // para todos, correcto para el catálogo general de la demo.
            cmd.Parameters.AddWithValue("@Color", 0);
            cmd.Parameters.AddWithValue("@Stock", p.stock);
            cmd.Parameters.AddWithValue("@Bulto", p.bulto);
            cmd.Parameters.AddWithValue("@Especial", final);
            cmd.Parameters.AddWithValue("@Autoservicio", autoservicio);
            cmd.Parameters.AddWithValue("@Almacen", almacen);
            cmd.Parameters.AddWithValue("@Kiosko", kiosko);
            cmd.Parameters.AddWithValue("@Salon", salon);
            cmd.Parameters.AddWithValue("@Ecom", ecom);
            cmd.Parameters.AddWithValue("@EspecialNeto", Neto(final));
            cmd.Parameters.AddWithValue("@AutoservicioNeto", Neto(autoservicio));
            cmd.Parameters.AddWithValue("@AlmacenNeto", Neto(almacen));
            cmd.Parameters.AddWithValue("@KioskoNeto", Neto(kiosko));
            cmd.Parameters.AddWithValue("@SalonNeto", Neto(salon));
            cmd.Parameters.AddWithValue("@EcomNeto", Neto(ecom));

            int nuevoId = (int)(await cmd.ExecuteScalarAsync())!;
            productoIds[p.codigo] = nuevoId;
            marcaId++;
        }

        // ── Clientes (8, coordenadas reales de lugares históricos de CABA) ─────────────
        (string codigo, string nombre, string direccion, double lat, double lng, string cuit, string telefono)[] clientesSeed =
        {
            ("C001", "Kiosco Obelisco",          "Av. 9 de Julio y Av. Corrientes, CABA", -34.6037, -58.3816, "20-40111222-3", "011-4111-2223"),
            ("C002", "Almacén Plaza de Mayo",     "Plaza de Mayo, CABA",                   -34.6083, -58.3712, "20-40222333-4", "011-4222-3334"),
            ("C003", "Despensa Recoleta",         "Junín 1760, CABA",                      -34.5875, -58.3931, "20-40333444-5", "011-4333-4445"),
            ("C004", "Kiosco Puerto Madero",      "Puente de la Mujer, Puerto Madero, CABA", -34.6118, -58.3634, "20-40444555-6", "011-4444-5556"),
            ("C005", "Almacén Caminito",          "Caminito, La Boca, CABA",               -34.6345, -58.3631, "20-40555666-7", "011-4555-6667"),
            ("C006", "Comercio Teatro Colón",     "Cerrito 628, CABA",                     -34.6010, -58.3831, "20-40666777-8", "011-4666-7778"),
            ("C007", "Despensa Congreso",         "Av. Rivadavia 1864, CABA",              -34.6095, -58.3924, "20-40777888-9", "011-4777-8889"),
            ("C008", "Kiosco Planetario",         "Av. Sarmiento y Belisario Roldán, CABA", -34.5652, -58.4131, "20-40888999-0", "011-4888-9990"),
        };

        foreach (var c in clientesSeed)
        {
            const string sql = @"
                INSERT INTO dbo.Clientes
                    (EmpresaID, Codigo, Nombre, Direccion, Telefono, Latitud, Longitud, CUIT, MCE, VendedoresID, Baja)
                VALUES
                    (@EmpresaID, @Codigo, @Nombre, @Direccion, @Telefono, @Latitud, @Longitud, @CUIT, 0, @VendedoresID, 0)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Codigo", c.codigo);
            cmd.Parameters.AddWithValue("@Nombre", c.nombre);
            cmd.Parameters.AddWithValue("@Direccion", c.direccion);
            cmd.Parameters.AddWithValue("@Telefono", c.telefono);
            cmd.Parameters.AddWithValue("@Latitud", c.lat);
            cmd.Parameters.AddWithValue("@Longitud", c.lng);
            cmd.Parameters.AddWithValue("@CUIT", c.cuit);
            cmd.Parameters.AddWithValue("@VendedoresID", vendedoresID);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── Combo "Combo Desayuno" (café + galletitas dulces + dulce de leche) ─────────
        // 15% de descuento sobre la suma individual, expresado en Descuento1 (texto,
        // porcentaje) tal cual lo espera CombosProductosItem/CombosItemJson del lado app.
        int comboId;
        const string sqlCombo = @"
            INSERT INTO dbo.Combos
                (EmpresaID, Nombre, Codigo, CantidadMaximaPorCliente, CantidadMaximaPorFactura,
                 RubroComb, Intro, ImporteProductosFueraDeCombo, ProductoIDdeReferencia, Baja)
            OUTPUT INSERTED.CombosID
            VALUES
                (@EmpresaID, @Nombre, @Codigo, 10, 50, 1, @Intro, 0, @ProductoRef, 0)";
        await using (var cmd = new SqlCommand(sqlCombo, conn))
        {
            cmd.Parameters.AddWithValue("@EmpresaID", empresaId);
            cmd.Parameters.AddWithValue("@Nombre", "Combo Desayuno");
            cmd.Parameters.AddWithValue("@Codigo", "COMBO001");
            cmd.Parameters.AddWithValue("@Intro", "Café + galletitas dulces + dulce de leche, con descuento.");
            cmd.Parameters.AddWithValue("@ProductoRef", productoIds["ALM008"]);
            comboId = (int)(await cmd.ExecuteScalarAsync())!;
        }

        (string codigoProducto, int cantidad)[] comboItems =
        {
            ("ALM008", 1), // Café molido 250g
            ("ALM001", 1), // Galletitas dulces surtidas 300g
            ("ALM009", 1), // Dulce de leche 400g
        };
        foreach (var (codigoProducto, cantidad) in comboItems)
        {
            const string sql = @"
                INSERT INTO dbo.CombosProductos
                    (CombosID, ProductosID, Cantidad, ComboTipo, Descuento1, Descuento2, DpcID, NrGrupoDinamico)
                VALUES
                    (@CombosID, @ProductosID, @Cantidad, 0, @Descuento1, '0', 0, 0)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CombosID", comboId);
            cmd.Parameters.AddWithValue("@ProductosID", productoIds[codigoProducto]);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Descuento1", "15");
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
