document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-nb-doubleclick-logout]").forEach((trigger) => {
        trigger.addEventListener("dblclick", (event) => {
            event.preventDefault();

            const form = trigger.closest("form");
            if (form) {
                form.submit();
            }
        });
    });
});
