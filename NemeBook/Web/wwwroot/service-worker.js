const CACHE_NAME = "nemebook-pwa-v2";
const CORE_ASSETS = [
    "/offline.html",
    "/css/site.css",
    "/js/pwa.js",
    "/icons/icon.svg"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(CORE_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => Promise.all(
                cacheNames
                    .filter((cacheName) => cacheName !== CACHE_NAME)
                    .map((cacheName) => caches.delete(cacheName))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET") {
        return;
    }

    const requestUrl = new URL(event.request.url);
    if (requestUrl.origin !== self.location.origin) {
        return;
    }

    if (requestUrl.pathname.startsWith("/notifications")) {
        event.respondWith(fetch(event.request));
        return;
    }

    if (event.request.mode === "navigate") {
        event.respondWith(
            fetch(event.request).catch(() => caches.match("/offline.html"))
        );
        return;
    }

    event.respondWith(
        caches.match(event.request).then((cachedResponse) => {
            return cachedResponse || fetch(event.request).then((response) => {
                if (!response || response.status !== 200 || response.type !== "basic") {
                    return response;
                }

                const responseToCache = response.clone();
                caches.open(CACHE_NAME).then((cache) => cache.put(event.request, responseToCache));
                return response;
            });
        })
    );
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil(self.clients.openWindow("/"));
});
