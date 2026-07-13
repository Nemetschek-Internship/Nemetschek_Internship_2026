(function () {
    const main = document.querySelector(".nb-chat-main");
    if (!main || !window.signalR) {
        return;
    }

    const currentUserId = (main.dataset.currentUserId || "").toLowerCase();
    const roomPanel = document.getElementById("chatRoomPanel");
    const chatList = document.getElementById("chatList");
    const contactsContainer = document.getElementById("chatContacts");
    const emptyState = document.getElementById("chatEmptyState");
    const newChatButton = document.getElementById("chatNewButton");
    const searchPanel = document.getElementById("chatSearchPanel");
    const searchCloseButton = document.getElementById("chatSearchClose");
    const searchInput = document.getElementById("chatContactSearch");
    const messagesContainer = document.getElementById("chatMessages");
    const messageForm = document.getElementById("chatMessageForm");
    const messageInput = document.getElementById("chatMessageInput");
    const sendButton = document.getElementById("chatSendButton");
    const activeTitle = document.getElementById("activeChatTitle");
    const activeSubtitle = document.getElementById("activeChatSubtitle");
    const activeAvatar = document.getElementById("activeChatAvatar");
    const connectionDot = document.getElementById("chatConnectionDot");
    const connectionText = document.getElementById("chatConnectionText");

    let activeChatId = null;
    let searchTimer = null;

    const connection = new window.signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    function setConnectionState(text, stateClass) {
        connectionText.textContent = text;
        connectionDot.className = `nb-chat-connection-dot ${stateClass || ""}`.trim();
    }

    function normalizeId(value) {
        return (value || "").toLowerCase();
    }

    function getValue(value, fallback) {
        return value === undefined || value === null ? fallback : value;
    }

    function formatTime(value) {
        if (!value) {
            return "";
        }

        return new Intl.DateTimeFormat("bg-BG", {
            hour: "2-digit",
            minute: "2-digit"
        }).format(new Date(value));
    }

    function clearMessages() {
        messagesContainer.innerHTML = "";
    }

    function appendMessage(message) {
        const senderId = normalizeId(getValue(message.senderId, message.SenderId));
        const isMine = senderId === currentUserId;
        const item = document.createElement("div");
        item.className = `nb-chat-message ${isMine ? "is-mine" : ""}`;

        const bubble = document.createElement("div");
        bubble.className = "nb-chat-bubble";

        const meta = document.createElement("span");
        meta.className = "nb-chat-message-meta";
        meta.textContent = `${getValue(message.senderName, message.SenderName)} · ${formatTime(getValue(message.sentAt, message.SentAt))}`;

        const text = document.createElement("p");
        text.textContent = getValue(message.text, message.Text);

        bubble.append(meta, text);
        item.appendChild(bubble);
        messagesContainer.appendChild(item);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function setComposerEnabled(isEnabled) {
        messageInput.disabled = !isEnabled;
        sendButton.disabled = !isEnabled;
    }

    function setActiveListItem(chatId) {
        document.querySelectorAll(".nb-chat-list-item").forEach((item) => {
            item.classList.toggle("is-active", normalizeId(item.dataset.chatId) === normalizeId(chatId));
        });
    }

    function setSearchPanelOpen(isOpen) {
        if (!searchPanel || !newChatButton) {
            return;
        }

        searchPanel.hidden = !isOpen;
        newChatButton.setAttribute("aria-expanded", isOpen ? "true" : "false");

        if (isOpen) {
            searchInput.focus();
            searchContacts();
        }
    }

    async function openChat(chatId, title, subtitle, initials) {
        activeChatId = chatId;
        roomPanel.classList.remove("is-idle");
        activeTitle.textContent = title || "Разговор";
        activeSubtitle.textContent = subtitle || "Активен чат";
        activeAvatar.textContent = initials || "N";
        setActiveListItem(chatId);
        setComposerEnabled(true);
        setSearchPanelOpen(false);
        clearMessages();

        const loading = document.createElement("div");
        loading.className = "nb-chat-welcome";
        loading.innerHTML = "<strong>Зареждане...</strong>";
        messagesContainer.appendChild(loading);

        try {
            await connection.invoke("JoinChat", chatId);
        } catch {
            clearMessages();
            showWelcome("Неуспешно отваряне на чата.", "Опитайте отново след малко.");
            setComposerEnabled(false);
        }
    }

    function showWelcome(title, subtitle) {
        messagesContainer.innerHTML = "";
        const welcome = document.createElement("div");
        welcome.className = "nb-chat-welcome";
        const icon = document.createElement("i");
        icon.className = "bi bi-chat-heart-fill";
        const strong = document.createElement("strong");
        strong.textContent = title;
        const span = document.createElement("span");
        span.textContent = subtitle;
        welcome.append(icon, strong, span);
        messagesContainer.appendChild(welcome);
    }

    function bindChatItem(item) {
        item.addEventListener("click", () => {
            openChat(
                item.dataset.chatId,
                item.querySelector("strong")?.textContent,
                item.querySelector("small")?.textContent,
                item.querySelector(".nb-chat-avatar")?.textContent
            );
        });
    }

    function createChatItem(chat) {
        const id = getValue(chat.id, chat.Id);
        let existing = chatList.querySelector(`[data-chat-id="${id}"]`);
        if (existing) {
            return existing;
        }

        const item = document.createElement("button");
        item.className = "nb-chat-list-item";
        item.type = "button";
        item.dataset.chatId = id;

        const avatar = document.createElement("span");
        avatar.className = "nb-chat-avatar";
        avatar.textContent = getValue(chat.initials, chat.Initials);

        const copy = document.createElement("span");
        copy.className = "nb-chat-list-copy";

        const title = document.createElement("strong");
        title.textContent = getValue(chat.title, chat.Title);

        const subtitle = document.createElement("small");
        subtitle.textContent = getValue(chat.lastMessage, chat.LastMessage)
            || getValue(chat.subtitle, chat.Subtitle)
            || "Директен чат";

        const time = document.createElement("em");
        time.textContent = getValue(chat.lastMessageTime, chat.LastMessageTime) || "";

        copy.append(title, subtitle);
        item.append(avatar, copy, time);
        chatList.prepend(item);
        bindChatItem(item);
        updateEmptyState();
        return item;
    }

    function updateEmptyState() {
        emptyState.classList.toggle("is-visible", chatList.children.length === 0);
    }

    async function startDirectChat(contactId) {
        try {
            const response = await fetch("/Chat/Direct", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ targetUserId: contactId })
            });

            if (!response.ok) {
                throw new Error("Cannot start chat");
            }

            const chat = await response.json();
            const item = createChatItem(chat);
            setSearchPanelOpen(false);
            await openChat(
                item.dataset.chatId,
                item.querySelector("strong")?.textContent,
                item.querySelector("small")?.textContent,
                item.querySelector(".nb-chat-avatar")?.textContent
            );
        } catch {
            showWelcome("Не можете да започнете този чат.", "Проверете дали контактът е достъпен за вашата роля.");
        }
    }

    function renderContacts(contacts) {
        contactsContainer.innerHTML = "";

        if (contacts.length === 0) {
            const empty = document.createElement("div");
            empty.className = "nb-chat-contact-empty";
            empty.textContent = "Няма намерени контакти.";
            contactsContainer.appendChild(empty);
            return;
        }

        contacts.forEach((contact) => {
            const id = getValue(contact.id, contact.Id);
            const button = document.createElement("button");
            button.className = "nb-chat-contact";
            button.type = "button";
            button.dataset.contactId = id;

            const avatar = document.createElement("span");
            avatar.className = "nb-chat-avatar";
            avatar.textContent = getValue(contact.initials, contact.Initials);

            const copy = document.createElement("span");
            const name = document.createElement("strong");
            name.textContent = getValue(contact.fullName, contact.FullName);
            const role = document.createElement("small");
            role.textContent = getValue(contact.role, contact.Role);

            copy.append(name, role);
            button.append(avatar, copy);
            button.addEventListener("click", () => startDirectChat(button.dataset.contactId));

            contactsContainer.appendChild(button);
        });
    }

    async function searchContacts() {
        const term = searchInput.value.trim();
        try {
            const response = await fetch(`/Chat/Search?term=${encodeURIComponent(term)}`);
            if (!response.ok) {
                renderContacts([]);
                return;
            }

            renderContacts(await response.json());
        } catch {
            renderContacts([]);
        }
    }

    connection.onreconnecting(() => setConnectionState("Повторно свързване", "is-warning"));
    connection.onreconnected(() => setConnectionState("Свързано", "is-online"));
    connection.onclose(() => setConnectionState("Изключено", "is-offline"));

    connection.on("ChatReady", (chatId, messages) => {
        if (normalizeId(chatId) !== normalizeId(activeChatId)) {
            return;
        }

        clearMessages();
        if (!messages || messages.length === 0) {
            showWelcome("Няма съобщения.", "Изпратете първото съобщение в този разговор.");
            return;
        }

        messages.forEach(appendMessage);
    });

    connection.on("ReceiveMessage", (message) => {
        const chatId = getValue(message.chatId, message.ChatId);
        if (normalizeId(chatId) !== normalizeId(activeChatId)) {
            return;
        }

        if (messagesContainer.querySelector(".nb-chat-welcome")) {
            clearMessages();
        }

        appendMessage(message);
    });

    messageForm.addEventListener("submit", async (event) => {
        event.preventDefault();

        const text = messageInput.value.trim();
        if (!activeChatId || !text) {
            return;
        }

        messageInput.value = "";
        try {
            await connection.invoke("SendMessage", activeChatId, text);
        } catch {
            messageInput.value = text;
            showWelcome("Съобщението не беше изпратено.", "Проверете връзката и опитайте отново.");
        }
    });

    newChatButton?.addEventListener("click", () => setSearchPanelOpen(searchPanel.hidden));
    searchCloseButton?.addEventListener("click", () => setSearchPanelOpen(false));

    searchInput?.addEventListener("input", () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(searchContacts, 250);
    });

    document.querySelectorAll(".nb-chat-list-item").forEach(bindChatItem);
    document.querySelectorAll(".nb-chat-contact").forEach((item) => {
        item.addEventListener("click", () => startDirectChat(item.dataset.contactId));
    });

    async function start() {
        setConnectionState("Свързване", "is-warning");
        try {
            await connection.start();
            setConnectionState("Свързано", "is-online");
        } catch {
            setConnectionState("Изключено", "is-offline");
            window.setTimeout(start, 1500);
        }
    }

    updateEmptyState();
    start();
})();
