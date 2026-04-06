// This no-op service worker replaces the old Blazor WASM service worker.
// It immediately unregisters itself and clears all caches so that the
// React SPA is served directly by the network.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', async () => {
  const keys = await caches.keys();
  await Promise.all(keys.map(k => caches.delete(k)));
  await self.registration.unregister();
});
