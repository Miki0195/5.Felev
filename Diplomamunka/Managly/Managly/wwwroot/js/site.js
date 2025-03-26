if (!window.chatSignalR) { 
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
        .then(groupedNotifications => {
            console.log("Received notifications:", groupedNotifications);
            
            const notificationList = document.getElementById("notificationList");
            let totalUnread = 0;
            notificationList.innerHTML = "";

            if (!groupedNotifications || groupedNotifications.length === 0) {
                notificationList.innerHTML = `
                    <div class="notification-empty">
                        <i class="fas fa-bell-slash"></i>
                        <p class="mb-0">No new notifications</p>
                    </div>
                `;
                updateNotificationIndicators(0);
                return;
            }

            // Create a container for all notifications
            const notificationsContainer = document.createElement("div");
            notificationsContainer.className = "notifications-container";

            groupedNotifications.forEach((typeGroup, index) => {
                totalUnread += typeGroup.unreadCount;

                // Create type group container
                const typeContainer = document.createElement("div");
                typeContainer.className = "notification-type-group";
                
                // Create unique ID for this group
                const groupId = `notification-group-${index}`;
                
                // Add collapsible type header
                typeContainer.innerHTML = `
                    <div class="dropdown-header d-flex justify-content-between align-items-center notification-group-header" 
                         data-bs-toggle="collapse" 
                         data-bs-target="#${groupId}" 
                         role="button"
                         aria-expanded="false">
                        <div class="d-flex align-items-center">
                            <i class="fas fa-chevron-right me-2 group-icon"></i>
                            <span>${typeGroup.groupTitle}</span>
                        </div>
                        <span class="badge bg-secondary">${typeGroup.unreadCount}</span>
                    </div>
                    <div class="collapse" id="${groupId}">
                        <div class="notification-group-content"></div>
                    </div>
                `;

                const groupContent = typeContainer.querySelector('.notification-group-content');

                if (typeGroup.senderGroups && typeGroup.senderGroups.length > 0) {
                    typeGroup.senderGroups.forEach(senderGroup => {
                        const senderContainer = createSenderGroupContainer(senderGroup, typeGroup.type);
                        groupContent.appendChild(senderContainer);
                    });
                } else if (typeGroup.notifications && typeGroup.notifications.length > 0) {
                    // Old structure with direct notifications
                    const senderContainer = document.createElement("div");
                    senderContainer.className = "notification-sender-group";
                    
                    typeGroup.notifications.forEach(notification => {
                        const notificationItem = createNotificationItem(notification, typeGroup.type);
                        senderContainer.appendChild(notificationItem);
                    });
                    
                    groupContent.appendChild(senderContainer);
                }

                notificationsContainer.appendChild(typeContainer);
            });

            notificationList.appendChild(notificationsContainer);
            updateNotificationIndicators(totalUnread);

            // Add click handlers for group headers
            document.querySelectorAll('.notification-group-header').forEach(header => {
                header.addEventListener('click', function() {
                    const icon = this.querySelector('.group-icon');
                    const isExpanded = this.getAttribute('aria-expanded') === 'true';
                    
                    // Rotate icon based on collapse state
                    icon.style.transform = isExpanded ? 'rotate(0deg)' : 'rotate(90deg)';
                    this.setAttribute('aria-expanded', !isExpanded);
                });
            });
        })
        .catch(err => {
            console.error("❌ Error loading notifications: ", err);
            const notificationList = document.getElementById("notificationList");
            notificationList.innerHTML = '<p class="text-muted text-center py-3 mb-0">Error loading notifications</p>';
            updateNotificationIndicators(0);
        });
}

function createSenderGroupContainer(senderGroup, type) {
    const senderContainer = document.createElement("div");
    senderContainer.className = "notification-sender-group";

    // Add collapsible sender header if there are multiple messages
    if (senderGroup.unreadCount > 1) {
        const groupId = `sender-group-${senderGroup.senderId}-${Date.now()}`;
        
        senderContainer.innerHTML = `
            <div class="dropdown-header d-flex justify-content-between align-items-center sender-group-header" 
                 role="button"
                 aria-expanded="false">
                <div class="d-flex align-items-center">
                    <i class="fas fa-chevron-right me-2 sender-icon"></i>
                    <small class="text-muted">
                        ${senderGroup.senderName} (${senderGroup.unreadCount})
                    </small>
                </div>
            </div>
            <div class="collapse" id="${groupId}">
                <div class="sender-group-content"></div>
            </div>
        `;

        const groupContent = senderContainer.querySelector('.sender-group-content');
        const header = senderContainer.querySelector('.sender-group-header');
        const collapseElement = senderContainer.querySelector('.collapse');
        
        // Add notifications for this sender
        senderGroup.notifications.forEach(notification => {
            const notificationItem = createNotificationItem(notification, type);
            groupContent.appendChild(notificationItem);
        });

        // Initialize Bootstrap collapse
        const collapse = new bootstrap.Collapse(collapseElement, {
            toggle: false
        });

        // Add click handler for the header
        header.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const icon = this.querySelector('.sender-icon');
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            
            if (isExpanded) {
                collapse.hide();
                icon.style.transform = 'rotate(0deg)';
                this.setAttribute('aria-expanded', 'false');
            } else {
                collapse.show();
                icon.style.transform = 'rotate(90deg)';
                this.setAttribute('aria-expanded', 'true');
            }
        });
    } else {
        // Single notification, no grouping needed
        const notificationItem = createNotificationItem(senderGroup.notifications[0], type);
        senderContainer.appendChild(notificationItem);
    }

    return senderContainer;
}

