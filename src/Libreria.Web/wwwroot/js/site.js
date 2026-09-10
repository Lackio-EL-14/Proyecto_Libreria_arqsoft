document.addEventListener("DOMContentLoaded", function () {
    const menuToggle = document.getElementById("menuToggle");
    const sidebar = document.getElementById("sidebar");
    const sidebarOverlay = document.getElementById("sidebarOverlay");

    if (!menuToggle || !sidebar || !sidebarOverlay) {
        return;
    }

    function abrirMenu() {
        sidebar.classList.add("is-open");
        sidebarOverlay.classList.add("is-open");
        document.body.classList.add("menu-open");
        menuToggle.setAttribute("aria-expanded", "true");
    }

    function cerrarMenu() {
        sidebar.classList.remove("is-open");
        sidebarOverlay.classList.remove("is-open");
        document.body.classList.remove("menu-open");
        menuToggle.setAttribute("aria-expanded", "false");
    }

    function alternarMenu() {
        const estaAbierto = sidebar.classList.contains("is-open");
        if (estaAbierto) {
            cerrarMenu();
        } else {
            abrirMenu();
        }
    }

    menuToggle.addEventListener("click", alternarMenu);
    sidebarOverlay.addEventListener("click", cerrarMenu);

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && sidebar.classList.contains("is-open")) {
            cerrarMenu();
        }
    });
});