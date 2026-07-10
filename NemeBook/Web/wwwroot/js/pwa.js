(function () {
    const bells = Array.from(document.querySelectorAll("[data-notification-bell]"));
    const supportsNotifications = "Notification" in window;
    const supportsServiceWorker = "serviceWorker" in navigator;

    let registrationPromise = Promise.resolve(null);

    if (supportsServiceWorker) {
        registrationPromise = navigator.serviceWorker.register("/service-worker.js").catch(() => null);
    }

    function setBellState() {
        bells.forEach((bell) => {
            bell.classList.toggle("is-enabled", supportsNotifications && Notification.permission === "granted");
            bell.classList.toggle("is-denied", supportsNotifications && Notification.permission === "denied");
            bell.title = supportsNotifications
                ? "Нотификации"
                : "Браузърът не поддържа нотификации";
        });
    }

    async function showEnabledNotification() {
        const registration = await registrationPromise;
        const options = {
            body: "Нотификациите са включени.",
            icon: "/icons/icon.svg",
            badge: "/icons/icon.svg"
        };

        if (registration?.showNotification) {
            await registration.showNotification("NemeBook", options);
            return;
        }

        new Notification("NemeBook", options);
    }

    async function handleBellClick() {
        if (!supportsNotifications) {
            setBellState();
            return;
        }

        if (Notification.permission === "default") {
            await Notification.requestPermission();
        }

        setBellState();

        if (Notification.permission === "granted") {
            await showEnabledNotification();
        }
    }

    bells.forEach((bell) => {
        bell.addEventListener("click", () => {
            handleBellClick().catch(setBellState);
        });
    });

    setBellState();
})();
