/**
 * Gestión del Menú de Navegación (Mini-Sidebar Desktop y Drawer Móvil)
 * US-29: Rediseño Escalable del Menú y Dashboard
 */
document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const menuToggle = document.getElementById("menuToggle");
    const sidebarCollapseBtn = document.getElementById("sidebarCollapseBtn");
    const sidebar = document.getElementById("sidebar");
    const sidebarOverlay = document.getElementById("sidebarOverlay");

    // 1. Restaurar preferencia de mini-sidebar colapsado en desktop
    const STORAGE_KEY = "libreria_sidebar_collapsed";
    const isCollapsedSaved = localStorage.getItem(STORAGE_KEY) === "true";

    if (isCollapsedSaved && window.innerWidth > 768) {
        body.classList.add("sidebar-collapsed");
    }

    // 2. Alternar estado colapsado (Desktop)
    function toggleSidebarCollapse() {
        body.classList.toggle("sidebar-collapsed");
        const isCollapsed = body.classList.contains("sidebar-collapsed");
        localStorage.setItem(STORAGE_KEY, isCollapsed);
    }

    if (sidebarCollapseBtn) {
        sidebarCollapseBtn.addEventListener("click", toggleSidebarCollapse);
    }

    // 3. Menú Drawer para Móviles (< 768px) y Toggle
    function abrirMenuMovil() {
        if (!sidebar || !sidebarOverlay) return;
        sidebar.classList.add("is-open");
        sidebarOverlay.classList.add("is-open");
        body.classList.add("menu-open");
        if (menuToggle) menuToggle.setAttribute("aria-expanded", "true");
    }

    function cerrarMenuMovil() {
        if (!sidebar || !sidebarOverlay) return;
        sidebar.classList.remove("is-open");
        sidebarOverlay.classList.remove("is-open");
        body.classList.remove("menu-open");
        if (menuToggle) menuToggle.setAttribute("aria-expanded", "false");
    }

    function toggleMenu() {
        if (window.innerWidth <= 768) {
            const estaAbierto = sidebar && sidebar.classList.contains("is-open");
            if (estaAbierto) {
                cerrarMenuMovil();
            } else {
                abrirMenuMovil();
            }
        } else {
            // En desktop, el botón superior también colapsa/expande
            toggleSidebarCollapse();
        }
    }

    if (menuToggle) {
        menuToggle.addEventListener("click", toggleMenu);
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener("click", cerrarMenuMovil);
    }

    // Cerrar drawer móvil con Escape
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && sidebar && sidebar.classList.contains("is-open")) {
            cerrarMenuMovil();
        }
    });

    // Ajustar si cambia el tamaño de ventana
    window.addEventListener("resize", function () {
        if (window.innerWidth > 768 && sidebar && sidebar.classList.contains("is-open")) {
            cerrarMenuMovil();
        }
    });
});
