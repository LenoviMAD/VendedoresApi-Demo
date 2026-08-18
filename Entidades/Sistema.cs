// Ver Entidades/ClienteYPedidos.cs para la nota general sobre este modelado local.
using System;
using System.Collections.Generic;

namespace EntidadesAppVendedores
{
    // Tabla ParametrosItem (SQLite local) — almacén clave/valor genérico usado por
    // ParametrosServices.cs (AddorUpdate / GetByNombre). ParamDecimal es nullable
    // porque el código compara "res.ParamDecimal == null" antes de castear a int.
    public class ParametrosItem
    {
        public int Id { get; set; }
        public string ParamNombre { get; set; } = "";
        public string ParamString { get; set; } = "";
        public decimal? ParamDecimal { get; set; }
        public DateTime ParamDateTime { get; set; }

        public ParametrosItem() { }

        public ParametrosItem(string paramNombre, string paramString, decimal paramDecimal, DateTime paramDateTime)
        {
            ParamNombre = paramNombre;
            ParamString = paramString ?? "";
            ParamDecimal = paramDecimal;
            ParamDateTime = paramDateTime;
        }
    }

    // Respuesta de VendedorItem/ValidarVD (login). Shape relevado de
    // FuncionesSincronizar.ValidarVD y Components/Pages/Index.razor.cs (uso extensivo
    // de "res.<Campo>" tras deserializar la respuesta JSON del API).
    public class ValidaUsuario
    {
        public string? CodError { get; set; }
        public string? Mensaje { get; set; }
        public bool OcultarBotonEliminar { get; set; }
        public bool Autogestion { get; set; }
        public bool DescargarImagen { get; set; }
        public int CantidadEstrellitasVendedor { get; set; }
        public string? UrlGPSpoint { get; set; }
        public decimal CoheficienteComision { get; set; }
        public int tiempoEnvioGps { get; set; }
        public int VendedorID { get; set; }
        public string? UrlHomeVendedor { get; set; }
        public string? GoolgeApiKey { get; set; }
        public int TiposDeVendedoresID { get; set; }
        public string? NombreDelVendedor { get; set; }
        public List<object> ListEstrellas { get; set; } = new();
        public List<int> ListadDisponibles { get; set; } = new();

        // Campos agregados por el login multiempresa (VendedorItemController.ValidarVDyVersion
        // con {empresaID}) que el cliente todavía no consume pero la API ya devuelve — ver
        // Vendedores_TXValidarAcceso. Mismo DTO compartido, sólo se le suman columnas.
        public string? CodigoVendedor { get; set; }
        public int DiaInicioRuta { get; set; }
        public int? CategoriasComercialesID { get; set; }
        public bool VerTodasLasCategorias { get; set; }
        public int EmpresaID { get; set; }
        public bool EsPago { get; set; }

        public ValidaUsuario() { }

        // Constructor usado como fallback cuando no hay usuario/contraseña o falla el
        // login (ver FuncionesSincronizar.ValidarVD).
        public ValidaUsuario(List<int> listadDisponiblesPreciosCliente)
        {
            ListadDisponibles = listadDisponiblesPreciosCliente ?? new List<int>();
            CodError = "2";
        }
    }

    // Tabla GPSLocationPointItem (SQLite local). Nombres de propiedad en minúscula
    // ("date", "latitude", etc.) tal cual los usa GPSService.cs; "speed" guarda en
    // realidad el ClienteID por reutilización de columna (ver comentario original:
    // "speed va a pasar a ser en la tabla clienteID").
    public class GPSLocationPointItem
    {
        public int GPSLocationPointID { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public DateTime date { get; set; }
        public string? username { get; set; }
        public string? keyEmpresa { get; set; }
        public string? Pwd { get; set; }
        public string? Telefono { get; set; }
        public string? SessionId { get; set; }
        public decimal accuracy { get; set; }
        public int Direction { get; set; }
        public string? AppVersion { get; set; }
        public decimal distance { get; set; }
        // ClienteID codificado en esta columna (ver nota de la clase). Se usa via
        // Convert.ToInt32(point.speed) / (int)ultimoPuntoTrasmitido.speed.
        public decimal speed { get; set; }
        // 0/1: si ya se transmitió el punto al servidor de tracking.
        public int Tramsmitido { get; set; }

        public GPSLocationPointItem() { }

        public GPSLocationPointItem(decimal latitude, decimal longitude, int clienteID, DateTime date,
            string username, int gpsLocationPointID, string keyEmpresa, string pwd, string telefono,
            string sessionId, decimal accuracy, int direction, string appVersion, int tramsmitido)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            speed = clienteID;
            this.date = date;
            this.username = username;
            GPSLocationPointID = gpsLocationPointID;
            this.keyEmpresa = keyEmpresa;
            Pwd = pwd;
            Telefono = telefono;
            SessionId = sessionId;
            this.accuracy = accuracy;
            Direction = direction;
            AppVersion = appVersion;
            Tramsmitido = tramsmitido;
        }
    }

