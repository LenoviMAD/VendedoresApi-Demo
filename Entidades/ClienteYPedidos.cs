// Modelado local mínimo que reemplaza a las librerías externas EntidadesDesarrollo /
// EntidadesAppVendedores (no disponibles en este entorno). Contiene únicamente las
// clases y propiedades que el código de AppVendedores2025-Demo usa realmente.
// Fuente de los nombres de campo: uso real en Services/*.cs y Components/Pages/*.razor
// (ver también SYSTEM_DESIGN.md, sección "5. Modelo de Dominio", y analisis-end-to-end.md).
using System;
using System.Collections.Generic;

namespace EntidadesAppVendedores
{
    // Tabla ClienteItem (SQLite local). Los nombres con prefijo "cli_" son tal cual
    // los usa el código (Clientes.razor, GPSService.cs, ClienteItemService.cs).
    public class ClienteItem
    {
        public int cli_clientesid { get; set; }
        public string cli_codigo { get; set; } = "";
        public string cli_nombre { get; set; } = "";
        public string cli_direccion { get; set; } = "";
        public string cli_telefono { get; set; } = "";
        public decimal cli_porcentajepercepcionib { get; set; }
        public double cli_latitud { get; set; }
        public double cli_longitud { get; set; }
        public string? cli_CUIT { get; set; }
        // Estado de la cuenta (0 = CF habilitado). Ver VistaProductos.razor / CatalogoProductos.razor.
        public int cli_MCE { get; set; }
        // Estado de transmisión de GPS del último pedido: 1 OK, 2 error, 3 sin transmitir.
        public int cli_reba { get; set; }
        // 0 = CUIT válido (consumidor final habilitado); distinto de 0 = requiere validación.
        public int CUITValido { get; set; }
        public int VendedoresID { get; set; }
        // Calculada en runtime por GPSService (distancia al vendedor, en km).
        public decimal Distancia { get; set; }
        // Días desde la última venta al cliente (alerta > 30 días, ver Clientes.razor).
        public int DiasNoVenta { get; set; }
        // Orden de cercanía calculado por GPSService.OrdenarClientesporDistancia.
        public int Orden { get; set; }
        public bool Visistado { get; set; }
        // Mensaje de alerta mostrado al seleccionar el cliente (Clientes.razor), p.ej.
        // avisos de devolución pendiente. Vacío/"" cuando no hay nada que mostrar.
        public string? MensajeDevolucion { get; set; }
    }

    // Tabla PedidoCabeceraItem (SQLite local). El constructor posicional de 18
    // argumentos y el inicializador con nombres (ver PedidoCabeceraService.cs) exponen
    // el shape completo. Varias fechas/ints no tienen un consumidor de lectura directo
    // en el código actual (solo se escriben) — se modelan igual porque el código
    // realmente los asigna.
    public class PedidoCabeceraItem
    {
        public int PedidoCabeceraID { get; set; }
        public string DeviceID { get; set; } = "";
        public int VendedorID { get; set; }
        public int ClienteID { get; set; }
        public string? ClienteCodigo { get; set; }
        public string? ClienteDireccion { get; set; }
        public DateTime FechaCarga { get; set; }
        public bool Cf { get; set; }
        public string? Division { get; set; }
        public string? Comentario { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string ResultadoTx { get; set; } = "";
        public DateTime? FechaDeTx { get; set; }
        public string? Error { get; set; }
        public bool PedidoCerrado { get; set; }
        public int PedidoTransmitido { get; set; }
        public bool PedidoReactivado { get; set; }
        public int PedidoReactivadoId { get; set; }
        // Se marca [Ignore]: es una lista, sqlite-net no puede mapearla a columna.
        public List<int> PedidosClientesId { get; set; } = new();
        public DateTime? FechaDeEntrega { get; set; }
        public string? KeyPagoID { get; set; }
        public string? PaymentID { get; set; }
        public decimal TotalPedido { get; set; }
        // 0 = pedido de vendedor presencial, 1 = pedido autogestión/e-commerce.
        public int TipoDeClienteDelPedido { get; set; }
        // Timestamp auxiliar visto en el constructor posicional; sin lectura conocida.
        public DateTime FechaUltimaModificacion { get; set; }
        // Se marca [Ignore]: lista de detalle en memoria, no columna de la tabla.
        public List<PedidosDetalleItem> LstProductos { get; set; } = new();

        public PedidoCabeceraItem() { }

        public PedidoCabeceraItem(int pedidoCabeceraID, string deviceID, int vendedorID, int clienteID,
            DateTime fechaCarga, bool cf, string comentario, string division, int pedidoTransmitido,
            int tipoDeClienteDelPedido, string error, DateTime fechaDeEntrega, string resultadoTx,
            DateTime? fechaDeTx, string clienteCodigo, string clienteDireccion,
            DateTime fechaUltimaModificacion, int pedidoReactivadoId)
        {
            PedidoCabeceraID = pedidoCabeceraID;
            DeviceID = deviceID;
            VendedorID = vendedorID;
            ClienteID = clienteID;
            FechaCarga = fechaCarga;
            Cf = cf;
            Comentario = comentario;
            Division = division;
            PedidoTransmitido = pedidoTransmitido;
            TipoDeClienteDelPedido = tipoDeClienteDelPedido;
            Error = error;
            FechaDeEntrega = fechaDeEntrega;
            ResultadoTx = resultadoTx;
            FechaDeTx = fechaDeTx;
            ClienteCodigo = clienteCodigo;
            ClienteDireccion = clienteDireccion;
            FechaUltimaModificacion = fechaUltimaModificacion;
            PedidoReactivadoId = pedidoReactivadoId;
        }
    }

