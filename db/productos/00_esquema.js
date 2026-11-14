// =========================================================
// MS Productos · MongoDB · libreria_productos (US-111)
// Esquema inicial de la sección 5 del planning. Idempotente.
// Docker lo ejecuta solo la primera vez (/docker-entrypoint-initdb.d);
// se puede volver a correr a mano: mongosh /scripts/00_esquema.js
//
// Sin transacciones de varios documentos: el histórico de costos va
// embebido en el producto y el stock se descuenta con una sola
// operación atómica sobre el documento.
// =========================================================

const base = db.getSiblingDB('libreria_productos');

const colecciones = ['categorias', 'marcas', 'productos', 'contadores'];
const existentes = base.getCollectionNames();
for (const nombre of colecciones) {
  if (!existentes.includes(nombre)) {
    base.createCollection(nombre);
  }
}

// createIndex no hace nada si el índice ya existe con la misma definición.
base.categorias.createIndex({ publicId: 1 }, { unique: true, name: 'UQ_categorias_publicId' });
base.categorias.createIndex({ nombre: 1 }, { unique: true, name: 'UQ_categorias_nombre' });
base.categorias.createIndex({ codigo: 1 }, { unique: true, name: 'UQ_categorias_codigo' });

base.marcas.createIndex({ publicId: 1 }, { unique: true, name: 'UQ_marcas_publicId' });
base.marcas.createIndex({ nombre: 1 }, { unique: true, name: 'UQ_marcas_nombre' });

base.productos.createIndex({ publicId: 1 }, { unique: true, name: 'UQ_productos_publicId' });
base.productos.createIndex({ 'categoria.publicId': 1 }, { name: 'IX_productos_categoria' });
base.productos.createIndex({ 'marca.publicId': 1 }, { name: 'IX_productos_marca' });
base.productos.createIndex({ nombre: 1 }, { name: 'IX_productos_nombre' });

print('libreria_productos: colecciones e índices listos.');
