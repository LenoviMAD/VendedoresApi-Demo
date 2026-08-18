// Ver Entidades/ClienteYPedidos.cs para la nota general sobre este modelado local.
// Shapes relevados de Services/CombosItemService.cs (constructores posicionales
// completos), Services/FuncionesSincronizar.cs y Components/Pages/VistaProductos*.razor.
using System;
using System.Collections.Generic;

namespace EntidadesAppVendedores
{
    // Tabla CombosHeadItem (SQLite local). El constructor posicional de 23 argumentos
    // en CombosItemService.Sincronizar/SincronizarActualizados fija el shape.
    public class CombosHeadItem
    {
        public int CombosID { get; set; }
        public string? NombreCombo { get; set; }
        public string? Codigo { get; set; }
        public int CantidadMaximaPorCliente { get; set; }
        public int Cantidad { get; set; }
        public int Grupo1 { get; set; }
        public int Grupo2 { get; set; }
        public int Grupo3 { get; set; }
        public int Grupo4 { get; set; }
        public int Grupo5 { get; set; }
        public int Grupo6 { get; set; }
        public int Grupo7 { get; set; }
        public int CantidadDinamica { get; set; }
        // Cantidad sin cargo (bonificada) del combo.
        public int CantSCargo { get; set; }
        public string? Rubro { get; set; }
        public int CantidadMaximaPorFactura { get; set; }
        public string? ComboIntro { get; set; }
        public string ImagenDataBase { get; set; } = "";
        // Producto de referencia usado para heredar imagen/color (ver
        // CombosItemService.Sincronizar: oProductoListaService.GetByID(item.ProductoIDdeReferencia)).
        public int ProductoIDdeReferencia { get; set; }
        public int Color { get; set; }
        public decimal ImporteProductosFueraDeCombo { get; set; }
        // Segundo importe de arrastre pasado en la misma posición que
        // ImporteProductosFueraDeCombo en el constructor original (duplicado tal cual
        // en el código fuente, no es un error de este modelado).
        public decimal MontoMinimoArrastre { get; set; }
        // Usado para ordenar combos por venta (columna consultada directo por nombre
        // "TotalVenta" en ProductoListaService.GetAll: lsCombosItem.OrderByDescending(c => c.TotalVenta)).
        public decimal TotalVenta { get; set; }
        // Flag de "sumar combos en arrastre" (CombosItemService.GetSumarCombosEnArrastre
        // lo compara contra 1). No se setea desde ningún sync visible en el código —
        // queda en su default (0) salvo que se actualice manualmente en el futuro.
        public int SumarCombosEnArrastre { get; set; }

        // Cantidad requerida por grupo dinámico (1-7), indexada por número de grupo.
        // Se usa como colección (sin paréntesis) en CatalogoProductos.razor:
        // "var cantDinamicaGrp = comboHeadItem.GetCantidadesGrupo; ... cantDinamicaGrp[proIT.NroDeGrupoCombo]".
        public Dictionary<int, int> GetCantidadesGrupo => new()
        {
            [1] = Grupo1,
            [2] = Grupo2,
            [3] = Grupo3,
            [4] = Grupo4,
            [5] = Grupo5,
            [6] = Grupo6,
            [7] = Grupo7
        };

        public CombosHeadItem() { }

        public CombosHeadItem(int combosID, string nombreCombo, string codigo, int cantidadMaximaPorCliente,
            int cantidad, int grupo1, int grupo2, int grupo3, int grupo4, int grupo5, int grupo6, int grupo7,
            int cantidadDinamica, int cantSCargo, string rubro, int cantidadMaximaPorFactura, string comboIntro,
            string imagenDataBase, int productoIDdeReferencia, int color, decimal importeProductosFueraDeCombo,
            decimal montoMinimoArrastre, decimal totalVenta)
        {
            CombosID = combosID;
            NombreCombo = nombreCombo;
            Codigo = codigo;
            CantidadMaximaPorCliente = cantidadMaximaPorCliente;
            Cantidad = cantidad;
            Grupo1 = grupo1;
            Grupo2 = grupo2;
            Grupo3 = grupo3;
            Grupo4 = grupo4;
            Grupo5 = grupo5;
            Grupo6 = grupo6;
            Grupo7 = grupo7;
            CantidadDinamica = cantidadDinamica;
            CantSCargo = cantSCargo;
            Rubro = rubro;
            CantidadMaximaPorFactura = cantidadMaximaPorFactura;
            ComboIntro = comboIntro;
            ImagenDataBase = imagenDataBase ?? "";
            ProductoIDdeReferencia = productoIDdeReferencia;
            Color = color;
            ImporteProductosFueraDeCombo = importeProductosFueraDeCombo;
            MontoMinimoArrastre = montoMinimoArrastre;
            TotalVenta = totalVenta;
        }
    }

