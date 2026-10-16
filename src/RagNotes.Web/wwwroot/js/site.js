(function () {
    var storageKey = "theme";
    var toggle = document.getElementById("theme-toggle");
    if (!toggle) {
        return;
    }

    var label = toggle.querySelector(".theme-toggle-label");

    function currentTheme() {
        return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    }

    function applyTheme(theme) {
        var isDark = theme === "dark";
        document.documentElement.setAttribute("data-theme", theme);
        toggle.setAttribute("aria-pressed", isDark ? "true" : "false");
        toggle.setAttribute("aria-label", isDark ? "Switch to light mode" : "Switch to dark mode");
        if (label) {
            label.textContent = isDark ? "Light mode" : "Dark mode";
        }
    }

    applyTheme(currentTheme());

    toggle.addEventListener("click", function () {
        var next = currentTheme() === "dark" ? "light" : "dark";
        try {
            localStorage.setItem(storageKey, next);
        } catch (e) {
        }
        applyTheme(next);
    });

    var media = window.matchMedia("(prefers-color-scheme: dark)");
    if (typeof media.addEventListener === "function") {
        media.addEventListener("change", function (event) {
            var stored = null;
            try {
                stored = localStorage.getItem(storageKey);
            } catch (e) {
            }
            if (stored !== "light" && stored !== "dark") {
                applyTheme(event.matches ? "dark" : "light");
            }
        });
    }
})();

(function () {
    function setBusyButton(button, options) {
        if (!button) {
            return;
        }

        var busy = !!(options && options.busy);
        var label = options && options.label != null ? String(options.label) : "";

        if (busy) {
            if (!button.dataset.busyOriginal) {
                button.dataset.busyOriginal = button.innerHTML;
            }
            button.disabled = true;
            button.setAttribute("aria-busy", "true");
            var text = label || button.dataset.busyOriginal || "";
            button.innerHTML =
                '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' +
                text;
            return;
        }

        button.disabled = false;
        button.removeAttribute("aria-busy");
        if (button.dataset.busyOriginal) {
            button.innerHTML = button.dataset.busyOriginal;
            delete button.dataset.busyOriginal;
        }
    }

    function busyStatusFor(form) {
        var inside = form.querySelector("[data-busy-status]");
        if (inside) {
            return inside;
        }
        var next = form.nextElementSibling;
        if (next && next.hasAttribute("data-busy-status")) {
            return next;
        }
        return null;
    }

    function isFormValid(form) {
        if (window.jQuery && jQuery(form).valid) {
            return jQuery(form).valid();
        }
        return true;
    }

    function bindBusyForms() {
        document.querySelectorAll("form[data-busy-label]").forEach(function (form) {
            form.addEventListener("submit", function () {
                if (!isFormValid(form)) {
                    return;
                }

                var label = form.getAttribute("data-busy-label") || "Working…";
                var button = form.querySelector('button[type="submit"], input[type="submit"]');
                setBusyButton(button, { busy: true, label: label });

                var status = busyStatusFor(form);
                if (status) {
                    status.textContent = label;
                    status.classList.remove("d-none");
                }
            });
        });
    }

    window.setBusyButton = setBusyButton;
    bindBusyForms();
})();
