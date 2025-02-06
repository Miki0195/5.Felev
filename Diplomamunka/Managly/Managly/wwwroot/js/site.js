if (typeof connection === "undefined") { 
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub", { withCredentials: false })
        .withAutomaticReconnect()
        .build();

    connection.start()
        .then(() => console.log("✅ SignalR Connected"))
        .catch(err => console.error("❌ SignalR Connection Error: ", err));
}

function showToast(message, type = "success") {
    let toastContainer = document.getElementById("toastContainer");

    let bgColor = type === "success" ? "bg-success" : "bg-danger";

    let toastElement = document.createElement("div");
    toastElement.classList.add("toast", "align-items-center", "text-white", bgColor, "border-0");
    toastElement.setAttribute("role", "alert");
    toastElement.setAttribute("aria-live", "assertive");
    toastElement.setAttribute("aria-atomic", "true");

    toastElement.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

    toastContainer.appendChild(toastElement);

    let toast = new bootstrap.Toast(toastElement);
    toast.show();

    toastElement.addEventListener("hidden.bs.toast", () => {
        toastElement.remove();
    });
}

function toggleNotifications() {
    let dropdown = document.getElementById("notificationDropdown");
    dropdown.classList.toggle("show");
}

function loadNotifications() {
    fetch("/api/notifications")
        .then(response => response.json())
        .then(notifications => {
            let notificationList = document.getElementById("notificationList");
            let notificationCount = document.getElementById("notificationCount");
            let notificationBell = document.getElementById("notificationBell").querySelector("i");
            let deleteAllButton = document.getElementById("deleteAllNotifications");

            notificationList.innerHTML = "";

            if (notifications.length === 0) {
                notificationList.innerHTML = "<p class='text-muted text-center'>No new notifications</p>";
                notificationCount.classList.add("d-none");
                notificationBell.classList.remove("has-notifications");
                deleteAllButton.classList.add("d-none"); 
                return;
            }

            let groupedNotifications = {};
            let totalUnread = 0;

            notifications.forEach(notification => {
                let senderId = new URLSearchParams(notification.link.split("?")[1]).get("userId");
                let serverTime = new Date(notification.timestamp);
                let localTime = new Date(serverTime.getTime() - serverTime.getTimezoneOffset() * 60000);
                let formattedTimestamp = localTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                if (!groupedNotifications[senderId]) {
                    groupedNotifications[senderId] = { messages: [], link: notification.link, isRead: notification.isRead, latestTimestamp: formattedTimestamp };
                }
                groupedNotifications[senderId].messages.push(notification.message);
                let currentLatest = new Date(groupedNotifications[senderId].latestTimestamp);
                if (serverTime > currentLatest) {
                    groupedNotifications[senderId].latestTimestamp = formattedTimestamp;
                }
            });

            Object.keys(groupedNotifications).forEach(senderId => {
                let notificationData = groupedNotifications[senderId];

                let item = document.createElement("li");
                item.classList.add("notification-item");
                item.setAttribute("data-sender-id", senderId);

                if (!notificationData.isRead) {
                    item.classList.add("unread-notification");
                    totalUnread += notificationData.messages.length;
                } else {
                    item.classList.add("read-notification");
                }

                item.innerHTML = `
                    <div class="notification-content">
                        <strong>${notificationData.messages[0]}</strong>
                        <div class="notification-timestamp text-muted small">${notificationData.latestTimestamp}</div>
                        ${notificationData.messages.length > 1 ? `<span class="message-count badge bg-danger">(${notificationData.messages.length} messages)</span>` : ""}
                    </div>
                `;

                item.onclick = function () {
                    markNotificationsAsRead(senderId, notificationData.link);
                };

                notificationList.appendChild(item);
            });

            deleteAllButton.classList.remove("d-none");

            notificationCount.textContent = totalUnread;
            notificationCount.classList.toggle("d-none", totalUnread === 0);
            notificationBell.classList.toggle("has-notifications", totalUnread > 0);
        })
        .catch(err => console.error("❌ Error loading notifications: ", err));
}


document.addEventListener("DOMContentLoaded", function () {
    loadNotifications();

    let isMessagesPage = window.location.pathname.startsWith("/chat");

    if (isMessagesPage) {
        let userId = new URLSearchParams(window.location.search).get("userId");

        if (userId) {
            fetch(`/api/notifications/mark-as-read/${userId}`, { method: "POST" })
                .then(() => console.log("✅ Automatically marked message notifications as read on chat page"))
                .catch(err => console.error("❌ Error auto-marking messages as read: ", err));
        }
    }
});

