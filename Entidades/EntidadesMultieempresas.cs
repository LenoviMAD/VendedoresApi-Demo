// Modelado local mínimo que reemplaza a la librería externa EntidadesMultieempresas
// (no disponible en este entorno). Contiene únicamente las clases y propiedades
// que el código de SincroApp-Demo usa realmente. Ver Guia-Archivos-CSV-JSON.md
// para el formato de cada archivo que estas clases representan.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EntidadesMultieempresas
{
    public class ClientesMultiEmpresaItem
    {
        public int ClientesID { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Localidad { get; set; } = "";
        public string Telefono { get; set; } = "";
        public int DiaDeVenta { get; set; }
        public decimal PorcentajePercepcionIB { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public int ListasPreciosID { get; set; }
        public int TiposDocumentosID { get; set; }
        public string? NumeroDocumento { get; set; }
        public bool Baja { get; set; }
        public int EmpresaID { get; set; }
    }

    public class StockMultiempresaItem
    {
        public int ProductosID { get; set; }
        public string? CodigoProducto { get; set; }
        // String tal cual la envía el ERP; el API la parsea a decimal (ver guía, sección CSV Stock).
        public string StockUnidades { get; set; } = "";
        public int EmpresaID { get; set; }
    }

    public class ProductosMultiEmpresaItem
    {
        public int ProductosMultiEmpresaID { get; set; }
        public int? ProductosID { get; set; }
        public int? ProductosIDPadre { get; set; }
        public string? CodigoDeProducto { get; set; }
        public string? Nombre { get; set; }
        public int StockEnUnidades { get; set; }
        public int CantidadMultiplo { get; set; }
        public bool DesactivadoParaLaVenta { get; set; }
        public int MarcasProductosID { get; set; }
        public int FamiliaProductosID { get; set; }
        public bool StockPropio { get; set; }
        public List<int> CategoriasComercialesIDs { get; set; } = new();
        public int UnidadesPorBulto { get; set; }
        public decimal PrecioProducto { get; set; }
        public int ImpuestosID { get; set; }
        public bool Baja { get; set; }
        public int CategoriasProductosID { get; set; }
        public int SubCategoriasProductosID { get; set; }
        public int EmpresaID { get; set; }
        public string? UrlImagenWeb { get; set; }
    }

    // Reparto (ruta de entrega) subido como JSON. Estructura exacta documentada en
    // SincoApp_Guia_Archivos_CSV_v2.html, sección 7 — "FelterosID" (con F, no "Fleteros")
    // es el nombre real del campo, no un typo de este modelado.
    public class RepartosDetalleMultiEmpresaExcelItem
    {
        public int NroReparto { get; set; }
        public int FelterosID { get; set; }
        public string? Comentario { get; set; }
        public string? COTSIAP { get; set; }
        public string? ClaveFletero { get; set; }
        public List<VentaMultiEmpresaItem> VentasMultiEmpresa { get; set; } = new();
    }

    public class VentaMultiEmpresaItem
    {
        public DateTime Fecha { get; set; }
        public int ClientesID { get; set; }
        public int NroComprobante { get; set; }
        public decimal Total { get; set; }
        public int RepartosDetalleMultiEmpresaID { get; set; }
        public int NroReparto { get; set; }
        public List<VentaProductoDetalleItem> VentasProductosDetalle { get; set; } = new();
    }

    public class VentaProductoDetalleItem
    {
        public int ProductoID { get; set; }
        public int CodigoProducto { get; set; }
        public int CantidadUnidades { get; set; }
        public decimal Total { get; set; }
    }

    // Catálogos de referencia (maestros.json) — estructura exacta documentada
    // en Guia-Archivos-CSV-JSON.md, sección "JSON Maestros".
    public class MaestrosSyncItem
    {
        public int EmpresaID { get; set; }
        public List<ImpuestoItem> Impuestos { get; set; } = new();
        public List<MarcaProductoItem> MarcasProductos { get; set; } = new();
        public List<FamiliaProductoItem> FamiliasProductos { get; set; } = new();
        public List<CategoriaProductoItem> Categorias { get; set; } = new();
        public List<SubCategoriaProductoItem> SubCategorias { get; set; } = new();
        public List<CategoriaComercialItem> CategoriasComerciales { get; set; } = new();
        public List<ListaPrecioItem> ListasPrecios { get; set; } = new();
        public List<TipoDocumentoItem> TiposDocumentos { get; set; } = new();
    }

    public class ImpuestoItem
    {
        public int ImpuestosID { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Porcentaje { get; set; }
    }

    public class MarcaProductoItem
    {
        public int MarcasProductosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class FamiliaProductoItem
    {
        public int FamiliaProductosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class CategoriaProductoItem
    {
        public int CategoriasProductosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class SubCategoriaProductoItem
    {
        public int SubCategoriasProductosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class CategoriaComercialItem
    {
        public int CategoriasComercialesID { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class ListaPrecioItem
    {
        public int ListasPreciosID { get; set; }
        public string Nombre { get; set; } = "";
        public bool Baja { get; set; }
        public decimal MontoMinimo { get; set; }
        public decimal PorcentajeMarcup { get; set; }
    }

    public class TipoDocumentoItem
    {
        public int TiposDocumentosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    // Fila "aplanada" de un pedido (un producto por fila) para exportar a CSV
    // al descargar pedidos pendientes desde el API.
    public class PedidosJsonMultiempresaExcelItem
    {
        public int VendedorID { get; set; }
        public int ClienteID { get; set; }
        public string ClienteCodigo { get; set; } = "";
        public string CodigoProducto { get; set; } = "";
        public int ProductoID { get; set; }
        public decimal Unidades { get; set; }
        public int ListaDePrecioID { get; set; }
        public decimal PrecioUnitarioFinal { get; set; }
        public decimal PrecioUnitarioNeto { get; set; }
        public decimal TotalProducto { get; set; }
        public decimal TotalPedido { get; set; }
    }

    // Pedido tal cual llega en JSON desde el API de pedidos pendientes (sin aplanar).
    public class PedidoJsonMultiempresaItem
    {
        public int VendedorID { get; set; }
        public int ClienteID { get; set; }
        public string? ClienteCodigo { get; set; }
        public decimal TotalPedido { get; set; }
        public List<PedidoProductoItem> LstProductos { get; set; } = new();
    }

    public class PedidoProductoItem
    {
        public string? CodigoProducto { get; set; }
        public int ProductoID { get; set; }
        public decimal Unidades { get; set; }
        public decimal Cantidad { get; set; }
        public int ListaDePrecioID { get; set; }
        public decimal PrecioUnitarioFinal { get; set; }
        public decimal PrecioUnitarioNeto { get; set; }
        public decimal TotalProducto { get; set; }
    }

    // Fila cruda tal como la devuelve /pedidos/pendientes-wrapper/{empresaId}: cada
    // pedido pendiente viaja como un wrapper con su JSON de detalle sin parsear.
    public class PedidoJsonMultiempresaDb
    {
        public int PedidosJsonMultiempresaID { get; set; }
        public string? JsonPedido { get; set; }
        public DateTime HoraGuardada { get; set; }
    }

    // Helper legacy para armar filas de pedido a partir de su JSON de detalle.
    // Equivalente a Form1.BuildRowsFromJson, mantenido aparte porque así lo
    // consume el flujo de exportación por ítems (ExportarCsvDesdeItemsAsync).
    public class PedidosJsonMultiempresa
    {
        private static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static List<PedidosJsonMultiempresaExcelItem> JsonToRows(string jsonPedido)
        {
            var pedido = JsonSerializer.Deserialize<PedidoJsonMultiempresaItem>(jsonPedido, Opts);
            var rows = new List<PedidosJsonMultiempresaExcelItem>();
            if (pedido == null) return rows;

            foreach (var pr in pedido.LstProductos)
            {
                var unidades = pr.Unidades != 0 ? pr.Unidades : pr.Cantidad;
                rows.Add(new PedidosJsonMultiempresaExcelItem
                {
                    VendedorID = pedido.VendedorID,
                    ClienteID = pedido.ClienteID,
                    ClienteCodigo = pedido.ClienteCodigo ?? "",
                    CodigoProducto = pr.CodigoProducto ?? "",
                    ProductoID = pr.ProductoID,
                    Unidades = unidades,
                    ListaDePrecioID = pr.ListaDePrecioID,
                    PrecioUnitarioFinal = pr.PrecioUnitarioFinal,
                    PrecioUnitarioNeto = pr.PrecioUnitarioNeto,
                    TotalProducto = pr.TotalProducto,
                    TotalPedido = pedido.TotalPedido
                });
            }
            return rows;
        }

        // No hay endpoint documentado para este flujo (no referenciado desde ningún
        // botón ni caller); se deja sin resultados en vez de inventar un contrato de API.
        public List<PedidosJsonMultiempresaExcelItem> GetEmpresasExternaItems(int empresaId)
            => new();
    }
}