    // Punto GPS en memoria (no persistido) usado por GPSService.GetCurrentLocationV2 /
    // GrabarUbicacionV2 / OrdenarClientesporDistancia. Nombres en minúscula tal cual
    // los usa el código ("latitud", "longitud", "fecha", etc.).
    public class Ubicacion
    {
        public double latitud { get; set; }
        public double longitud { get; set; }
        public string? codCliente { get; set; }
        public DateTime? fecha { get; set; }
        public int vdID { get; set; }
        public string? vdpwd { get; set; }
        public string? vdCod { get; set; }
        public decimal Accuracy { get; set; }
    }

    // Tabla ImagenAppItem (SQLite local) — caché local de imágenes de producto.
    public class ImagenAppItem
    {
        public int ProductosID { get; set; }
        public string ImagenChica { get; set; } = "";
        public DateTime FechaUltimaAct { get; set; }

        public ImagenAppItem() { }

        public ImagenAppItem(int productosID, string imagenChica, DateTime fechaUltimaAct)
        {
            ProductosID = productosID;
            ImagenChica = imagenChica ?? "";
            FechaUltimaAct = fechaUltimaAct;
        }
    }

    // Respuesta del API al pedir la imagen de un producto puntual
    // (FuncionesSincronizar.SincronizarImagenFromApi). No es tabla SQLite.
    public class ImagenSqlItem
    {
        public int ProductosID { get; set; }
        public string ImagenChica { get; set; } = "";
    }

    // Mensaje de texto libre configurable por el vendedor (Index.razor.cs), guardado
    // en localStorage como JSON, no en SQLite.
    public class TextoVDItem
    {
        public string TextoLibreVD { get; set; } = "";

        public TextoVDItem() { }

        public TextoVDItem(string textoLibreVD)
        {
            TextoLibreVD = textoLibreVD;
        }
    }

    // Mensaje recibido del back-office (Mensajes.razor / VendedorItem/mensajes).
    // "Mensage" (sin la primera "j") es el nombre real usado por el código, no un
    // error de tipeo de este modelado.
    public class MensajeAVendedorItem
    {
        public int MensajeID { get; set; }
        public string? Subject { get; set; }
        public string? Mensage { get; set; }
        public DateTime Fecha { get; set; }
        public bool Bloqueado { get; set; }
        public bool Leido { get; set; }
        public int VendedorID { get; set; }

        public MensajeAVendedorItem() { }

        // Usado por VendedorItemController.GetMensajesAVendedorMultiEmpresa al mapear
        // filas de MensajesVendedor_TXListarPorVendedor.
        public MensajeAVendedorItem(int mensajeId, bool bloqueado, string? subject, string? mensage,
            bool leido, DateTime fecha, int vendedorId)
        {
            MensajeID = mensajeId;
            Bloqueado = bloqueado;
            Subject = subject;
            Mensage = mensage;
            Leido = leido;
            Fecha = fecha;
            VendedorID = vendedorId;
        }
    }

    // DTO enviado a LogAccionApp/AgregarLogAccionApp (ver FuncionesSincronizar.cs e
    // Index.razor.cs). Shape documentado también en analisis-end-to-end.md sección 1.
    public class PeticionLogAccionApp
    {
        public int VendedorID { get; set; }
        public string? Usuario { get; set; }
        public string? CodigoCliente { get; set; }
        public string? CodigoAccion { get; set; }
        public string? Accion { get; set; }
        public string? Aplicacion { get; set; }
        public string? NombreCelular { get; set; }
        public string? Mensaje { get; set; }
        public DateTime Fecha { get; set; }
    }

    // Instanciada una vez en FuncionesSincronizar.Sincronizar ("EntDebug oEntDebug =
    // new EntDebug();") pero la variable nunca se usa después: no hay ningún miembro
    // real consumido en el código. Se deja como stub vacío en vez de inventar una API
    // de "modo debug" que no existe en este repo.
    public class EntDebug
    {
    }
}