    // Tabla PedidosDetalleItem (SQLite local). Shape según el constructor posicional
    // usado en PedidoCabeceraService.AgregarPedidosProductos y PedidosDetalleItemService.
    public class PedidosDetalleItem
    {
        public int Id { get; set; }
        public int PedidoCabeceraID { get; set; }
        public int ProductoID { get; set; }
        public int CombosID { get; set; }
        public int Cantidad { get; set; }
        public int ListadePrecioID { get; set; }
        public decimal PrecioUnitarioFinal { get; set; }
        public decimal PrecioUnitarioNeto { get; set; }
        // Resultado de transmisión de esta línea ("" / "OK..." / mensaje de error).
        public string? ResultadoTx { get; set; }
        public int GrupoDinamico { get; set; }
        // Nombre real usado por PedidoCabeceraService al clonar un detalle
        // (ver reactivación de pedido: "palabraClave: d.PalabraClave").
        public string? PalabraClave { get; set; }
        public string? NombreProducto { get; set; }
        public string? CodigoProducto { get; set; }
        public bool CF { get; set; }
        public int UnidadesPorBulto { get; set; }
        // Cantidad de combos vendidos en esta línea (distinto de Cantidad de unidades).
        public int Cantdecombos { get; set; }
        public string? ComboCodigo { get; set; }
        public string? ImagenWeb { get; set; }
        // Suma de la línea (Cantidad * PrecioUnitarioFinal * UnidadesPorBulto).
        public decimal TotalLinea { get; set; }

        public PedidosDetalleItem() { }

        public PedidosDetalleItem(int id, int pedidoCabeceraID, int productoID, int combosID, int cantidad,
            int listadePrecioID, decimal precioUnitarioFinal, decimal precioUnitarioNeto, string resultadoTx,
            int grupoDinamico, string palabraClave, string nombreProducto, string codigoProducto, bool cf,
            int unidadesPorBulto, int cantdecombos, string combocodigo)
        {
            Id = id;
            PedidoCabeceraID = pedidoCabeceraID;
            ProductoID = productoID;
            CombosID = combosID;
            Cantidad = cantidad;
            ListadePrecioID = listadePrecioID;
            PrecioUnitarioFinal = precioUnitarioFinal;
            PrecioUnitarioNeto = precioUnitarioNeto;
            ResultadoTx = resultadoTx;
            GrupoDinamico = grupoDinamico;
            PalabraClave = palabraClave;
            NombreProducto = nombreProducto;
            CodigoProducto = codigoProducto;
            CF = cf;
            UnidadesPorBulto = unidadesPorBulto;
            Cantdecombos = cantdecombos;
            ComboCodigo = combocodigo;
            TotalLinea = cantidad * precioUnitarioFinal * (unidadesPorBulto > 0 ? unidadesPorBulto : 1);
        }
    }

    // Tabla AltaDeClientes (flujo legacy SolicitudAlta.razor / ClienteAltaService.cs).
    // Shape documentado explícitamente en analisis-end-to-end.md sección 1.4:
    // el front usa "NombreApellido" pero la entidad real usa "NombreYApellido".
    public class AltaDeClientesItem
    {
        public int Id { get; set; }
        public string NombreYApellido { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime FechaAlta { get; set; }
        public int Estatus { get; set; }
    }
}
