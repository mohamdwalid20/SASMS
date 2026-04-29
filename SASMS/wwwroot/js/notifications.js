// ============================================================
// SASMS - Windows Desktop Notifications via Service Worker
// Works on any modern browser on Windows
// ============================================================

const SASMSNotifications = (() => {
    let swRegistration = null;
    let permissionGranted = false;
    let signalRConnection = null;

    // Register Service Worker
    async function registerSW() {
        if (!('serviceWorker' in navigator)) {
            console.warn('[SASMS Notifications] Service Worker not supported.');
            return false;
        }
        try {
            swRegistration = await navigator.serviceWorker.register('/sw.js', { scope: '/' });
            console.log('[SASMS Notifications] Service Worker registered.');
            return true;
        } catch (err) {
            console.error('[SASMS Notifications] Service Worker registration failed:', err);
            return false;
        }
    }

    // Request notification permission from the user
    async function requestPermission() {
        if (!('Notification' in window)) {
            console.warn('[SASMS Notifications] Notifications not supported.');
            return false;
        }

        if (Notification.permission === 'granted') {
            permissionGranted = true;
            return true;
        }

        if (Notification.permission === 'denied') {
            console.warn('[SASMS Notifications] Notification permission denied by user.');
            return false;
        }

        const result = await Notification.requestPermission();
        permissionGranted = (result === 'granted');
        return permissionGranted;
    }

    // Send a Windows desktop notification
    function showNotification(title, body, url = '/', tag = null) {
        if (!permissionGranted) return;

        if (swRegistration && swRegistration.active) {
            // Use Service Worker for better Windows integration
            swRegistration.active.postMessage({
                type: 'SHOW_NOTIFICATION',
                title: title,
                body: body,
                icon: '/img/logo.png',
                url: url,
                tag: tag || ('sasms-' + Date.now())
            });
        } else {
            // Fallback to direct Notification API
            const n = new Notification(title, {
                body: body,
                icon: '/img/logo.png',
                tag: tag || ('sasms-' + Date.now())
            });
            n.onclick = () => {
                window.focus();
                if (url !== '/') window.location.href = url;
                n.close();
            };
        }
    }

    // Initialize and hook into existing SignalR connection
    function hookIntoSignalR(connection) {
        if (!connection) return;
        signalRConnection = connection;

        // Listen for the real-time notification event from server
        connection.on('ReceiveNewNotification', (notifData) => {
            showNotification(
                notifData.title || 'SASMS',
                notifData.message || '',
                notifData.actionUrl || '/',
                'sasms-notif-' + notifData.id
            );
        });

        // Also listen to generic data updates and convert important ones to notifications
        connection.on('ReceiveDataUpdate', (entityType, action) => {
            // We rely on ReceiveNewNotification for user-specific alerts.
            // ReceiveDataUpdate is for page refreshes only — no OS popup here.
        });

        console.log('[SASMS Notifications] Hooked into SignalR connection.');
    }

    // Auto-init on page load
    async function init(connection) {
        const swOk = await registerSW();
        if (swOk) {
            await requestPermission();
        }
        if (connection) {
            hookIntoSignalR(connection);
        }
    }

    return { init, showNotification, requestPermission, hookIntoSignalR };
})();

// ============================================================
// Auto-init: wait for the page's SignalR connection
// We expose a global so layouts can call SASMSNotifications.init(connection)
// ============================================================
window.SASMSNotifications = SASMSNotifications;
