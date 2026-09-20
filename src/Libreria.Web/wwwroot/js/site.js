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

document.addEventListener("DOMContentLoaded", function () {
    const modalTriggers = document.querySelectorAll("[data-confirmation-trigger]");
    let lastTrigger = null;

    modalTriggers.forEach(function (trigger) {
        trigger.addEventListener("click", function () {
            const modal = document.getElementById(trigger.dataset.modalTarget);
            if (!modal) {
                return;
            }

            const name = modal.querySelector("[data-confirmation-name]");
            const identifier = modal.querySelector("[data-confirmation-id]");
            const warning = modal.querySelector("[data-confirmation-warning]");

            if (name) {
                name.textContent = trigger.dataset.entityName || "";
            }

            if (identifier) {
                identifier.value = trigger.dataset.entityId || "";
            }

            if (warning) {
                warning.hidden = trigger.dataset.hasWarning !== "true";
            }

            lastTrigger = trigger;
            modal.showModal();
        });
    });

    document.querySelectorAll(".confirmation-modal").forEach(function (modal) {
        modal.querySelectorAll("[data-confirmation-close]").forEach(function (button) {
            button.addEventListener("click", function () {
                modal.close();
            });
        });

        modal.addEventListener("click", function (event) {
            if (event.target === modal) {
                modal.close();
            }
        });

        modal.addEventListener("close", function () {
            if (lastTrigger) {
                lastTrigger.focus();
            }
        });
    });
});
