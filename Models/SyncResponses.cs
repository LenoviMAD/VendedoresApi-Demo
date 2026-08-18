using System.Collections.Generic;

namespace VendedoresApi.Models
{
    // Wrappers de respuesta "unificada" que consume FuncionesSincronizar.cs del lado de la
    // app (Services/FuncionesSincronizar.cs líneas ~236-423). Shape copiado 1 a 1 de las
    // clases privadas DashboardEcomResponse / StockYPreciosResponse /
    // BloqueadosYSubcategoriasResponse declaradas al final de ese mismo archivo (fuente de
    // verdad del contrato JSON) — no son tablas, solo el sobre de la respuesta HTTP.

    public class DashboardEcomResponse
    {
        public List<EntidadesAppVendedores.LomasvendidoItem> RecomendadosTop { get; set; } = new();
        public List<int> NuevosIngresos { get; set; } = new();
        public List<EntidadesAppVendedores.LoQueMasTeGustaItem> MasTeGusta { get; set; } = new();
        public List<EntidadesAppVendedores.CombosXClientesItem> CombosXCliente { get; set; } = new();
        public IEnumerable<EntidadesAppVendedores.CategoriaItem> Categorias { get; set; } = new List<EntidadesAppVendedores.CategoriaItem>();
        public IEnumerable<EntidadesAppVendedores.SubCategoriasApiEcomItem> SubCategorias { get; set; } = new List<EntidadesAppVendedores.SubCategoriasApiEcomItem>();
        public IEnumerable<EntidadesAppVendedores.ClienteItem> Clientes { get; set; } = new List<EntidadesAppVendedores.ClienteItem>();
    }

    public class StockYPreciosResponse
    {
        public List<EntidadesAppVendedores.ProductoStockItem> Stock { get; set; } = new();
        public List<EntidadesAppVendedores.ProductoPrecioItems> Precios { get; set; } = new();
    }

    public class BloqueadosYSubcategoriasResponse
    {
        public List<EntidadesAppVendedores.ProductosBloqueadosxCliente> Bloqueados { get; set; } = new();
        public List<EntidadesAppVendedores.ProductosSubCategoriasItem> SubCategorias { get; set; } = new();
    }
}
