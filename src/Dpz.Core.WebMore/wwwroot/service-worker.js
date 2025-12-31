// 开发环境的 Service Worker
// 在开发环境中，我们不希望缓存任何内容，以便于调试

self.addEventListener('install', event => {
    // 跳过等待，立即激活
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    // 立即接管所有客户端
    event.waitUntil(clients.claim());
});

self.addEventListener('fetch', event => {
    // 在开发环境中，直接使用网络请求，不缓存
    return;
});
