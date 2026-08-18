// Ver Entidades/ClienteYPedidos.cs para la nota general sobre este modelado local.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EntidadesAppVendedores
{
    // Tabla ProductoListaItem (SQLite local) — la entidad más grande del dominio.
    // Propiedades relevadas de: Services/ProductoListaService.cs, Services/DABSqlLite.cs
    // (índices), Services/CombosItemService.cs, Components/Pages/VistaProductos.razor,
    // Shared/UtilCompartidas.cs y SYSTEM_DESIGN.md sección 5.
    public class ProductoListaItem
    {
        public int ProductosID { get; set; }
        public int ProductosIDPadre { get; set; }
        public int ProductosIDMaestro { get; set; }
        public int Orden { get; set; }
        public string CodigoDeProducto { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Marca { get; set; }
        public int Color { get; set; }
        public string? ImagenWeb { get; set; }
        public string ImagenDataBase { get; set; } = "";
        public string TextoParaBusqueda { get; set; } = "";
        public int StockEnUnidades { get; set; }
        public int UnidadesPorBulto { get; set; }
        public int CantidadMultiplo { get; set; }
        public int DiasDeStockDisponible { get; set; }

        // Precios finales por lista de precio (perfil de venta).
        public decimal PrecioUnitarioFinalAutoservicio { get; set; }
        public decimal PrecioUnitarioFinalEspecial { get; set; }
        public decimal PrecioUnitarioFinalAlamcene { get; set; }
        public decimal PrecioUnitarioFinalKiosko { get; set; }
        public decimal PrecioUnitarioSalon { get; set; }
        public decimal PrecioUnitarioEcom { get; set; }
        public decimal PrecioUnitarioFinalAutoservicioNeto { get; set; }
        public decimal PrecioUnitarioFinalEspecialNeto { get; set; }
        public decimal PrecioUnitarioFinalAlamceneNeto { get; set; }
        public decimal PrecioUnitarioFinalKioskoNeto { get; set; }
        public decimal PrecioUnitarioSalonNeto { get; set; }
        public decimal PrecioUnitarioEcomNeto { get; set; }

        // Estado de carrito / combos (uso en memoria, no todo tiene sentido persistido
        // pero sqlite-net lo soporta igual por ser tipos simples).
        public int ComboID { get; set; }
        public bool ConCargo { get; set; }
        public bool DesactivadoParaLaVenta { get; set; }
        // Múltiplo mínimo de venta (usado en Dictionary<int,int> de cantidades locales
        // del carrito, ver ModalCarrito.razor: por eso es int y no decimal).
        public int CantidadEnCarrito { get; set; }
        public int ListaDePreciosUsada { get; set; }
        public bool CF { get; set; }
        public int NroDeGrupoCombo { get; set; }
        public int CantidadMaximaPorGrupo { get; set; }
        public string? NombreDelCombo { get; set; }
        public string? ComboCodigo { get; set; }
        public decimal PorcentajDescuento1 { get; set; }
        public decimal PorcentajDescuento2 { get; set; }
        public decimal TotalLinea { get; set; }
        public decimal TotalLineaNeto { get; set; }
        public decimal TotalVenta { get; set; }
        // Unidades necesarias para completar un bulto (venta por bulto cerrado).
        public int UnidadesParaCompetarUnBulto { get; set; }
        // Orden usado por la vista de subcategoría (VistaProductos.razor: OrderBy(c => c.OrdenXVista)).
        public int OrdenXVista { get; set; }
        // Cliente al que quedó asociado este producto en la sección "lo que te gusta"
        // (ver Shared/UtilCompartidas.cs, GetLoMasPedidoService).
        public int ClientesIDAsociado { get; set; }

        // Flags de badges de UI (ver SYSTEM_DESIGN.md 4.5 y analisis-nuevos-ingresos-precio-modificado.md).
        public bool NuevaIncorporacoin { get; set; }
        public bool IngresosRecientes { get; set; }
        public int DiasDePrecioModificado { get; set; }
        // Comisión diferencial: flag booleano (VistaProductos.razor la usa en un
        // predicado .All(p => p.ComisionDiferencial), no como monto).
        public bool ComisionDiferencial { get; set; }

        // Textos de precio de combo por lista (1-8), copiados desde CombosProductosItem
        // por CombosItemService.GetTextoPrecioCalculadoEnCombo. "N/Calc" = no calculado.
        public string? TextoPrecioCalculado1 { get; set; }
        public string? TextoPrecioCalculado2 { get; set; }
        public string? TextoPrecioCalculado3 { get; set; }
        public string? TextoPrecioCalculado4 { get; set; }
        public string? TextoPrecioCalculado5 { get; set; }
        public string? TextoPrecioCalculado6 { get; set; }
        public string? TextoPrecioCalculado7 { get; set; }
        public string? TextoPrecioCalculado8 { get; set; }
        public string? PrecioNoCalculado { get; set; }

        // Marca de "visto" en el último snapshot de sincronización (borra lo no visto).
        public long LastSeenSyncId { get; set; }

        // Combos asociados a este producto (no es columna: se llena en memoria).
        public List<CombosEnUnProductoItem> LstCombo { get; set; } = new();
        public List<CombosEnUnProductoItem> LstComboCliente { get; set; } = new();

        // Devuelve el precio final según la lista de precios activa. El mapeo de IDs
        // documentado en SYSTEM_DESIGN.md (1 Distribución, 3 Autoservicio, 4 Salón,
        // 7 E-commerce) cubre 4 de las 6 listas; 2 y 5 (Almacén/Kiosko) no están
        // documentados y se completan con un orden razonable.
        public decimal PrecioUnitarioFinal(int listaDePrecio) => listaDePrecio switch
        {
            1 => PrecioUnitarioFinalEspecial,
            2 => PrecioUnitarioFinalAlamcene,
            3 => PrecioUnitarioFinalAutoservicio,
            4 => PrecioUnitarioSalon,
            5 => PrecioUnitarioFinalKiosko,
            7 => PrecioUnitarioEcom,
            _ => PrecioUnitarioFinalEspecial
        };

        public decimal PrecioUnitarioNeto(int listaDePrecio) => listaDePrecio switch
        {
            1 => PrecioUnitarioFinalEspecialNeto,
            2 => PrecioUnitarioFinalAlamceneNeto,
            3 => PrecioUnitarioFinalAutoservicioNeto,
            4 => PrecioUnitarioSalonNeto,
            5 => PrecioUnitarioFinalKioskoNeto,
            7 => PrecioUnitarioEcomNeto,
            _ => PrecioUnitarioFinalEspecialNeto
        };

        public string TextoCombo(int listaDePrecio) => listaDePrecio switch
        {
            1 => TextoPrecioCalculado1 ?? "",
            2 => TextoPrecioCalculado2 ?? "",
            3 => TextoPrecioCalculado3 ?? "",
            4 => TextoPrecioCalculado4 ?? "",
            5 => TextoPrecioCalculado5 ?? "",
            6 => TextoPrecioCalculado6 ?? "",
            7 => TextoPrecioCalculado7 ?? "",
            8 => TextoPrecioCalculado8 ?? "",
            _ => TextoPrecioCalculado1 ?? ""
        };

        public decimal PrecioFinalCombo(int listaDePrecio)
        {
            decimal.TryParse(TextoCombo(listaDePrecio), out var precio);
            return precio;
        }
    }

    // Wrapper de List<ProductoListaItem> con helpers propios usados por las páginas
    // de catálogo (CatalogoProductos.razor, VistaProductos.razor, SubCategorias.razor,
    // Index.razor.cs, PedidoCabeceraService.cs). No hay un contrato de API documentado
    // para estos helpers (HayCombos/GetLoMasVendidos): se implementan con la lógica
    // mínima razonable a partir de los datos ya cargados en la propia lista.
    public class ProductoListaItems : List<ProductoListaItem>
    {
        public bool HayCombos(ProductoListaItem producto) =>
            producto?.LstCombo != null && producto.LstCombo.Count > 0;

        // Combos asociados al producto (buscado en esta misma lista por ProductosID,
        // que es donde ProductoListaService.GetAll() ya deja cargado LstCombo).
        public List<CombosEnUnProductoItem> GetProductoListaCombos(ProductoListaItem productoListaItem)
        {
            var match = this.FirstOrDefault(p => p.ProductosID == productoListaItem.ProductosID) ?? productoListaItem;
            return match?.LstCombo ?? new List<CombosEnUnProductoItem>();
        }

        public List<ProductoListaItem> GetLoMasVendidos(List<LomasvendidoItem> lsLomasVendido, int color)
        {
            // LomasvendidoItem no tiene un shape documentado (nunca se le lee ninguna
            // propiedad en el código real, solo se pasa entre localStorage y esta
            // función), así que no hay forma de filtrar por ID sin inventar un
            // contrato de API. Se devuelve la lista propia filtrada por color como
            // fallback razonable, igual que hacía GetNuevosIngresosService.
            var items = color > 0 ? this.Where(p => p.Color == color) : this.AsEnumerable();
            return items.ToList();
        }
    }

    // maestros.json / GetCatgorias(JS) — catálogo de categorías (localStorage).
    public class CategoriaItem
    {
        public int categoriaID { get; set; }
        public string categoria { get; set; } = "";
    }

    // GetSubCatgorias(JS) — subcategorías del catálogo e-commerce (localStorage).
    public class SubCategoriasApiEcomItem
    {
        public int subCategoriasEcomItemID { get; set; }
        public string subCategoria { get; set; } = "";
        public string? imagen { get; set; }
        // Lista de IDs de categoría a las que pertenece esta subcategoría (una
        // subcategoría puede colgar de más de una categoría — ver SubCategorias.razor:
        // "foreach (var cate in sub.categoriasID)").
        public List<int> categoriasID { get; set; } = new();
    }

    // Tabla ProductosSubCategoriasItem (SQLite local). Nota: "SubCategiruasID" (con
    // esa grafía, no "SubCategoriasID") es el nombre real de la columna — se usa tal
    // cual en la consulta cruda de ProductosSubCategoriasService.GetProductosIDxSubcategoria.
    public class ProductosSubCategoriasItem
    {
        public int ProductosID { get; set; }
        public int SubCategiruasID { get; set; }
    }

    // Tabla ProductosBloqueadosxCliente (SQLite local).
    public class ProductosBloqueadosxCliente
    {
        public int ClientesID { get; set; }
        public int ProductosID { get; set; }
    }

    // "Lo más vendido" (dashboard / getrecomendadosTop). Nunca se le lee ninguna
    // propiedad en el código real (solo viaja entre la API, localStorage y
    // ProductoListaItems.GetLoMasVendidos), así que se deja sin campos propios más
    // allá del identificador de producto, mínimo razonable para el nombre del tipo.
    public class LomasvendidoItem
    {
        public int ProductosID { get; set; }
    }

    // Tabla LoQueMasTeGustaItem (SQLite local) — histórico de compras del cliente.
    public class LoQueMasTeGustaItem
    {
        public int ClienteID { get; set; }
        public int ProductoID { get; set; }
        public int Orden { get; set; }
    }

    // Referenciado por interfaces (IListasdePrecioItemService / IListadePrecioItemService
    // en DABSqlLite.cs) que no tienen ninguna clase implementadora ni caller real —
    // código muerto. Se modela mínimamente para que las interfaces compilen.
    public class ListadePrecioItem
    {
        public int ListasPreciosID { get; set; }
        public string Nombre { get; set; } = "";
    }

    // Referenciado solo por un método stub sin caller real
    // (ProductoListaService.Add(ProductoItemJson) lanza NotImplementedException).
    public class ProductoItemJson
    {
        public int ProductosID { get; set; }
    }
}
