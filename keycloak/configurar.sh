#!/bin/bash
# =========================================================
# US-124 · Configuración idempotente del realm «libreria».
#
# Corre dentro del contenedor «keycloak-configurar» (misma imagen que
# Keycloak, con kcadm.sh), así que funciona igual en Windows y en Linux:
#
#   docker compose up -d                          # lo ejecuta solo
#   docker compose run --rm keycloak-configurar   # para repetirlo
#
# Con las variables de .env:
#   1. asigna los secretos de los clientes confidenciales;
#   2. crea admin y vendedor con los GUID de docs/contratos/datos-semilla.md
#      (si ya existen, no los toca) y les asegura su rol;
#   3. fija sus contraseñas de desarrollo (definitivas, sin UPDATE_PASSWORD);
#   4. habilita o deshabilita el cliente libreria-pruebas (solo desarrollo).
#
# Nunca imprime secretos ni contraseñas.
# =========================================================
set -euo pipefail

SERVIDOR="${KEYCLOAK_URL_INTERNA:-http://keycloak:8080}"
REALM="libreria"
KCADM="/opt/keycloak/bin/kcadm.sh"
export KCADM_CONFIG="/tmp/kcadm.config"

requerir() {
    local nombre="$1"
    if [[ -z "${!nombre:-}" ]]; then
        echo "ERROR: falta la variable $nombre (ver .env.example)." >&2
        exit 1
    fi
}

for variable in KC_BOOTSTRAP_ADMIN_USERNAME KC_BOOTSTRAP_ADMIN_PASSWORD \
    KEYCLOAK_FRONT_CLIENT_SECRET KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET \
    KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET \
    KEYCLOAK_ADMIN_SEMILLA_PASSWORD KEYCLOAK_VENDEDOR_SEMILLA_PASSWORD; do
    requerir "$variable"
done

kc() { "$KCADM" "$@" --config "$KCADM_CONFIG"; }

echo "Conectando con $SERVIDOR..."
for intento in $(seq 1 30); do
    if "$KCADM" config credentials --config "$KCADM_CONFIG" --server "$SERVIDOR" \
        --realm master --user "$KC_BOOTSTRAP_ADMIN_USERNAME" \
        --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null 2>&1; then
        break
    fi
    if [[ "$intento" -eq 30 ]]; then
        echo "ERROR: no se pudo autenticar contra Keycloak." >&2
        exit 1
    fi
    sleep 2
done

if ! kc get "realms/$REALM" --fields realm >/dev/null 2>&1; then
    echo "ERROR: el realm $REALM no existe; revise la importación de keycloak/realm-libreria.json." >&2
    exit 1
fi

id_cliente() {
    kc get clients -r "$REALM" -q "clientId=$1" --fields id --format csv --noquotes | head -n 1
}

fijar_secreto() {
    local cliente="$1" secreto="$2" id
    id="$(id_cliente "$cliente")"
    if [[ -z "$id" ]]; then
        echo "ERROR: no existe el cliente $cliente." >&2
        exit 1
    fi
    kc update "clients/$id" -r "$REALM" -s "secret=$secreto"
    echo "  secreto asignado: $cliente"
}

echo "Clientes confidenciales:"
fijar_secreto libreria-front "$KEYCLOAK_FRONT_CLIENT_SECRET"
fijar_secreto libreria-usuarios-admin "$KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET"
fijar_secreto libreria-usuarios-verificacion "$KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET"

echo "Usuarios semilla:"
# partialImport con ifResourceExists=SKIP conserva el id del JSON y no toca a los que ya existen.
kc create partialImport -r "$REALM" -f /config/usuarios-semilla.json >/dev/null

asegurar_usuario() {
    local id="$1" usuario="$2" rol="$3" contrasena="$4" nombre
    nombre="$(kc get "users/$id" -r "$REALM" --fields username --format csv --noquotes 2>/dev/null || true)"
    if [[ "$nombre" != "$usuario" ]]; then
        echo "ERROR: el usuario $usuario no tiene el id $id; borre el volumen (docker compose down -v) o el usuario en Keycloak." >&2
        exit 1
    fi
    kc add-roles -r "$REALM" --uid "$id" --rolename "$rol" >/dev/null
    kc set-password -r "$REALM" --userid "$id" --new-password "$contrasena" >/dev/null
    kc update "users/$id" -r "$REALM" -s 'requiredActions=[]' -s enabled=true >/dev/null
    echo "  $usuario ($id) con rol $rol y contraseña de desarrollo"
}

asegurar_usuario a0000000-0000-0000-0000-000000000001 admin Administrador "$KEYCLOAK_ADMIN_SEMILLA_PASSWORD"
asegurar_usuario a0000000-0000-0000-0000-000000000002 vendedor Vendedor "$KEYCLOAK_VENDEDOR_SEMILLA_PASSWORD"

echo "Cliente de pruebas (solo desarrollo):"
habilitar_pruebas="false"
if [[ "${KEYCLOAK_HABILITAR_CLIENTE_PRUEBAS:-false}" == "true" ]]; then
    habilitar_pruebas="true"
fi
kc update "clients/$(id_cliente libreria-pruebas)" -r "$REALM" -s "enabled=$habilitar_pruebas"
echo "  libreria-pruebas habilitado: $habilitar_pruebas"

echo "Listo: realm $REALM configurado."
