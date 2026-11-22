/**
 * Menú de navegación (menú lateral colapsable en escritorio y panel en el celular)
 * y mensajes de la página que el usuario puede cerrar.
 */
document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const menuToggle = document.getElementById("menuToggle");
    const sidebar = document.getElementById("sidebar");
    const sidebarOverlay = document.getElementById("sidebarOverlay");

    // 1. Restaurar la preferencia de menú colapsado en escritorio
    const STORAGE_KEY = "libreria_sidebar_collapsed";
    let isCollapsedSaved = false;
    try {
        isCollapsedSaved = localStorage.getItem(STORAGE_KEY) === "true";
    } catch (error) {
        isCollapsedSaved = false;
    }

    if (isCollapsedSaved && window.innerWidth > 768) {
        body.classList.add("sidebar-collapsed");
    }

    // 2. Colapsar o expandir el menú (escritorio)
    function toggleSidebarCollapse() {
        body.classList.toggle("sidebar-collapsed");
        const isCollapsed = body.classList.contains("sidebar-collapsed");
        if (menuToggle) menuToggle.setAttribute("aria-expanded", isCollapsed ? "false" : "true");
        try {
            localStorage.setItem(STORAGE_KEY, isCollapsed);
        } catch (error) {
            // Sin almacenamiento local, la preferencia solo dura en esta página.
        }
    }

    // 3. Menú como panel en el celular (< 768px)
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
            toggleSidebarCollapse();
        }
    }

    if (menuToggle) {
        menuToggle.setAttribute("aria-expanded",
            window.innerWidth > 768 && !body.classList.contains("sidebar-collapsed") ? "true" : "false");
        menuToggle.addEventListener("click", toggleMenu);
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener("click", cerrarMenuMovil);
    }

    // Cerrar el panel del celular con Escape
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && sidebar && sidebar.classList.contains("is-open")) {
            cerrarMenuMovil();
        }
    });

    // Ajustar si cambia el tamaño de la ventana
    window.addEventListener("resize", function () {
        if (window.innerWidth > 768 && sidebar && sidebar.classList.contains("is-open")) {
            cerrarMenuMovil();
        }
    });

    // 4. Los mensajes de éxito o error de la página se pueden cerrar
    document.querySelectorAll(".mensaje").forEach(function (mensaje) {
        const cerrar = document.createElement("button");
        cerrar.type = "button";
        cerrar.className = "mensaje__cerrar";
        cerrar.setAttribute("aria-label", "Cerrar mensaje");
        cerrar.textContent = "×";
        cerrar.addEventListener("click", function () {
            mensaje.remove();
        });
        mensaje.appendChild(cerrar);
    });
});
