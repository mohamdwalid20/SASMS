// SASMS Service Worker - Windows Push Notifications
const CACHE_NAME = 'sasms-sw-v1';

self.addEventListener('install', (event) => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(clients.claim());
});

// Handle push notifications (for future Web Push API integration)
self.addEventListener('push', (event) => {
    let data = {};
    if (event.data) {
        try {
            data = event.data.json();
        } catch (e) {
            data = { title: 'SASMS', body: event.data.text() };
        }
    }

    const options = {
        body: data.body || 'You have a new notification.',
        icon: '/img/logo.png',
        badge: '/img/logo.png',
        tag: data.tag || 'sasms-notification',
        requireInteraction: false,
        data: { url: data.url || '/' }
    };

    event.waitUntil(
        self.registration.showNotification(data.title || 'SASMS', options)
    );
});

// Handle notification click - open or focus the app
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const targetUrl = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
            // Focus existing window if open
            for (const client of clientList) {
                if (client.url.includes(self.location.origin) && 'focus' in client) {
                    client.navigate(targetUrl);
                    return client.focus();
                }
            }
            // Otherwise open a new window
            if (clients.openWindow) {
                return clients.openWindow(targetUrl);
            }
        })
    );
});

// Listen for messages from the main page to show notifications
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SHOW_NOTIFICATION') {
        const { title, body, icon, url, tag } = event.data;
        const options = {
            body: body,
            icon: icon || '/img/logo.png',
            badge: '/img/logo.png',
            tag: tag || 'sasms-' + Date.now(),
            requireInteraction: false,
            data: { url: url || '/' }
        };
        self.registration.showNotification(title, options);
    }
});
