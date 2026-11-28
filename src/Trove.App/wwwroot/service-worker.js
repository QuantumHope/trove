// Trove's service worker: network first, falling back to the last copy, so the installed app (and the desktop window)
// opens without a connection and shows the last board it saw. Only same-origin GETs are cached; anything else (and every
// non-GET) goes straight to the network.
const cacheName = "trove-shell-v2";
// The saved copy of an API answer carries the time it was saved, so the app can say it's showing a saved board and how
// old it is (Trove.UI's ConnectionStatus reads it). Only API answers get it: they're small, and rebuilding a response
// means reading its body.
const savedAtHeader = "X-Trove-Saved-At";
// Old fingerprinted assets pile up across deploys, so keep only the most recently stored entries.
const maxEntries = 300;

self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (event) => {
    event.waitUntil((async () => {
        for (const key of await caches.keys()) {
            if (key !== cacheName) {
                await caches.delete(key);
            }
        }
        await self.clients.claim();
    })());
});

// Stores a copy without holding up the response. A failed write (a full quota, say) only means no offline copy.
async function remember(request, response) {
    try {
        if (new URL(request.url).pathname.includes("/api/")) {
            const headers = new Headers(response.headers);
            headers.set(savedAtHeader, new Date().toISOString());
            response = new Response(await response.blob(), { status: response.status, statusText: response.statusText, headers });
        }
        const cache = await caches.open(cacheName);
        await cache.delete(request);
        await cache.put(request, response);
        const keys = await cache.keys();
        for (const old of keys.slice(0, Math.max(0, keys.length - maxEntries))) {
            await cache.delete(old);
        }
    } catch {
        // Nothing to do: the network answer already went back.
    }
}

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET" || new URL(request.url).origin !== self.location.origin) {
        return;
    }

    event.respondWith((async () => {
        let response;
        try {
            response = await fetch(request);
        } catch (error) {
            const cached = await caches.match(request, { cacheName });
            if (cached) {
                return cached;
            }
            throw error;
        }
        if (response.ok && response.type === "basic") {
            event.waitUntil(remember(request, response.clone()));
        }
        return response;
    })());
});
