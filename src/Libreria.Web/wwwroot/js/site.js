const menuToggle = document.getElementById("menuToggle");
const sidebar = document.getElementById("sidebar");
const sidebarOverlay = document.getElementById("sidebarOverlay");

function abrirMenu() {
    sidebar.classList.add("app-sidebar--open");
    sidebarOverlay.classList.add("sidebar-overlay--visible");

    menuToggle.setAttribute("aria-expanded", "true");
    document.body.classList.add("menu-open");
}

function cerrarMenu() {
    sidebar.classList.remove("app-sidebar--open");
    sidebarOverlay.classList.remove("sidebar-overlay--visible");

    menuToggle.setAttribute("aria-expanded", "false");
    document.body.classList.remove("menu-open");
}

function alternarMenu() {
    const estaAbierto =
        sidebar.classList.contains("app-sidebar--open");

    if (estaAbierto) {
        cerrarMenu();
        return;
    }

    abrirMenu();
}

menuToggle.addEventListener("click", alternarMenu);
sidebarOverlay.addEventListener("click", cerrarMenu);

document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
        cerrarMenu();
    }
});