function createNotificationItem(notification, type) {
    const item = document.createElement("a");
    item.className = `dropdown-item notification-item notification-type-${type}`;
    item.href = notification.link;
    
    const timestamp = new Date(notification.timestamp).toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit'
    });

    item.innerHTML = `
        <div class="notification-content">
            <div class="notification-message">${notification.message}</div>
            ${notification.metaData?.messagePreview ? 
                `<small class="text-muted">${notification.metaData.messagePreview}</small>` : 
                ''}
            <small class="text-muted">${timestamp}</small>
        </div>
    `;

    item.onclick = (e) => {
        e.preventDefault();
        handleNotificationClick(type, notification.link, notification.relatedUserId);
    };

    return item;
}

function handleNotificationClick(type, link, senderId) {
    markNotificationAsRead(type, link);
}

function updateNotificationIndicators(count) {
    const notificationCount = document.getElementById("notificationCount");
    const notificationBell = document.getElementById("notificationBell").querySelector("i");
    const deleteAllButton = document.getElementById("deleteAllNotifications");

    notificationCount.textContent = count;
    notificationCount.classList.toggle("d-none", count === 0);
    notificationBell.classList.toggle("has-notifications", count > 0);
    deleteAllButton.classList.toggle("d-none", count === 0);
}

// SignalR notification handler
if (typeof connection !== "undefined") {
    connection.on("ReceiveNotification", function (notification) {
        console.log("🔔 New real-time notification received:", notification);

        // Check if we're on a page where we should auto-mark as read
        const currentPage = window.location.pathname;
        const shouldAutoMarkRead = (
            (currentPage.startsWith("/chat") && notification.type === "Message") ||
            (currentPage.startsWith("/projects") && notification.type.startsWith("Project")) ||
            (currentPage.startsWith("/videoconference") && notification.type === "VideoInvite")
        );

        if (shouldAutoMarkRead) {
            markNotificationAsRead(notification.type, notification.link);
        } else {
            // Refresh notifications to show the new one
            loadNotifications();
        }
    });
}

function markNotificationAsRead(type, link) {
    fetch(`/api/notifications/mark-as-read/${type}`, {
        method: "POST"
    })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                // Remove notifications of this type
                document.querySelectorAll(`[data-notification-type="${type}"]`)
                    .forEach(item => {
                        const group = item.closest('.notification-group');
                        item.remove();
                        
                        // If group is empty, remove it
                        if (group && !group.querySelector('.notification-item')) {
                            group.remove();
                        }
                    });

                // Update counts and check if all notifications are gone
                const remainingItems = document.querySelectorAll('.notification-item');
                if (remainingItems.length === 0) {
                    document.getElementById("notificationList").innerHTML = 
                        '<p class="text-muted text-center py-3 mb-0">No new notifications</p>';
                }

                updateNotificationIndicators(remainingItems.length);

                // Navigate to the link if provided
                if (link) {
                    window.location.href = link;
                }
            }
        })
        .catch(err => console.error("❌ Error marking notifications as read:", err));
}

document.addEventListener("DOMContentLoaded", function () {
    loadNotifications();
    //setInterval(loadNotifications, 2000); // Fetch every 2 seconds

    let isMessagesPage = window.location.pathname.startsWith("/chat");
    let isProjectsPage = window.location.pathname.startsWith("/projects");

    if (isMessagesPage) {
        let userId = new URLSearchParams(window.location.search).get("userId");

        if (userId) {
            fetch(`/api/notifications/mark-as-read/${userId}`, { method: "POST" })
                .then(() => console.log("✅ Automatically marked message notifications as read on chat page"))
                .catch(err => console.error("❌ Error auto-marking messages as read: ", err));
        }
    }

    if (isProjectsPage) {
        let projectId = new URLSearchParams(window.location.search).get("projectId");
        if (projectId) {
            fetch(`/api/notifications/mark-as-read/project_${projectId}`, { method: "POST" })
                .then(() => console.log("✅ Automatically marked project notifications as read"))
                .catch(err => console.error("❌ Error auto-marking project notifications as read: ", err));
        }
    }
});

// There should be way to combine these two!!!
function markNotificationsAsRead(senderId, link) {
    // First, check if this is a project notification
    if (link.includes('/projects')) {
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

                // Navigate to the project page
                window.location.href = link;
            })
            .catch(err => console.error("❌ Error marking project notifications as read: ", err));
    } else {
        // Handle other types of notifications (chat, etc.)
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
    }
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
        let isVideoPage = window.location.pathname.startsWith("/api/videoconference");

        let formattedTimestamp = new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        if (isVideoPage) {
            fetch(`/api/notifications/mark-as-read/${senderId}`, { method: "POST" })
                .then(() => console.log(`✅ Auto-marked messages from ${senderId} as read on chat page`))
                .catch(err => console.error("❌ Error auto-marking messages as read:", err));

        }
        else if (isMessagesPage) {
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



