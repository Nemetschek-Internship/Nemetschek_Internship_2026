// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const notificationsDropdown = document.getElementById("notificationsDropdown");
    if (!notificationsDropdown) {
        return;
    }

    const notificationsContainer = document.getElementById("notificationsContainer");
    const notificationBadge = document.getElementById("notificationBadge");
    const markAllButton = document.getElementById("markAllNotificationsRead");
    const antiForgeryTokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryToken = antiForgeryTokenElement ? antiForgeryTokenElement.value : "";

    let notifications = [];

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function formatDate(value) {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleString();
    }

    function updateBadge() {
        const unreadCount = notifications.filter(n => !n.isRead).length;
        notificationBadge.textContent = unreadCount.toString();
        if (unreadCount > 0) {
            notificationBadge.classList.remove("d-none");
        } else {
            notificationBadge.classList.add("d-none");
        }
    }

    function getNotificationUrl(notification) {
        if (notification.chatId) {
            return `/Chat?chatId=${encodeURIComponent(notification.chatId)}`;
        }

        return null;
    }

    function renderNotifications() {
        if (!notifications.length) {
            notificationsContainer.innerHTML = '<li><span class="dropdown-item-text text-muted small">Няма известия</span></li>';
            updateBadge();
            return;
        }

        const itemsHtml = notifications
            .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
            .map(notification => {
                const unreadClass = notification.isRead ? "" : "notification-item-unread";
                const url = getNotificationUrl(notification);

                if (url) {
                    return `
                        <li><a class="dropdown-item notification-item ${unreadClass}" data-notification-id="${notification.id}" href="${url}">
                            <div>${escapeHtml(notification.text || "")}</div>
                            <div class="notification-item-time">${escapeHtml(formatDate(notification.createdAt))}</div>
                        </a></li>
                    `;
                }

                return `
                    <li><button type="button" class="dropdown-item notification-item ${unreadClass}" data-notification-id="${notification.id}">
                        <div>${escapeHtml(notification.text || "")}</div>
                        <div class="notification-item-time">${escapeHtml(formatDate(notification.createdAt))}</div>
                    </button></li>
                `;
            })
            .join("");

        notificationsContainer.innerHTML = itemsHtml;
        updateBadge();
    }

    async function fetchJson(url, options) {
        const response = await fetch(url, options);
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }

        if (response.status === 204) {
            return null;
        }

        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/json")) {
            return await response.json();
        }

        return null;
    }

    async function loadNotifications() {
        try {
            const recent = await fetchJson("/api/notifications/recent?take=20", { credentials: "same-origin" });
            notifications = Array.isArray(recent) ? recent : [];
            renderNotifications();
        } catch {
            // ignore client-side fetch errors
        }
    }

    async function markAsRead(notificationId) {
        try {
            await fetchJson(`/api/notifications/${notificationId}/read`, {
                method: "POST",
                credentials: "same-origin",
                headers: antiForgeryToken
                    ? { "RequestVerificationToken": antiForgeryToken }
                    : undefined
            });

            notifications = notifications.map(notification => {
                if (notification.id === notificationId) {
                    return { ...notification, isRead: true };
                }

                return notification;
            });

            renderNotifications();
        } catch {
            // ignore client-side fetch errors
        }
    }

    async function markAllAsRead() {
        try {
            await fetchJson("/api/notifications/read-all", {
                method: "POST",
                credentials: "same-origin",
                headers: antiForgeryToken
                    ? { "RequestVerificationToken": antiForgeryToken }
                    : undefined
            });

            notifications = notifications.map(notification => ({ ...notification, isRead: true }));
            renderNotifications();
        } catch {
            // ignore client-side fetch errors
        }
    }

    notificationsContainer.addEventListener("click", (event) => {
        const target = event.target;
        if (!(target instanceof Element)) {
            return;
        }

        const item = target.closest("[data-notification-id]");
        if (!item) {
            return;
        }

        const notificationId = item.getAttribute("data-notification-id");
        if (!notificationId) {
            return;
        }

        markAsRead(notificationId);
    });

    if (markAllButton) {
        markAllButton.addEventListener("click", (event) => {
            event.preventDefault();
            markAllAsRead();
        });
    }

    async function startSignalR() {
        if (typeof signalR === "undefined") {
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();

        connection.on("notificationReceived", (notification) => {
            if (!notification || !notification.id) {
                return;
            }

            notifications = [notification, ...notifications.filter(existing => existing.id !== notification.id)].slice(0, 50);
            renderNotifications();
        });

        try {
            await connection.start();
        } catch {
            // ignore client-side SignalR startup errors
        }
    }

    loadNotifications();
    startSignalR();
})();