// There should be way to combine these two!!!
function markNotificationsAsRead(senderId, link) {
    fetch(`/api/notifications/mark-as-read/${senderId}`, { method: "POST" })
        .then(() => {
            document.querySelectorAll(`.notification-item[data-sender-id="${senderId}"]`).forEach(item => {
                item.dataset.updated = "false";
                item.remove();
            });

            let notificationCount = document.getElementById("notificationCount");
            let currentCount = parseInt(notificationCount.textContent, 10) || 0;
            let newCount = Math.max(0, currentCount - 1);
            notificationCount.textContent = newCount;
            notificationCount.classList.toggle("d-none", newCount === 0);

            window.location.href = link;
        })
        .catch(err => console.error("❌ Error marking notifications as read: ", err));
    fetch(`/chat/mark-as-read/${senderId}`, { method: "POST" }) // ✅ Mark messages as read in DB
        .then(() => {
            document.querySelectorAll(`.notification-item[data-sender-id="${senderId}"]`).forEach(item => {
                item.dataset.updated = "false";
                item.remove();
            });

            let notificationCount = document.getElementById("notificationCount");
            let currentCount = parseInt(notificationCount.textContent, 10) || 0;
            let newCount = Math.max(0, currentCount - 1);
            notificationCount.textContent = newCount;
            notificationCount.classList.toggle("d-none", newCount === 0);

            // ✅ Ensure recent chat is permanently marked as read
            let recentChat = document.querySelector(`.user-recent-chat[data-user-id="${senderId}"]`);
            if (recentChat) {
                recentChat.classList.remove("unread-message");
                let badge = recentChat.querySelector(".unread-badge");
                if (badge) badge.remove();
            }

            window.location.href = link;
        })
        .catch(err => console.error("❌ Error marking notifications as read: ", err));
}

if (typeof connection !== "undefined") {
    connection.on("ReceiveNotification", function (message, link, unreadCount, senderUnreadCount, timestamp) {
        console.log("🔔 New real-time notification received:", message);

        let notificationCount = document.getElementById("notificationCount");
        let notificationList = document.getElementById("notificationList");
        let notificationBell = document.getElementById("notificationBell").querySelector("i");
        let deleteAllButton = document.getElementById("deleteAllNotifications");

        let senderId = new URLSearchParams(link.split("?")[1]).get("userId");
        let isMessagesPage = window.location.pathname.startsWith("/chat");

        let formattedTimestamp = new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        if (isMessagesPage) {
            fetch(`/api/notifications/mark-as-read/${senderId}`, { method: "POST" })
                .then(() => console.log(`✅ Auto-marked messages from ${senderId} as read on chat page`))
                .catch(err => console.error("❌ Error auto-marking messages as read:", err));

        } else {
            if (notificationList.innerHTML.includes("No new notifications")) {
                notificationList.innerHTML = "";
            }

            if (typeof unreadCount !== "undefined" && !isNaN(unreadCount)) {
                notificationCount.textContent = unreadCount;
                notificationCount.classList.remove("d-none");
                notificationBell.classList.add("has-notifications");
            }

            let existingItem = document.querySelector(`.notification-item[data-sender-id="${senderId}"]`);

            if (existingItem) {
                let timestampElement = existingItem.querySelector(".notification-timestamp");
                if (timestampElement) {
                    timestampElement.textContent = formattedTimestamp; 
                } else {
                    let newTimestamp = document.createElement("div");
                    newTimestamp.classList.add("notification-timestamp", "text-muted", "small");
                    newTimestamp.textContent = formattedTimestamp;
                    existingItem.querySelector(".notification-content").appendChild(newTimestamp);
                }

                let messageCountSpan = existingItem.querySelector(".message-count");
                if (messageCountSpan) {
                    messageCountSpan.textContent = `(${senderUnreadCount} messages)`;
                } else {
                    let countSpan = document.createElement("span");
                    countSpan.classList.add("message-count", "badge", "bg-danger");
                    countSpan.textContent = `(${senderUnreadCount} messages)`;
                    existingItem.querySelector(".notification-content").appendChild(countSpan);
                }

                notificationList.prepend(existingItem);
            } else {
                let item = document.createElement("li");
                item.classList.add("notification-item", "unread-notification");
                item.setAttribute("data-sender-id", senderId);

                item.innerHTML = `
                    <div class="notification-content">
                        <strong>${message}</strong>
                        <div class="notification-timestamp text-muted small">${formattedTimestamp}</div>
                    </div>
                `;

                item.onclick = function () {
                    markNotificationsAsRead(senderId, link);
                };

                notificationList.prepend(item); 
            }

            if (notificationList.childElementCount > 0) {
                deleteAllButton.classList.remove("d-none");
            }
        }

    });
}

function deleteAllNotifications() {
    fetch("/api/notifications/mark-all-as-read", { method: "POST" })
        .then(() => {
            let notificationList = document.getElementById("notificationList");
            let notificationCount = document.getElementById("notificationCount");
            let notificationBell = document.getElementById("notificationBell").querySelector("i");
            let deleteAllButton = document.getElementById("deleteAllNotifications");

            notificationList.innerHTML = "<p class='text-muted text-center'>No new notifications</p>";
            notificationCount.textContent = "0";
            notificationCount.classList.add("d-none");
            notificationBell.classList.remove("has-notifications");
            deleteAllButton.classList.add("d-none"); 
        })
        .catch(err => console.error("❌ Error deleting notifications: ", err));
}



