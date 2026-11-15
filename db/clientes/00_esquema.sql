-- =========================================================
-- MS Clientes · MySQL 8 · libreria_clientes (US-111)
-- Esquema inicial de la sección 5 del planning. Idempotente.
-- Docker lo ejecuta solo la primera vez (/docker-entrypoint-initdb.d);
-- se puede volver a correr a mano sin efectos.
-- En Linux los nombres de tabla distinguen mayúsculas: siempre `Cliente`.
-- =========================================================

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS libreria_clientes
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE libreria_clientes;

CREATE TABLE IF NOT EXISTS Cliente (
    ClienteId              INT AUTO_INCREMENT PRIMARY KEY,
    PublicId               CHAR(36)     NOT NULL,
    CiNit                  VARCHAR(30)  NOT NULL,
    RazonSocial            VARCHAR(200) NOT NULL,
    Correo                 VARCHAR(254) NULL,
    Estado                 TINYINT(1)   NOT NULL DEFAULT 1,

    -- Auditoría: `sub` del JWT (PublicId del usuario en MS Usuarios), sin FK.
    FechaCreacion          DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UsuarioCreacionId      CHAR(36)     NULL,
    FechaModificacion      DATETIME(6)  NULL,
    UsuarioModificacionId  CHAR(36)     NULL,

    CONSTRAINT UQ_Cliente_PublicId UNIQUE (PublicId),
    CONSTRAINT UQ_Cliente_CiNit UNIQUE (CiNit),
    CONSTRAINT CK_Cliente_CiNit_NoVacio CHECK (TRIM(CiNit) <> ''),
    CONSTRAINT CK_Cliente_RazonSocial_NoVacia CHECK (TRIM(RazonSocial) <> '')
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_0900_ai_ci;
