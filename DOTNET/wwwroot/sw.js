const CACHE_NAME = 'libreria-offline-v1';
const OFFLINE_PAGE = '/offline.html';

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.add(OFFLINE_PAGE))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(key => key.startsWith('libreria-offline-') && key !== CACHE_NAME).map(key => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

// Solo ofrece respaldo offline para navegación; ventas, clientes y API nunca se cachean.
self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET' || request.mode !== 'navigate') return;

  event.respondWith(
    fetch(request).catch(async () => {
      const cached = await caches.match(OFFLINE_PAGE);
      return cached || new Response('Sin conexión. Conéctate a internet e inténtalo de nuevo.', {
        status: 503,
        headers: { 'Content-Type': 'text/plain; charset=UTF-8' }
      });
    })
  );
});
