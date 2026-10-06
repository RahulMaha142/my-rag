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