    // Tabla CombosProductosItem (SQLite local). ComboId/ComboProductoID/ComboTipo se
    // usan tal cual (con esa grafía) en SQL crudo y en comparaciones directas, así que
    // esos tres nombres son obligatorios, no una elección de este modelado.
    public class CombosProductosItem
    {
        public int ComboId { get; set; }
        public int ComboProductoID { get; set; }
        public int Cantidad { get; set; }
        public int CantidadConCargo { get; set; }
        public int ComboTipo { get; set; }
        // Guardado como texto tal cual llega de la API (se parsea recién al usarlo
        // con Convert.ToDecimal en CatalogoProductos.razor / SubCategorias.razor).
        public string? Descuento1 { get; set; }
        public string? Descuento2 { get; set; }
        public int DpcID { get; set; }
        public int NrGrupoDinamico { get; set; }
        public string? TextoPrecioCalculado1 { get; set; }
        public string? TextoPrecioCalculado2 { get; set; }
        public string? TextoPrecioCalculado3 { get; set; }
        public string? TextoPrecioCalculado4 { get; set; }
        public string? TextoPrecioCalculado5 { get; set; }
        public string? TextoPrecioCalculado6 { get; set; }
        public string? TextoPrecioCalculado7 { get; set; }
        public string? TextoPrecioCalculado8 { get; set; }
        public string? PrecioNoCalculado { get; set; }

        // Se completa en memoria (CombosItemService.GetProductosByID); no es columna.
        public ProductoListaItem? ProductoListaItem { get; set; }

        public CombosProductosItem() { }

        public CombosProductosItem(int comboId, int comboProductoID, int cantidad, int cantidadConCargo,
            int comboTipo, string descuento1, string descuento2, int dpcID, int nrGrupoDinamico,
            string textoPrecioCalculado1, string textoPrecioCalculado2, string textoPrecioCalculado3,
            string textoPrecioCalculado4, string textoPrecioCalculado5, string textoPrecioCalculado6,
            string textoPrecioCalculado7, string textoPrecioCalculado8, string precioNoCalculado)
        {
            ComboId = comboId;
            ComboProductoID = comboProductoID;
            Cantidad = cantidad;
            CantidadConCargo = cantidadConCargo;
            ComboTipo = comboTipo;
            Descuento1 = descuento1;
            Descuento2 = descuento2;
            DpcID = dpcID;
            NrGrupoDinamico = nrGrupoDinamico;
            TextoPrecioCalculado1 = textoPrecioCalculado1;
            TextoPrecioCalculado2 = textoPrecioCalculado2;
            TextoPrecioCalculado3 = textoPrecioCalculado3;
            TextoPrecioCalculado4 = textoPrecioCalculado4;
            TextoPrecioCalculado5 = textoPrecioCalculado5;
            TextoPrecioCalculado6 = textoPrecioCalculado6;
            TextoPrecioCalculado7 = textoPrecioCalculado7;
            TextoPrecioCalculado8 = textoPrecioCalculado8;
            PrecioNoCalculado = precioNoCalculado;
        }
    }

    // Tabla CombosXClientesItem (SQLite local) — combos habilitados por cliente.
    public class CombosXClientesItem
    {
        public int ClientesID { get; set; }
        public int CombosID { get; set; }
        public int Orden { get; set; }
    }

    // Resultado de la consulta cruda en CombosItemService.GetCombosDeUnProducto
    // (join CombosHeadItem + CombosProductosItem, columnas seleccionadas explícitas).
    public class CombosEnUnProductoItem
    {
        public int CombosID { get; set; }
        public string? NombreCombo { get; set; }
        public string? Codigo { get; set; }
        public decimal TotalVenta { get; set; }
    }

    // Fila cruda "comb_*" tal como llega desde el API de combos
    // (combosPorFechaMultiEmpresa / combosPorFechaClienteMiltiEmpresa). Ver
    // CombosItemService.Sincronizar/SincronizarActualizados para el uso completo de
    // cada campo (agrupado por comb_combosid y usado para armar CombosHeadItem /
    // CombosProductosItem).
    public class CombosItemJson
    {
        public int comb_combosid { get; set; }
        public int comb_productosid { get; set; }
        public int comb_cantidad { get; set; }
        public int comb_tipo { get; set; }
        public string? comb_descuento1 { get; set; }
        public string? comb_descuento2 { get; set; }
        public int comb_dpcID { get; set; }
        public int comb_NrGpDin { get; set; }
        public string? TextoPrecioCalculado1 { get; set; }
        public string? TextoPrecioCalculado2 { get; set; }
        public string? TextoPrecioCalculado3 { get; set; }
        public string? TextoPrecioCalculado4 { get; set; }
        public string? TextoPrecioCalculado5 { get; set; }
        public string? TextoPrecioCalculado6 { get; set; }
        public string? TextoPrecioCalculado7 { get; set; }
        public string? TextoPrecioCalculado8 { get; set; }
        public string? PrecioNoCalculado { get; set; }
        public string? comb_nombre { get; set; }
        public string? comb_codigo { get; set; }
        public int comb_cantmax { get; set; }
        public int comb_grp1 { get; set; }
        public int comb_grp2 { get; set; }
        public int comb_grp3 { get; set; }
        public int comb_grp4 { get; set; }
        public int comb_grp5 { get; set; }
        public int comb_grp6 { get; set; }
        public int comb_grp7 { get; set; }
        public int comb_CantDinam { get; set; }
        public int comb_CantSCargo { get; set; }
        public string? comb_RubroComb { get; set; }
        public int comb_CantMaxXFact { get; set; }
        public string? comb_Intro { get; set; }
        public decimal comb_ImporteProdFComb { get; set; }
        public decimal TotalVtaParaOrden { get; set; }
    }
}
