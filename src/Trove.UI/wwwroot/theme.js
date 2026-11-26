// Applies the saved HopeLab theme before Trove first paints, and lets the theme picker read and change it.
// Loaded in <head> without defer, so there's no flash of the default theme.
(function () {
    const key = "trove.theme";
    const read = () => { try { return localStorage.getItem(key); } catch { return null; } };
    // The browser's own bars (and an installed app's title bar) follow the theme's background.
    const syncBars = () => {
        const meta = document.querySelector('meta[name="theme-color"]');
        const color = getComputedStyle(document.documentElement).getPropertyValue("--surface-void").trim();
        if (meta && color) { meta.setAttribute("content", color); }
    };
    const apply = (id) => {
        if (id) { document.documentElement.dataset.theme = id; }
        if (document.readyState !== "loading") { syncBars(); }
    };
    apply(read());
    window.addEventListener("load", syncBars);
    window.trove = window.trove || {};
    window.trove.theme = {
        get: () => read() || document.documentElement.dataset.theme || null,
        set: (id) => { apply(id); try { localStorage.setItem(key, id); } catch { /* private window: applies for now only */ } },
    };
})();
