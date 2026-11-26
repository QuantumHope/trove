// Applies the saved HopeLab theme before Trove first paints, and lets the theme picker read and change it.
// Loaded in <head> without defer, so there's no flash of the default theme.
(function () {
    const key = "trove.theme";
    const read = () => { try { return localStorage.getItem(key); } catch { return null; } };
    const apply = (id) => { if (id) { document.documentElement.dataset.theme = id; } };
    apply(read());
    window.trove = window.trove || {};
    window.trove.theme = {
        get: () => read() || document.documentElement.dataset.theme || null,
        set: (id) => { apply(id); try { localStorage.setItem(key, id); } catch { /* private window: applies for now only */ } },
    };
})();
