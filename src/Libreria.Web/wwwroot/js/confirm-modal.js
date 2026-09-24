/**
 * Componente Modal de Confirmación Compartido (US-28)
 * Soporta Focus Trap, accesibilidad ARIA, cierre con Escape y clic en Overlay.
 */
document.addEventListener("DOMContentLoaded", function () {
    let lastActiveElement = null;

    // Selector para triggers de modal (soporta tanto data-confirm-trigger como data-confirmation-trigger)
    const triggers = document.querySelectorAll("[data-confirm-trigger], [data-confirmation-trigger]");

    triggers.forEach(function (trigger) {
        trigger.addEventListener("click", function () {
            const targetId = trigger.dataset.modalTarget;
            const modal = document.getElementById(targetId);
            if (!modal) return;

            const entityId = trigger.dataset.entityId || "";
            const entityName = trigger.dataset.entityName || "";
            const hasWarning = trigger.dataset.hasWarning === "true";
            const customWarning = trigger.dataset.warningText || "";

            // Inyectar valores dinámicos
            const nameEl = modal.querySelector("[data-confirm-name], [data-confirmation-name]");
            const idEl = modal.querySelector("[data-confirm-id], [data-confirmation-id]");
            const warningEl = modal.querySelector("[data-confirm-warning], [data-confirmation-warning]");
            const warningTextEl = modal.querySelector("[data-confirm-warning-text]");

            if (nameEl) nameEl.textContent = entityName;
            if (idEl) idEl.value = entityId;

            if (warningEl) {
                if (warningTextEl && customWarning) {
                    warningTextEl.textContent = customWarning;
                }
                warningEl.hidden = !hasWarning;
            }

            lastActiveElement = trigger;

            if (typeof modal.showModal === "function") {
                modal.showModal();
            } else {
                modal.setAttribute("open", "");
                modal.style.display = "block";
            }

            // Mover el foco al primer elemento accionable (botón Cancelar o botón Cerrar)
            const focusables = getFocusableElements(modal);
            if (focusables.length > 0) {
                focusables[0].focus();
            }
        });
    });

    // Delegación y cierre de todos los modales de confirmación
    const modals = document.querySelectorAll(".confirmation-modal");

    modals.forEach(function (modal) {
        // Botones de cierre (Cancelar / Cerrar x)
        const closeButtons = modal.querySelectorAll("[data-confirm-close], [data-confirmation-close]");
        closeButtons.forEach(function (btn) {
            btn.addEventListener("click", function () {
                closeModal(modal);
            });
        });

        // Clic fuera del modal (Overlay / Backdrop)
        modal.addEventListener("click", function (e) {
            const rect = modal.getBoundingClientRect();
            const isInDialog = (
                rect.top <= e.clientY &&
                e.clientY <= rect.top + rect.height &&
                rect.left <= e.clientX &&
                e.clientX <= rect.left + rect.width
            );
            if (!isInDialog) {
                closeModal(modal);
            }
        });

        // Evento nativo de cierre
        modal.addEventListener("close", function () {
            if (lastActiveElement) {
                lastActiveElement.focus();
                lastActiveElement = null;
            }
        });

        // Focus Trap y Tecla Escape para navegación por teclado
        modal.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                e.preventDefault();
                closeModal(modal);
                return;
            }

            if (e.key === "Tab") {
                handleFocusTrap(modal, e);
            }
        });
    });

    function closeModal(modal) {
        if (typeof modal.close === "function") {
            modal.close();
        } else {
            modal.removeAttribute("open");
            modal.style.display = "none";
            if (lastActiveElement) {
                lastActiveElement.focus();
                lastActiveElement = null;
            }
        }
    }

    function getFocusableElements(container) {
        return Array.from(container.querySelectorAll(
            'button:not([disabled]), [href], input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter(el => el.offsetWidth > 0 || el.offsetHeight > 0 || el === document.activeElement);
    }

    function handleFocusTrap(modal, e) {
        const focusableElements = getFocusableElements(modal);
        if (focusableElements.length === 0) return;

        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];

        if (e.shiftKey) {
            // Shift + Tab: si estamos en el primer elemento, ciclar al último
            if (document.activeElement === firstElement) {
                e.preventDefault();
                lastElement.focus();
            }
        } else {
            // Tab normal: si estamos en el último elemento, ciclar al primero
            if (document.activeElement === lastElement) {
                e.preventDefault();
                firstElement.focus();
            }
        }
    }
});
