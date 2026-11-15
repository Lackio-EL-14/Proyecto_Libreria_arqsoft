// =========================================================
// MS Productos · datos semilla (US-111). Idempotente.
// GUID fijos de docs/contratos/datos-semilla.md.
// $setOnInsert: si el documento ya existe no se modifica.
// =========================================================

const base = db.getSiblingDB('libreria_productos');
const ADMIN = 'a0000000-0000-0000-0000-000000000001';
const fechaSemilla = ISODate('2026-11-01T12:00:00Z');

function sembrar(coleccion, documento) {
  base[coleccion].updateOne(
    { publicId: documento.publicId },
    { $setOnInsert: documento },
    { upsert: true }
  );
}

function auditoria() {
  return {
    fechaCreacion: fechaSemilla,
    usuarioCreacionId: ADMIN,
    fechaModificacion: null,
    usuarioModificacionId: null
  };
}

// ---------------------------------------------------------
// Categorías
// ---------------------------------------------------------
const categorias = [
  { publicId: 'ca000000-0000-0000-0000-000000000001', codigo: 'CAT-001', nombre: 'Material escolar',
    descripcion: 'Cuadernos, lápices y útiles en general', ubicacion: 'Estante principal' },
  { publicId: 'ca000000-0000-0000-0000-000000000002', codigo: 'CAT-002', nombre: 'Libros de texto',
    descripcion: 'Textos de secundaria y nivel superior', ubicacion: 'Bodega' },
  { publicId: 'ca000000-0000-0000-0000-000000000003', codigo: 'CAT-003', nombre: 'Arte y manualidades',
    descripcion: 'Pinturas, pinceles y acrílicos', ubicacion: 'Vitrina' }
];
for (const c of categorias) {
  sembrar('categorias', { ...c, estado: true, ...auditoria() });
}

// El contador genera el siguiente Código (CAT-004 en adelante).
// $max no retrocede si ya se crearon categorías nuevas.
base.contadores.updateOne({ _id: 'categoria' }, { $max: { valor: 3 } }, { upsert: true });

// ---------------------------------------------------------
// Marcas
// ---------------------------------------------------------
const marcas = [
  { publicId: 'b0000000-0000-0000-0000-000000000001', nombre: 'Faber-Castell',
    descripcion: 'Útiles escolares', paisOrigen: 'Alemania', sitioWeb: 'https://www.faber-castell.com' },
  { publicId: 'b0000000-0000-0000-0000-000000000002', nombre: 'Editorial Santillana',
    descripcion: 'Textos educativos', paisOrigen: 'España', sitioWeb: 'https://www.santillana.com' },
  { publicId: 'b0000000-0000-0000-0000-000000000003', nombre: 'Acrilex',
    descripcion: 'Pinturas y productos de arte', paisOrigen: 'Brasil', sitioWeb: 'https://acrilex.com.br' }
];
for (const m of marcas) {
  sembrar('marcas', { ...m, estado: true, ...auditoria() });
}

// ---------------------------------------------------------
// Productos (categoría y marca embebidas como copia; histórico embebido)
// ---------------------------------------------------------
const materialEscolar = { publicId: categorias[0].publicId, nombre: categorias[0].nombre };
const librosDeTexto = { publicId: categorias[1].publicId, nombre: categorias[1].nombre };
const faber = { publicId: marcas[0].publicId, nombre: marcas[0].nombre };
const santillana = { publicId: marcas[1].publicId, nombre: marcas[1].nombre };

const productos = [
  { publicId: 'd0000000-0000-0000-0000-000000000001', nombre: 'Cuaderno Espiral 100 hojas',
    descripcionEspecifica: 'Cuadriculado', stock: 150, precioVenta: 15.00, costo: 10.00,
    categoria: materialEscolar, marca: faber },
  { publicId: 'd0000000-0000-0000-0000-000000000002', nombre: 'Caja Bolígrafos Azules x12',
    descripcionEspecifica: 'Punta fina', stock: 80, precioVenta: 25.00, costo: 18.00,
    categoria: materialEscolar, marca: faber },
  { publicId: 'd0000000-0000-0000-0000-000000000003', nombre: 'Libro Matemáticas 5to Sec',
    descripcionEspecifica: 'Álgebra', stock: 40, precioVenta: 120.00, costo: 90.00,
    categoria: librosDeTexto, marca: santillana },
  { publicId: 'd0000000-0000-0000-0000-000000000004', nombre: 'Historia Universal 2026',
    descripcionEspecifica: 'Edición actualizada', stock: 25, precioVenta: 180.00, costo: 130.00,
    categoria: librosDeTexto, marca: santillana },
  { publicId: 'd0000000-0000-0000-0000-000000000005', nombre: 'Calculadora Científica',
    descripcionEspecifica: 'Modelo básico', stock: 30, precioVenta: 85.00, costo: 60.00,
    categoria: materialEscolar, marca: faber }
];
for (const p of productos) {
  sembrar('productos', {
    publicId: p.publicId,
    nombre: p.nombre,
    descripcionEspecifica: p.descripcionEspecifica,
    fechaVencimiento: null,
    esPerecedero: false,
    stock: p.stock,
    precioVenta: Decimal128(p.precioVenta.toFixed(2)),
    costoAdquisicionActual: Decimal128(p.costo.toFixed(2)),
    estado: true,
    categoria: p.categoria,
    marca: p.marca,
    historicoCostos: [
      {
        costoAdquisicion: Decimal128(p.costo.toFixed(2)),
        fechaVencimiento: null,
        tipoCambioUsd: null,
        motivo: 'Registro inicial',
        fechaVigencia: fechaSemilla,
        usuarioCreacionId: ADMIN
      }
    ],
    ...auditoria()
  });
}

print('libreria_productos: datos semilla listos.');
