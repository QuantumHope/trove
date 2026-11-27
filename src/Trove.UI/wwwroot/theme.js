// Applies the saved HopeLab theme before Trove first paints, and gives the theme picker what it needs.
// Loaded in <head> without defer, so there's no flash of the wrong theme.
//
// The stored value is the person's choice: a theme id, or nothing for "system", which follows the device between
// Light and Dark (CORA's model; Trove.UI/Theming/ThemePreference.cs holds the same rules in C#).
(function () {
    const key = "trove.theme";
    const darkQuery = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;
    const systemDark = () => (darkQuery ? darkQuery.matches : true);
    const read = () => { try { return localStorage.getItem(key) || "system"; } catch { return "system"; } };
    const resolve = (pref) => (pref && pref !== "system" ? pref : (systemDark() ? "dark" : "light"));
    // The browser's own bars (and an installed app's title bar) follow the theme's background.
    const syncBars = () => {
        const meta = document.querySelector('meta[name="theme-color"]');
        const color = getComputedStyle(document.documentElement).getPropertyValue("--surface-void").trim();
        if (meta && color) { meta.setAttribute("content", color); }
    };
    const apply = () => {
        document.documentElement.dataset.theme = resolve(read());
        if (document.readyState !== "loading") { syncBars(); }
    };
    apply();
    window.addEventListener("load", syncBars);
    if (darkQuery && darkQuery.addEventListener) { darkQuery.addEventListener("change", apply); }

    const listKeys = ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", "Escape"];
    window.trove = window.trove || {};
    window.trove.theme = {
        get: () => read(),
        systemDark: () => systemDark(),
        set: (pref) => {
            try {
                if (!pref || pref === "system") { localStorage.removeItem(key); } else { localStorage.setItem(key, pref); }
            } catch { /* blocked storage: the choice holds for this page only */ }
            if (!pref || pref === "system") { document.documentElement.dataset.theme = resolve("system"); }
            else { document.documentElement.dataset.theme = pref; }
            syncBars();
        },
        // The picker's keys: the arrows, Home and End move the choice, Escape closes. Blazor can't cancel a key
        // conditionally, so the list hands these to .NET and leaves Tab and the rest to the browser.
        bindList: (el, list) => {
            el.troveKeys = (e) => {
                if (!listKeys.includes(e.key)) { return; }
                e.preventDefault();
                list.invokeMethodAsync("OnKey", e.key);
            };
            el.addEventListener("keydown", el.troveKeys);
        },
        unbindList: (el) => { if (el && el.troveKeys) { el.removeEventListener("keydown", el.troveKeys); } },
        focusChecked: (el) => { const on = el && el.querySelector('[aria-checked="true"]'); if (on) { on.focus(); } },
    };
})();
