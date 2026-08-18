// Ver Entidades/ClienteYPedidos.cs para la nota general sobre este modelado local.
// Filas "delta" que llegan del API de sincronización (ProductosItem/StockYPreciosProductos
// y ProductosItem/ProductosActualItems), consumidas en ProductoListaService.cs.
namespace EntidadesAppVendedores
{
    // Ver ProductoListaService.UpdateStockEnProductoBloque: "stk" es nullable porque
    // se compara contra null antes de usarlo ("prod.stk != null").
    public class ProductoStockItem
    {
        public int id { get; set; }
        public decimal? stk { get; set; }
    }

    // Ver ProductoListaService.UpdatePreciosEnProductoBloque: cada campo corto (PFxxx /
    // PNxxx) mapea 1 a 1 a un PrecioUnitarioFinalXxx(Neto) de ProductoListaItem.
    public class ProductoPrecioItems
    {
        public int ProdID { get; set; }
        public decimal PFAut { get; set; }
        public decimal PFEsp { get; set; }
        public decimal PFAl { get; set; }
        public decimal PFFKi { get; set; }
        public decimal PFSal { get; set; }
        public decimal PFEcom { get; set; }
        public decimal PNAut { get; set; }
        public decimal PNEsp { get; set; }
        public decimal PNAlm { get; set; }
        public decimal PNKio { get; set; }
        public decimal PNSal { get; set; }
        public decimal PNEcom { get; set; }
    }

    // Ver ProductoListaService.UpdateProductoBloquePorItems: cada campo corto mapea a
    // una propiedad de ProductoListaItem relacionada con badges de UI (nuevo ingreso,
    // precio modificado, comisión diferencial).
    public class ProductosActualizarItems
    {
        public int ProdID { get; set; }
        public int DiasStkDisp { get; set; }
        public int DiasPrecMod { get; set; }
        public bool NuevaIncor { get; set; }
        public bool IngreRec { get; set; }
        // Booleano, no monto: se copia directo a ProductoListaItem.ComisionDiferencial
        // que la UI consume como flag (ver VistaProductos.razor: .All(p => p.ComisionDiferencial)).
        public bool ComisDif { get; set; }
    }
}
