-- =========================================================
-- MS Clientes · datos semilla (US-111). Idempotente.
-- GUID fijos de docs/contratos/datos-semilla.md.
-- =========================================================

SET NAMES utf8mb4;
USE libreria_clientes;

INSERT INTO Cliente (PublicId, CiNit, RazonSocial, Correo, Estado, FechaCreacion, UsuarioCreacionId)
VALUES
    ('c0000000-0000-0000-0000-000000000001', '9999999', 'Universidad Mayor',
     'compras@um.edu.bo', 1, '2026-11-01 09:00:00', 'a0000000-0000-0000-0000-000000000001'),
    ('c0000000-0000-0000-0000-000000000002', '4567890', 'María Fernanda Quispe Choque',
     'maria.quispe@correo.bo', 1, '2026-11-01 09:10:00', 'a0000000-0000-0000-0000-000000000002'),
    ('c0000000-0000-0000-0000-000000000003', '1234567', 'Juan Carlos Pérez Rojas',
     NULL, 0, '2026-11-01 09:20:00', 'a0000000-0000-0000-0000-000000000001')
ON DUPLICATE KEY UPDATE PublicId = Cliente.PublicId;
