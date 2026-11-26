// Trove's service worker: network first, falling back to the last copy, so the installed app opens without a
// connection. Only same-origin GETs are cached; anything else (and every non-GET) goes straight to the network.
const cacheName = "trove-shell-v1";

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

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET" || new URL(request.url).origin !== self.location.origin) {
        return;
    }

    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        try {
            const response = await fetch(request);
            if (response.ok && response.type === "basic") {
                await cache.put(request, response.clone());
            }
            return response;
        } catch (error) {
            const cached = await cache.match(request);
            if (cached) {
                return cached;
            }
            throw error;
        }
    })());
});
