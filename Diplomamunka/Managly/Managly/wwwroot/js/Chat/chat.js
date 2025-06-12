let selectedUserId = null;
let selectedMembers = new Set();
let createGroupModal;
let selectedGroupId = null;

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

document.addEventListener("DOMContentLoaded", function () {
    loadRecentChats(); // Load direct messages by default

    const urlParams = new URLSearchParams(window.location.search);
    const userId = urlParams.get("userId");
    const messageInput = document.getElementById("messageInput");
    const sendMessageBtn = document.getElementById("sendMessageBtn");
    const emojiButton = document.getElementById('emojiButton');
    const emojiPicker = document.getElementById('emojiPicker');

    // Add tab switching functionality
    document.querySelectorAll('.tab-btn').forEach(button => {
        button.addEventListener('click', () => {
            // Remove active class from all buttons and content
            document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(content => {
                content.classList.remove('active');
                content.style.display = 'none';
            });

            // Add active class to clicked button
            button.classList.add('active');

            // Show corresponding content
            const tabName = button.getAttribute('data-tab');
            const targetContent = tabName === 'direct' ? document.getElementById('recentChats') : document.getElementById('groupChats');
            targetContent.classList.add('active');
            targetContent.style.display = 'flex';

            if (tabName === 'direct') {
                loadRecentChats(); // Refresh direct messages
            } else if (tabName === 'groups') {
                loadGroupChats(); // Load group chats
            }
        });
    });

    // Trigger click on direct messages tab by default
    document.querySelector('.tab-btn[data-tab="direct"]').click();

    if (userId) {
        console.log(`✅ Opening chat with user: ${userId}`);
        selectedUserId = userId;
        loadMessages(userId);
        document.getElementById("chatBox").style.display = "block";
        messageInput.focus();
    }

    // Initialize the group modal
    createGroupModal = new bootstrap.Modal(document.getElementById('createGroupModal'));

    // Add member search functionality
    const memberSearch = document.getElementById('memberSearch');
    if (memberSearch) {
        memberSearch.addEventListener('input', debounce(searchMembers, 300));
    }

    // Send message function
    async function sendMessage() {
        const message = messageInput.value.trim();
        if (!message) return;

        if (selectedUserId) {
            // Existing direct message logic
            await sendDirectMessage(message);
        } else if (selectedGroupId) {
            // New group message logic
            await sendGroupMessage(message);
        }

        messageInput.value = '';
    }

    // Send button click event
    sendMessageBtn.addEventListener("click", sendMessage);

    // Enter key press event
    messageInput.addEventListener("keypress", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
            sendMessage();
        }
    });

    // Emoji picker functionality
    emojiButton.addEventListener('click', (e) => {
        e.stopPropagation();
        emojiPicker.style.display = emojiPicker.style.display === 'none' ? 'block' : 'none';
    });

    // Close emoji picker when clicking outside
    document.addEventListener('click', (e) => {
        if (!emojiButton.contains(e.target) &&
            !emojiPicker.contains(e.target)) {
            emojiPicker.style.display = 'none';
        }
    });

    // Handle emoji selection
    document.querySelector('emoji-picker')
        .addEventListener('emoji-click', event => {
            const cursor = messageInput.selectionStart;
            const text = messageInput.value;
            const insert = event.detail.unicode;

            messageInput.value = text.slice(0, cursor) + insert + text.slice(cursor);
            messageInput.setSelectionRange(cursor + insert.length, cursor + insert.length);
            messageInput.focus();
        });
});

document.getElementById("searchInput").addEventListener("input", function () {
    let query = this.value.trim();
    let resultsDiv = document.getElementById("searchResults");

    if (query.length === 0) {
        resultsDiv.innerHTML = "";
        return;
    }

    if (query.length > 2) {
        fetch(`/chat/search/${query}`)
            .then(response => response.json())
            .then(users => {
                resultsDiv.innerHTML = "";

                if (users.length === 0) {
                    resultsDiv.innerHTML = "<p>No users found</p>";
                    return;
                }

                users.forEach(user => {
                    let userDiv = document.createElement("div");
                    userDiv.textContent = user.fullName;
                    userDiv.classList.add("user-search-result");

                    userDiv.onclick = function () {
                        selectedUserId = user.id;
                        loadMessages(selectedUserId);
                        document.getElementById("chatBox").style.display = "block";
                        document.getElementById("messageInput").focus();
                        resultsDiv.innerHTML = "";
                    };

                    resultsDiv.appendChild(userDiv);
                });
            })
            .catch(err => console.error("Search error: ", err));
    }
});

function loadMessages(userId) {
    fetch(`/chat/messages/${userId}`)
        .then(response => response.json())
        .then(messages => {
            let chatBox = document.getElementById("chatBox");
            chatBox.innerHTML = "";

            fetch(`/chat/user/${userId}`)
                .then(response => response.json())
                .then(user => {
                    document.getElementById("chatHeader").innerHTML = `
                            <div class="chat-header-user">
                                <img src="${user.profilePicturePath || '/images/default/default-profile.png'}" alt="${user.fullName}" class="chat-header-avatar">
                                <div class="chat-header-info">
                                    <span class="chat-header-name">${user.fullName}</span>
                                    <span class="chat-header-status">${user.isOnline ? 'Online' : 'Offline'}</span>
                                </div>
                            </div>
                        `;
                })
                .catch(err => console.error("Error fetching user details: ", err));

            let lastDate = null;
            let deletedMessages = JSON.parse(localStorage.getItem("deletedMessages")) || []; // ✅ Get deleted messages

            messages.forEach(msg => {
                if (!msg.id) {
                    console.warn("⚠ Warning: Message without ID found, skipping.");
                    return;
                }

                if (deletedMessages.includes(msg.id.toString())) {
                    console.log(`⛔ Hiding deleted message ID: ${msg.id}`);
                    return; // ✅ Hide deleted messages only for the sender
                }

                let isSentByMe = msg.senderId === document.getElementById("currentUserId").value;
                let messageDate = new Date(msg.timestamp);
                let formattedDate = messageDate.toLocaleDateString("en-US", {
                    weekday: "short",
                    year: "numeric",
                    month: "short",
                    day: "numeric"
                });

                if (lastDate !== formattedDate) {
                    let dateSeparator = document.createElement("div");
                    dateSeparator.classList.add("date-separator");
                    dateSeparator.textContent = formattedDate;
                    chatBox.appendChild(dateSeparator);
                    lastDate = formattedDate;
                }

                let wrapper = document.createElement("div");
                wrapper.classList.add("message-wrapper", isSentByMe ? "sent" : "received");
                wrapper.setAttribute("data-message-id", msg.id);
                wrapper.setAttribute("data-user-id", msg.senderId);
                
                // Clear any existing children
                while (wrapper.firstChild) {
                    wrapper.removeChild(wrapper.firstChild);
                }

                let messageDiv = document.createElement("div");
                messageDiv.classList.add(isSentByMe ? "my-message" : "other-message");

                if (msg.isDeleted) {
                    messageDiv.classList.add("deleted-message");
                    messageDiv.innerHTML = `<em>Deleted message</em>`; // ✅ Only for "Delete for Both"
                } else {
                    messageDiv.textContent = msg.content;
                }

                let profileImg = document.createElement("img");
                profileImg.src = msg.senderProfilePicture || '/images/default/default-profile.png';
                profileImg.classList.add("message-profile-img");

                if (isSentByMe && !msg.isDeleted) {  // Only add click handler if message is not deleted
                    messageDiv.onclick = function () {
                        openMessageOptionsModal(msg.id, msg.content);
                    };
                }

                if (isSentByMe) {
                    // For sent messages, message first then profile image
                    wrapper.appendChild(messageDiv);
                    wrapper.appendChild(profileImg);
                } else {
                    // For received messages, profile image first then message
                    wrapper.appendChild(profileImg);
                    wrapper.appendChild(messageDiv);
                }

                chatBox.appendChild(wrapper);
                chatBox.scrollTop = chatBox.scrollHeight;
            });
        })
        .catch(err => console.error("Load messages error: ", err));
}

connection.on("ReceiveMessage", function (senderId, message, messageId) {
    let chatBox = document.getElementById("chatBox");

    if (selectedUserId === senderId) {
        let isSentByMe = senderId === document.getElementById("currentUserId").value;
        let wrapper = document.createElement("div");
        wrapper.classList.add("message-wrapper", isSentByMe ? "sent" : "received");

        // Set message ID if available
        if (messageId) {
            wrapper.setAttribute("data-message-id", messageId);
            console.log(`✅ Received message with ID: ${messageId}`);
        } else {
            console.warn("⚠️ Warning: Received message without ID");
        }

        wrapper.setAttribute("data-user-id", senderId);
        
        // Clear any existing children
        while (wrapper.firstChild) {
            wrapper.removeChild(wrapper.firstChild);
        }

        let messageDiv = document.createElement("div");
        messageDiv.classList.add(isSentByMe ? "my-message" : "other-message");
        messageDiv.textContent = message;

        if (isSentByMe && messageId) {
            messageDiv.onclick = function () {
                openMessageOptionsModal(messageId, message);
            };
        }

        fetch(`/chat/user/${senderId}`)
            .then(response => response.json())
            .then(user => {
                let profileImg = document.createElement("img");
                profileImg.src = user.profilePicturePath || '/images/default/default-profile.png';
                profileImg.classList.add("message-profile-img");

                if (isSentByMe) {
                    // For sent messages, message first then profile image
                    wrapper.appendChild(messageDiv);
                    wrapper.appendChild(profileImg);
                } else {
                    // For received messages, profile image first then message
                    wrapper.appendChild(profileImg);
                    wrapper.appendChild(messageDiv);
                }

                chatBox.appendChild(wrapper);
                chatBox.scrollTop = chatBox.scrollHeight;
            })
            .catch(err => console.error("Error fetching user details: ", err));

        // Mark messages as read when they are displayed
        if (!isSentByMe) {
            fetch(`/chat/mark-as-read/${senderId}`, { method: "POST" })
                .then(() => {
                    // Update both old and new style chat items
                    let recentChat = document.querySelector(`.chat-user-item[data-user-id="${senderId}"], .user-recent-chat[data-user-id="${senderId}"]`);
                    if (recentChat) {
                        recentChat.classList.remove("unread-message");
                        let badge = recentChat.querySelector(".unread-badge");
                        if (badge) badge.remove();
                    }
                })
                .catch(err => console.error("❌ Error marking messages as read: ", err));
        }
    }

    // Always update the recent chat with the new message
    // But use a small delay to prevent race conditions
    setTimeout(() => {
        updateRecentChat(senderId, message, false);
    }, 100);
});

if (typeof connection !== "undefined") {
    connection.on("MessageDeleted", function (messageId) {

        applyDeletedMessageStyle(messageId);

        // ✅ Update recent chat preview for sender & receiver
        updateRecentChat(selectedUserId, "Deleted message", true);
    });
}

if (typeof connection !== "undefined") {
    connection.on("MessageDeletedForMe", function (messageId) {
        let messageElement = document.querySelector(`.message-wrapper[data-message-id="${messageId}"]`);
        if (messageElement) {
            messageElement.remove();

            // ✅ Store in LocalStorage so it stays deleted after reload
            let deletedMessages = JSON.parse(localStorage.getItem("deletedMessages")) || [];
            if (!deletedMessages.includes(messageId.toString())) {
                deletedMessages.push(messageId.toString());
                localStorage.setItem("deletedMessages", JSON.stringify(deletedMessages));
            }
        }
    });
}

function loadRecentChats() {
    console.log("Loading recent chats...");
    const recentChatsContainer = document.querySelector("#recentChats");
    recentChatsContainer.innerHTML = "<div class='loading-spinner'><div class='spinner-border text-primary' role='status'><span class='visually-hidden'>Loading...</span></div></div>";

    fetch(`/chat/recent-chats`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(users => {
            console.log("Recent chats data:", users);
            recentChatsContainer.innerHTML = ""; // Clear loading spinner

            if (!users || users.length === 0) {
                recentChatsContainer.innerHTML = "<p class='no-chats-message'>No recent chats</p>";
                return;
            }

            users.forEach(user => {
                console.log("Processing user:", user);

                // Check if lastMessage exists and has expected properties
                if (user.lastMessage) {
                    console.log("User has lastMessage:", user.lastMessage);
                } else {
                    console.log("User has no lastMessage");
                }

                const lastMessageText = user.lastMessage ?
                    (user.lastMessage.isFromUser ? "You: " : "") + user.lastMessage.content :
                    "No messages yet";

                const lastMessageTime = user.lastMessage && user.lastMessage.timestamp ?
                    formatChatTime(new Date(user.lastMessage.timestamp)) : "";

                const unreadBadge = user.unreadCount > 0 ?
                    `<span class="unread-badge">${user.unreadCount}</span>` : "";

                const onlineStatus = user.isOnline ?
                    "<span class='online-indicator'></span>" : "";

                const userDiv = document.createElement("div");
                userDiv.className = "chat-user-item";
                userDiv.innerHTML = `
                        <div class="user-avatar">
                            <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}">
                            ${onlineStatus}
                        </div>
                        <div class="user-info">
                            <div class="user-name-row">
                                <span class="user-name">${user.fullName}</span>
                                <span class="last-message-time">${lastMessageTime}</span>
                            </div>
                            <div class="last-message-row">
                                <span class="last-message-text ${user.lastMessage && user.lastMessage.isDeleted ? 'deleted-message' : ''}">${lastMessageText}</span>
                                ${unreadBadge}
                            </div>
                        </div>
                    `;

                userDiv.onclick = function () {
                    document.querySelectorAll('.chat-user-item').forEach(el => el.classList.remove('active'));
                    this.classList.add('active');
                    selectedUserId = user.id;
                    loadMessages(selectedUserId);

                    document.getElementById("chatHeader").innerHTML = `
                            <div class="chat-header-user">
                                <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}" class="chat-header-avatar">
                                <div class="chat-header-info">
                                    <span class="chat-header-name">${user.fullName}</span>
                                    <span class="chat-header-status">${user.isOnline ? 'Online' : 'Offline'}</span>
                                </div>
                            </div>
                        `;

                    document.getElementById("chatBox").style.display = "block";
                    document.getElementById("messageInput").focus();

                    if (this.classList.contains('unread-message')) {
                        markMessagesAsRead(user.id);
                        this.classList.remove('unread-message');
                        const badge = this.querySelector('.unread-badge');
                        if (badge) badge.remove();
                    }
                };

                recentChatsContainer.appendChild(userDiv);
            });
        })
        .catch(err => {
            console.error("❌ Error loading recent chats:", err);
            recentChatsContainer.innerHTML = "<p class='error-message'>Could not load chats. Please try again later.</p>";
        });
}

function updateRecentChat(userId, newMessage, isSentByMe) {
    let recentChatsDiv = document.getElementById("recentChats");
    let messageText = isSentByMe ? `You: ${newMessage}` : newMessage;
    let truncatedMessage = messageText.length > 30 ? messageText.substring(0, 27) + "..." : messageText;

    // First, remove ALL duplicate chat items for this user
    const existingItems = document.querySelectorAll(`.chat-user-item[data-user-id="${userId}"], .user-recent-chat[data-user-id="${userId}"]`);
    if (existingItems.length > 0) {
        // Keep only the first one (we'll update it) and remove the rest
        for (let i = 1; i < existingItems.length; i++) {
            existingItems[i].remove();
        }

        // Get the remaining item
        let existingChat = existingItems[0];

        // If it's the old style, transform it to new style
        if (existingChat.classList.contains('user-recent-chat')) {
            fetch(`/chat/user/${userId}`)
                .then(response => response.json())
                .then(user => {
                    // Create new style chat item
                    const onlineStatus = user.isOnline ? "<span class='online-indicator'></span>" : "";
                    const unreadBadge = selectedUserId !== userId ?
                        `<span class="unread-badge">●</span>` : "";

                    const newChatItem = document.createElement("div");
                    newChatItem.className = "chat-user-item";
                    newChatItem.setAttribute("data-user-id", userId);

                    if (existingChat.classList.contains('unread-message')) {
                        newChatItem.classList.add('unread-message');
                    }

                    // Get current timestamp for message
                    const currentTime = formatChatTime(new Date());

                    newChatItem.innerHTML = `
                            <div class="user-avatar">
                                <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}">
                                ${onlineStatus}
                            </div>
                            <div class="user-info">
                                <div class="user-name-row">
                                    <span class="user-name">${user.fullName}</span>
                                    <span class="last-message-time">${currentTime}</span>
                                </div>
                                <div class="last-message-row">
                                    <span class="last-message-text ${newMessage === "Deleted message" ? 'deleted-message' : ''}">${truncatedMessage}</span>
                                    ${unreadBadge}
                                </div>
                            </div>
                        `;

                    newChatItem.onclick = function () {
                        document.querySelectorAll('.chat-user-item').forEach(el => el.classList.remove('active'));
                        this.classList.add('active');
                        selectedUserId = userId;
                        loadMessages(selectedUserId);

                        document.getElementById("chatHeader").innerHTML = `
                                <div class="chat-header-user">
                                    <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}" class="chat-header-avatar">
                                    <div class="chat-header-info">
                                        <span class="chat-header-name">${user.fullName}</span>
                                        <span class="chat-header-status">${user.isOnline ? 'Online' : 'Offline'}</span>
                                    </div>
                                </div>
                            `;

                        document.getElementById("chatBox").style.display = "block";
                        document.getElementById("messageInput").focus();

                        if (this.classList.contains('unread-message')) {
                            markMessagesAsRead(userId);
                            this.classList.remove('unread-message');
                            const badge = this.querySelector('.unread-badge');
                            if (badge) badge.remove();
                        }
                    };

                    // Replace old chat item with new one
                    existingChat.replaceWith(newChatItem);

                    // Move to top
                    if (recentChatsDiv.firstChild && recentChatsDiv.firstChild !== newChatItem) {
                        recentChatsDiv.insertBefore(newChatItem, recentChatsDiv.firstChild);
                    }
                })
                .catch(err => console.error("❌ Error updating chat item:", err));
        } else {
            // It's already the new style, just update the message
            const lastMessageElement = existingChat.querySelector('.last-message-text');
            if (lastMessageElement) {
                if (newMessage === "Deleted message") {
                    lastMessageElement.classList.add('deleted-message');
                    lastMessageElement.innerHTML = `<em>${truncatedMessage}</em>`;
                } else {
                    lastMessageElement.classList.remove('deleted-message');
                    lastMessageElement.textContent = truncatedMessage;
                }
            }

            // Update timestamp
            const timeElement = existingChat.querySelector('.last-message-time');
            if (timeElement) {
                timeElement.textContent = formatChatTime(new Date());
            }

            // Update unread status
            if (selectedUserId === userId) {
                existingChat.classList.remove('unread-message');
                const badge = existingChat.querySelector('.unread-badge');
                if (badge) badge.remove();

                fetch(`/chat/mark-as-read/${userId}`, { method: "POST" })
                    .catch(err => console.error("❌ Error marking chat as read: ", err));
            } else {
                existingChat.classList.add('unread-message');
                let badge = existingChat.querySelector('.unread-badge');
                if (!badge) {
                    const lastMessageRow = existingChat.querySelector('.last-message-row');
                    if (lastMessageRow) {
                        badge = document.createElement('span');
                        badge.className = 'unread-badge';
                        badge.textContent = '●';
                        lastMessageRow.appendChild(badge);
                    }
                }
            }

            // Move to top if not already at the top
            if (recentChatsDiv.firstChild && recentChatsDiv.firstChild !== existingChat) {
                recentChatsDiv.insertBefore(existingChat, recentChatsDiv.firstChild);
            }
        }
    } else {
        // No existing chat, create a new one in the new style
        fetch(`/chat/user/${userId}`)
            .then(response => response.json())
            .then(user => {
                const onlineStatus = user.isOnline ?
                    "<span class='online-indicator'></span>" : "";

                const unreadBadge = selectedUserId !== userId ?
                    '<span class="unread-badge">●</span>' : "";

                const currentTime = formatChatTime(new Date());

                const newChatItem = document.createElement("div");
                newChatItem.className = "chat-user-item";
                if (selectedUserId !== userId) {
                    newChatItem.classList.add('unread-message');
                }
                newChatItem.setAttribute("data-user-id", userId);

                newChatItem.innerHTML = `
                        <div class="user-avatar">
                            <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}">
                            ${onlineStatus}
                        </div>
                        <div class="user-info">
                            <div class="user-name-row">
                                <span class="user-name">${user.fullName}</span>
                                <span class="last-message-time">${currentTime}</span>
                            </div>
                            <div class="last-message-row">
                                <span class="last-message-text ${newMessage === "Deleted message" ? 'deleted-message' : ''}">${truncatedMessage}</span>
                                ${unreadBadge}
                            </div>
                        </div>
                    `;

                newChatItem.onclick = function () {
                    document.querySelectorAll('.chat-user-item').forEach(el => el.classList.remove('active'));
                    this.classList.add('active');
                    selectedUserId = userId;
                    loadMessages(selectedUserId);

                    document.getElementById("chatHeader").innerHTML = `
                            <div class="chat-header-user">
                                <img src="${user.profilePicturePath || '/img/default-avatar.png'}" alt="${user.fullName}" class="chat-header-avatar">
                                <div class="chat-header-info">
                                    <span class="chat-header-name">${user.fullName}</span>
                                    <span class="chat-header-status">${user.isOnline ? 'Online' : 'Offline'}</span>
                                </div>
                            </div>
                        `;

                    document.getElementById("chatBox").style.display = "block";
                    document.getElementById("messageInput").focus();

                    if (this.classList.contains('unread-message')) {
                        markMessagesAsRead(userId);
                        this.classList.remove('unread-message');
                        const badge = this.querySelector('.unread-badge');
                        if (badge) badge.remove();
                    }
                };

                // Add to the beginning of the list
                if (recentChatsDiv.firstChild) {
                    recentChatsDiv.insertBefore(newChatItem, recentChatsDiv.firstChild);
                } else {
                    recentChatsDiv.appendChild(newChatItem);
                }
            })
            .catch(err => console.error("❌ Error creating new chat item:", err));
    }
}

function openMessageOptionsModal(messageId, messageText) {
    let modal = new bootstrap.Modal(document.getElementById("messageOptionsModal"));

    document.getElementById("modalMessagePreview").textContent = messageText;

    document.getElementById("deleteForMeBtn").onclick = function () {
        deleteMessageForMe(messageId);
        modal.hide();
    };

    document.getElementById("deleteForEveryoneBtn").onclick = function () {
        deleteMessageForEveryone(messageId);
        modal.hide();
    };

    modal.show();
}

function deleteMessageForMe(messageId) {
    console.error("deleteMessageForMe method called.");
    let userId = document.getElementById("currentUserId").value;
    let messageElement = document.querySelector(`.message-wrapper[data-message-id="${messageId}"]`);

    if (messageElement) {
        messageElement.remove();
        console.error("message removed");

        let deletedMessages = JSON.parse(localStorage.getItem("deletedMessages")) || [];
        if (!deletedMessages.includes(messageId.toString())) {
            deletedMessages.push(messageId.toString());
            localStorage.setItem("deletedMessages", JSON.stringify(deletedMessages));
        }

        fetch(`/chat/messages/delete-for-me/${messageId}`, { method: "POST" })
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    console.error(`✅ Sending delete update to SignalR for message ${messageId}`);
                    console.error(messageId);
                    console.error(userId);
                    connection.invoke("MessageDeletedForMe", messageId, userId)
                        .then(response => {
                            console.error(`✅ SignalR Delete Response: ${response}`);
                        })
                        .catch(err => {
                            console.error("❌ Error sending delete update:", err);
                        });

                } else {
                    console.error("❌ Server Error deleting for me:", result);
                }
            })
            .catch(err => console.error("❌ Server error deleting for me: ", err));
    }
}

function deleteMessageForEveryone(messageId) {
    if (!messageId) {
        console.error("❌ Error: Message ID is undefined.");
        return;
    }

    fetch(`/chat/messages/delete/${messageId}`, { method: "DELETE" })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                let messageElement = document.querySelector(`.message-wrapper[data-message-id="${messageId}"]`);
                if (messageElement) {
                    let messageDiv = messageElement.querySelector(".my-message, .other-message");
                    if (messageDiv) {
                        messageDiv.classList.add("deleted-message");
                        messageDiv.innerHTML = `<em>Deleted message</em>`;
                    }
                }

                applyDeletedMessageStyle(messageId);

                connection.invoke("MessageDeleted", messageId)
                    .catch(err => console.error("❌ Error sending delete update:", err));

                updateRecentChat(selectedUserId, "Deleted message", true);
            } else {
                console.error("❌ Error deleting message on both ends:", result);
            }
        })
        .catch(err => console.error("❌ Server Error:", err));
}

connection.on("MessageDeleted", function (messageId) {
    applyDeletedMessageStyle(messageId);
});

function applyDeletedMessageStyle(messageId) {
    let messageElement = document.querySelector(`.message-wrapper[data-message-id="${messageId}"]`);
    if (messageElement) {
        let messageDiv = messageElement.querySelector(".my-message, .other-message");
        if (messageDiv) {
            messageDiv.classList.add("deleted-message");
            messageDiv.innerHTML = `<em>Deleted message</em>`;
        }
    }
}

connection.on("UserOnlineStatusChanged", function (userId, isOnline) {
    if (selectedUserId === userId) {
        // Update status indicator
        const statusIndicator = document.querySelector('.online-status');
        if (statusIndicator) {
            statusIndicator.className = `online-status ${isOnline ? 'online' : 'offline'}`;
        }

        // Update status text
        const statusText = document.querySelector('.user-status-text');
        if (statusText) {
            statusText.textContent = isOnline ? 'Online' : 'Offline';
        }
    }

    // Update status in recent chats if exists
    const recentChat = document.querySelector(`.user-recent-chat[data-user-id="${userId}"]`);
    if (recentChat) {
        const statusDot = recentChat.querySelector('.status-dot') ||
            document.createElement('div');
        statusDot.className = `status-dot ${isOnline ? 'online' : 'offline'}`;
        if (!recentChat.querySelector('.status-dot')) {
            recentChat.appendChild(statusDot);
        }
    }
});

function openCreateGroupModal() {
    selectedMembers.clear();
    document.getElementById('selectedMembers').innerHTML = '';
    document.getElementById('groupName').value = '';
    createGroupModal.show();
}

async function searchMembers(event) {
    const searchTerm = event.target.value.trim();
    const resultsContainer = document.getElementById('memberSearchResults');

    if (searchTerm.length < 2) {
        resultsContainer.classList.remove('active');
        return;
    }

    try {
        const response = await fetch(`/chat/search/${searchTerm}`);
        const users = await response.json();

        resultsContainer.innerHTML = '';
        users.forEach(user => {
            if (!selectedMembers.has(user.id)) {
                const div = document.createElement('div');
                div.className = 'user-search-result';
                div.innerHTML = `
                        <img src="${user.profilePicturePath || '/images/default/default-profile.png'}" alt="${user.fullName}">
                        <span>${user.fullName}</span>
                    `;
                div.onclick = () => addMember(user);
                resultsContainer.appendChild(div);
            }
        });

        resultsContainer.classList.add('active');
    } catch (error) {
        console.error('Error searching members:', error);
    }
}

function addMember(user) {
    if (selectedMembers.has(user.id)) return;

    selectedMembers.add(user.id);
    const memberElement = document.createElement('div');
    memberElement.className = 'selected-member';
    memberElement.innerHTML = `
            <img src="${user.profilePicturePath || '/images/default/default-profile.png'}" alt="${user.fullName}">
            <span>${user.fullName}</span>
            <button class="remove-member" onclick="removeMember('${user.id}', this)">
                <i class="bi bi-x"></i>
            </button>
        `;

    document.getElementById('selectedMembers').appendChild(memberElement);
    document.getElementById('memberSearchResults').classList.remove('active');
    document.getElementById('memberSearch').value = '';
}

function removeMember(userId, button) {
    selectedMembers.delete(userId);
    button.closest('.selected-member').remove();
}

async function createGroup() {
    const groupName = document.getElementById('groupName').value.trim();

    if (!groupName) {
        showToast('Please enter a group name', 'error');
        return;
    }

    if (selectedMembers.size < 2) {
        showToast('Please add at least 2 members', 'error');
        return;
    }

    try {
        const response = await fetch('/chat/groups/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                name: groupName,
                members: Array.from(selectedMembers)
            })
        });

        const result = await response.json();

        if (result.success) {
            createGroupModal.hide();
            showToast('Group created successfully!', 'success');
            loadGroupChats();
        } else {
            showToast(result.message || 'Failed to create group', 'error');
        }
    } catch (error) {
        console.error('Error creating group:', error);
        showToast('Failed to create group', 'error');
    }
}

async function loadGroupChats() {
    try {
        const response = await fetch('/chat/groups');
        const groups = await response.json();

        const container = document.getElementById('groupChats');
        container.innerHTML = '<h3>Group Chats</h3>';

        const currentUserId = document.getElementById('currentUserId').value;

        groups.forEach(group => {
            const div = document.createElement('div');
            div.className = 'user-recent-chat';
            if (group.hasUnreadMessages) {
                div.classList.add('unread-message');
            }
            div.setAttribute('data-group-id', group.id);

            const groupIcon = document.createElement('img');
            groupIcon.src = '/images/default/default-profile.png';
            groupIcon.className = 'recent-chat-img';

            const textWrapper = document.createElement('div');
            textWrapper.className = 'recent-chat-text';

            const nameDiv = document.createElement('div');
            nameDiv.className = 'user-name group-name-container';

            // Add edit icon if user is the creator
            if (group.createdById === currentUserId) {
                nameDiv.innerHTML = `
                        <span style="cursor: pointer;" onclick="openGroupChat(${group.id})">${group.name}</span>
                    `;
            } else {
                nameDiv.innerHTML = `
                        <span style="cursor: pointer;" onclick="openGroupChat(${group.id})">${group.name}</span>
                    `;
            }

            const lastMessageDiv = document.createElement('div');
            lastMessageDiv.className = 'message-preview';

            if (group.lastMessage) {
                if (group.lastMessageSenderId === currentUserId) {
                    lastMessageDiv.textContent = `You: ${group.lastMessage}`;
                } else {
                    lastMessageDiv.textContent = group.lastMessage;
                }
            } else {
                lastMessageDiv.textContent = 'No messages yet';
            }

            textWrapper.appendChild(nameDiv);
            textWrapper.appendChild(lastMessageDiv);

            div.appendChild(groupIcon);
            div.appendChild(textWrapper);

            if (group.hasUnreadMessages) {
                const unreadBadge = document.createElement('span');
                unreadBadge.className = 'unread-badge';
                unreadBadge.textContent = '●';
                div.appendChild(unreadBadge);
            }

            div.onclick = () => openGroupChat(group.id);
            container.appendChild(div);
        });
    } catch (error) {
        console.error('Error loading group chats:', error);
    }
}

async function openGroupChat(groupId) {
    try {
        selectedGroupId = groupId;
        selectedUserId = null;

        await fetch(`/chat/groups/${groupId}/read`, { method: 'POST' });

        const groupChatItem = document.querySelector(`.user-recent-chat[data-group-id="${groupId}"]`);
        if (groupChatItem) {
            groupChatItem.classList.remove('unread-message');
            const badge = groupChatItem.querySelector('.unread-badge');
            if (badge) badge.remove();
        }

        const response = await fetch(`/chat/groups/${groupId}`);
        const groupData = await response.json();

        const currentUserId = document.getElementById('currentUserId').value;
        const isCreator = groupData.createdById === currentUserId;
        console.log(currentUserId);
        console.log(groupData.createdById);
        console.log(isCreator);

        document.getElementById('chatHeader').innerHTML = `
                <div class="group-chat-header">
                    ${isCreator ?
                `<h4 style="display: flex; align-items: center; gap: 10px;">
                            ${groupData.name}
                            <i class="bi bi-pencil-square edit-group-name" 
                               style="cursor: pointer; font-size: 0.8em;" 
                               onclick="openEditGroupName(event, ${groupId}, '${groupData.name}')"></i>
                        </h4>` :
                `<h4>${groupData.name}</h4>`
            }
                    <small class="member-count" onclick="showGroupMembers(${groupId})">${groupData.memberCount} members</small>
                </div>
            `;

        const chatBox = document.getElementById('chatBox');
        chatBox.style.display = 'block';
        chatBox.innerHTML = '';

        let lastDate = null;

        if (groupData.messages) {
            for (const message of groupData.messages) {
                const messageDate = new Date(message.timestamp);
                const formattedDate = messageDate.toLocaleDateString("en-US", {
                    weekday: "short",
                    year: "numeric",
                    month: "short",
                    day: "numeric"
                });

                if (lastDate !== formattedDate) {
                    const dateSeparator = document.createElement("div");
                    dateSeparator.classList.add("date-separator");
                    dateSeparator.textContent = formattedDate;
                    chatBox.appendChild(dateSeparator);
                    lastDate = formattedDate;
                }

                const isCurrentUser = message.senderId === document.getElementById('currentUserId').value;
                const wrapper = document.createElement('div');
                wrapper.className = `message-wrapper ${isCurrentUser ? 'sent' : 'received'}`;
                wrapper.setAttribute('data-message-id', message.id);

                const messageContentWrapper = document.createElement('div');
                messageContentWrapper.className = 'message-content-wrapper';

                const messageContentInner = document.createElement('div');
                messageContentInner.className = 'message-content-inner';

                if (!isCurrentUser) {
                    const senderName = document.createElement('div');
                    senderName.className = 'message-sender-name';
                    senderName.textContent = message.senderName;
                    messageContentWrapper.appendChild(senderName);
                }

                const senderImg = document.createElement('img');
                senderImg.src = message.senderProfilePicture || '/images/default/default-profile.png';
                senderImg.className = 'message-profile-img';

                const messageDiv = document.createElement('div');
                messageDiv.className = isCurrentUser ? 'my-message' : 'other-message';

                if (message.isDeleted) {
                    messageDiv.classList.add('deleted-message');
                    messageDiv.innerHTML = '<em>Deleted message</em>';
                } else {
                    messageDiv.textContent = message.content;
                }

                if (isCurrentUser) {
                    // For sent messages (current user), place profile pic on right
                    messageContentInner.appendChild(messageDiv);
                    messageContentInner.appendChild(senderImg);
                } else {
                    // For received messages (other users), place profile pic on left
                    messageContentInner.appendChild(senderImg);
                    messageContentInner.appendChild(messageDiv);
                }

                messageContentWrapper.appendChild(messageContentInner);
                wrapper.appendChild(messageContentWrapper);
                chatBox.appendChild(wrapper);
            }
            chatBox.scrollTop = chatBox.scrollHeight;
        }
    } catch (error) {
        console.error('Error opening group chat:', error);
        showToast('Failed to load group chat', 'error');
    }
}

// Add this new function for sending group messages
async function sendGroupMessage(content) {
    try {
        const response = await fetch('/chat/groups/message', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                groupId: selectedGroupId,
                content: content
            })
        });

        if (!response.ok) {
            throw new Error('Failed to send message');
        }

        // Message will be added to the chat through SignalR
    } catch (error) {
        console.error('Error sending group message:', error);
        showToast('Failed to send message', 'error');
    }
}

// Add this to your existing SignalR connection.on handlers
connection.on("ReceiveGroupMessage", async function (message) {
    const currentUserId = document.getElementById('currentUserId').value;

    if (message.senderId !== currentUserId && selectedGroupId !== message.groupId) {
        const groupChatItem = document.querySelector(`.user-recent-chat[data-group-id="${message.groupId}"]`);
        if (groupChatItem) {
            groupChatItem.classList.add('unread-message');

            if (!groupChatItem.querySelector('.unread-badge')) {
                const unreadBadge = document.createElement('span');
                unreadBadge.className = 'unread-badge';
                unreadBadge.textContent = '●';
                groupChatItem.appendChild(unreadBadge);
            }

            const messagePreview = groupChatItem.querySelector('.message-preview');
            if (messagePreview) {
                messagePreview.textContent = message.content;
            }
        }
    }

    if (selectedGroupId === message.groupId) {
        const isCurrentUser = message.senderId === currentUserId;
        const wrapper = document.createElement('div');
        wrapper.className = `message-wrapper ${isCurrentUser ? 'sent' : 'received'}`;
        wrapper.setAttribute('data-message-id', message.messageId);

        const messageContentWrapper = document.createElement('div');
        messageContentWrapper.className = 'message-content-wrapper';

        const messageContentInner = document.createElement('div');
        messageContentInner.className = 'message-content-inner';

        const messageDiv = document.createElement('div');
        messageDiv.className = isCurrentUser ? 'my-message' : 'other-message';
        messageDiv.textContent = message.content;

        if (!isCurrentUser) {
            const senderName = document.createElement('div');
            senderName.className = 'message-sender-name';
            senderName.textContent = message.senderName;
            messageContentWrapper.appendChild(senderName);
        }

        // Fetch the sender's profile picture
        try {
            const userResponse = await fetch(`/chat/user/${message.senderId}`);
            const userData = await userResponse.json();

            const senderImg = document.createElement('img');
            senderImg.src = userData.profilePicturePath || '/images/default/default-profile.png';
            senderImg.className = 'message-profile-img';

            if (isCurrentUser) {
                // For sent messages (current user), place profile pic on right
                messageContentInner.appendChild(messageDiv);
                messageContentInner.appendChild(senderImg);
            } else {
                // For received messages (other users), place profile pic on left
                messageContentInner.appendChild(senderImg);
                messageContentInner.appendChild(messageDiv);
            }

            messageContentWrapper.appendChild(messageContentInner);
            wrapper.appendChild(messageContentWrapper);
            document.getElementById('chatBox').appendChild(wrapper);
            scrollToBottom();
        } catch (error) {
            console.error('Error fetching user profile:', error);
        }
    }
});

// Add this function to handle direct messages
async function sendDirectMessage(message) {
    const senderId = document.getElementById("currentUserId").value;

    if (!selectedUserId) {
        showToast("Select a user to chat with first!", "error");
        return;
    }

    try {
        const userResponse = await fetch(`/chat/user/${senderId}`);
        const user = await userResponse.json();

        let chatBox = document.getElementById("chatBox");
        let wrapper = document.createElement("div");
        wrapper.classList.add("message-wrapper", "sent");

        let messageDiv = document.createElement("div");
        messageDiv.classList.add("my-message");
        messageDiv.textContent = message;

        let profileImg = document.createElement("img");
        profileImg.src = user.profilePicturePath || '/images/default/default-profile.png';
        profileImg.classList.add("message-profile-img");

        // Clear existing children to prevent duplication
        while (wrapper.firstChild) {
            wrapper.removeChild(wrapper.firstChild);
        }
        
        // Add elements in the correct order - for sent messages, message first then profile pic
        wrapper.appendChild(messageDiv);
        wrapper.appendChild(profileImg);
        
        chatBox.appendChild(wrapper);
        chatBox.scrollTop = chatBox.scrollHeight;

        const response = await fetch("/chat/messages", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                senderId: senderId,
                receiverId: selectedUserId,
                content: message
            })
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error || "Failed to send message");
        }

        // Check for messageId in the response
        if (!result.messageId) {
            console.warn("⚠️ Warning: No message ID returned from server");
        } else {
            console.log(`✅ Message saved with ID: ${result.messageId}`);

            // Set the message ID attribute on the wrapper
            wrapper.setAttribute("data-message-id", result.messageId);
            wrapper.setAttribute("data-user-id", senderId);

            // Add click handler for message options
            messageDiv.onclick = function () {
                openMessageOptionsModal(result.messageId, message);
            };
        }

        // Make sure we update the recent chat list with the new message
        // But delay it slightly to ensure we don't create duplicates
        setTimeout(() => {
            updateRecentChat(selectedUserId, message, true);
        }, 100);

        connection.invoke("SendMessage", senderId, selectedUserId, message)
            .catch(err => console.error("❌ Error calling SendMessage:", err));

    } catch (err) {
        console.error("❌ Error saving message: ", err);
        showToast("Failed to send message", "error");
    }
}

// Add these new functions for handling group name editing and member list
function openEditGroupName(event, groupId, currentName) {
    event.stopPropagation();
    const modal = new bootstrap.Modal(document.getElementById('editGroupNameModal'));
    document.getElementById('newGroupName').value = currentName;
    document.getElementById('newGroupName').setAttribute('data-group-id', groupId);
    modal.show();
}

async function saveGroupName() {
    const input = document.getElementById('newGroupName');
    const groupId = input.getAttribute('data-group-id');
    const newName = input.value.trim();

    if (!newName) {
        showToast('Group name cannot be empty', 'error');
        return;
    }

    try {
        const response = await fetch(`/chat/groups/${groupId}/name`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name: newName })
        });

        if (response.ok) {
            const currentUserId = document.getElementById('currentUserId').value;
            bootstrap.Modal.getInstance(document.getElementById('editGroupNameModal')).hide();

            // Update group name in the list
            const groupItem = document.querySelector(`.user-recent-chat[data-group-id="${groupId}"]`);
            if (groupItem) {
                const nameDiv = groupItem.querySelector('.user-name');
                if (nameDiv) {
                    nameDiv.innerHTML = `
                            <span style="cursor: pointer;" onclick="openGroupChat(${groupId})">${newName}</span>
                            <i class="bi bi-pencil-square edit-group-name" onclick="openEditGroupName(event, ${groupId}, '${newName}')"></i>
                        `;
                }
            }

            // Update group name in the chat header if it's open
            if (selectedGroupId === parseInt(groupId)) {
                document.querySelector('.group-chat-header h4').innerHTML = `
                        ${newName}
                        <i class="bi bi-pencil-square edit-group-name" 
                           style="cursor: pointer; font-size: 0.8em; margin-left: 10px;" 
                           onclick="openEditGroupName(event, ${groupId}, '${newName}')"></i>
                    `;
            }

            showToast('Group name updated successfully', 'success');
        } else {
            showToast('Failed to update group name', 'error');
        }
    } catch (error) {
        console.error('Error updating group name:', error);
        showToast('Failed to update group name', 'error');
    }
}

async function showGroupMembers(groupId) {
    try {
        const response = await fetch(`/chat/groups/${groupId}`);
        const groupData = await response.json();

        const membersList = document.getElementById('groupMembersList');
        membersList.innerHTML = '';

        const currentUserId = document.getElementById('currentUserId').value;
        const isCreator = currentUserId === groupData.createdById;
        const isAdmin = groupData.currentUserRole.isAdmin;
        console.log(isAdmin);

        groupData.members.forEach(member => {
            const isGroupCreator = member.id === groupData.createdById;
            const isMemberAdmin = isGroupCreator || member.isAdmin;
            const canModifyAdmin = isCreator && !isGroupCreator;
            const canRemoveMember =
                // First check if current user has permission to remove members
                (isCreator || isAdmin) &&
                // Cannot remove the group creator under any circumstances
                !isGroupCreator &&
                // Cannot remove yourself
                member.id !== currentUserId &&
                // Only creator can remove admins
                (!member.isAdmin || isCreator);

            const memberDiv = document.createElement('div');
            memberDiv.className = 'group-member-item';
            memberDiv.innerHTML = `
                    <img src="${member.profilePicturePath || '/images/default/default-profile.png'}" alt="${member.fullName}">
                    <span>${member.fullName}</span>
                    ${isGroupCreator ?
                    '<span class="admin-badge creator">Creator</span>' :
                    (isMemberAdmin ? '<span class="admin-badge">Admin</span>' : '')}
                    <div class="member-actions">
                        ${canModifyAdmin ?
                    `<button class="admin-toggle-btn" onclick="toggleAdminRole(event, ${groupId}, '${member.id}', ${isMemberAdmin})">
                                ${isMemberAdmin ? 'Remove Admin' : 'Make Admin'}
                            </button>` :
                    ''}
                        ${(canRemoveMember) ?
                    `<button class="remove-member-btn" onclick="removeMemberFromGroup(${groupId}, '${member.id}')">
                                <i class="bi bi-x-circle"></i>
                            </button>` :
                    ''}
                    </div>
                `;
            membersList.appendChild(memberDiv);
        });

        // Add member management buttons for admin
        if (isCreator || isAdmin) {
            const managementDiv = document.createElement('div');
            managementDiv.className = 'member-management';
            managementDiv.innerHTML = `
                    <button class="btn btn-primary mt-3" onclick="openAddMembersModal(${groupId})">
                        <i class="bi bi-person-plus"></i> Add Members
                    </button>
                `;
            membersList.appendChild(managementDiv);
        }

        const modal = new bootstrap.Modal(document.getElementById('groupMembersModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading group members:', error);
        showToast('Failed to load group members', 'error');
    }
}

async function toggleAdminRole(event, groupId, memberId, isCurrentlyAdmin) {
    event.stopPropagation();
    try {
        const response = await fetch(`/chat/groups/${groupId}/admins/${memberId}`, {
            method: isCurrentlyAdmin ? 'DELETE' : 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to update admin role');
        }

        showToast(`Successfully ${isCurrentlyAdmin ? 'removed' : 'added'} admin role`, 'success');

        // Update the UI immediately for the user performing the action
        const memberItem = event.target.closest('.group-member-item');
        if (memberItem) {
            // Toggle admin badge
            const existingBadge = memberItem.querySelector('.admin-badge:not(.creator)');
            if (!isCurrentlyAdmin) {
                // Adding admin
                if (!existingBadge) {
                    const badge = document.createElement('span');
                    badge.className = 'admin-badge';
                    badge.textContent = 'Admin';
                    const nameSpan = memberItem.querySelector('span');
                    nameSpan.insertAdjacentElement('afterend', badge);
                }
            } else {
                // Removing admin
                if (existingBadge) {
                    existingBadge.remove();
                }
            }

            // Update the button text and onclick handler
            const button = memberItem.querySelector('.admin-toggle-btn');
            if (button) {
                button.textContent = isCurrentlyAdmin ? 'Make Admin' : 'Remove Admin';
                button.onclick = (e) => toggleAdminRole(e, groupId, memberId, !isCurrentlyAdmin);
            }
        }

    } catch (error) {
        console.error('Error updating admin role:', error);
        showToast(error.message || 'Failed to update admin role', 'error');
    }
}

connection.on("AdminRoleUpdated", function (data) {
    const memberItem = document.querySelector(`.group-member-item:has([onclick*="${data.userId}"])`);
    if (memberItem) {
        // Handle admin badge
        const existingBadge = memberItem.querySelector('.admin-badge:not(.creator)');
        if (data.isAdmin) {
            if (!existingBadge) {
                const badge = document.createElement('span');
                badge.className = 'admin-badge';
                badge.textContent = 'Admin';
                const nameSpan = memberItem.querySelector('span');
                nameSpan.insertAdjacentElement('afterend', badge);
            }
        } else {
            if (existingBadge) {
                existingBadge.remove();
            }
        }

        // Handle admin toggle button
        const button = memberItem.querySelector('.admin-toggle-btn');
        if (button) {
            button.textContent = data.isAdmin ? 'Remove Admin' : 'Make Admin';
            button.onclick = (e) => toggleAdminRole(e, data.groupId, data.userId, data.isAdmin);
        }
    }
});

// Add new functions for member management
async function removeMemberFromGroup(groupId, memberId) {
    if (!confirm('Are you sure you want to remove this member?')) return;

    try {
        const response = await fetch(`/chat/groups/${groupId}/members/${memberId}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showToast('Member removed successfully', 'success');

            // Properly close the modal and remove backdrop
            const modal = bootstrap.Modal.getInstance(document.getElementById('groupMembersModal'));
            modal.hide();

            // Remove modal backdrop if it exists
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }

            // Remove modal-open class from body
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';

            // Reopen the modal with updated member list
            setTimeout(() => {
                showGroupMembers(groupId);
            }, 100);
        } else {
            showToast('Failed to remove member', 'error');
        }
    } catch (error) {
        console.error('Error removing member:', error);
        showToast('Failed to remove member', 'error');
    }
}

function openAddMembersModal(groupId) {
    // Properly close the members modal
    const membersModal = bootstrap.Modal.getInstance(document.getElementById('groupMembersModal'));
    membersModal.hide();

    // Remove modal backdrop and cleanup
    const backdrop = document.querySelector('.modal-backdrop');
    if (backdrop) {
        backdrop.remove();
    }
    document.body.classList.remove('modal-open');
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';

    // Show the add members modal after a short delay
    setTimeout(() => {
        const addMembersModal = new bootstrap.Modal(document.getElementById('addGroupMembersModal'));
        document.getElementById('addMembersGroupId').value = groupId;
        addMembersModal.show();
    }, 100);
}

// Add member search functionality for the add members modal
async function searchNewMembers(event) {
    const searchTerm = event.target.value.trim();
    const resultsContainer = document.getElementById('newMemberSearchResults');

    if (searchTerm.length < 2) {
        resultsContainer.innerHTML = '';
        return;
    }

    try {
        const response = await fetch(`/chat/search/${searchTerm}`);
        const users = await response.json();

        resultsContainer.innerHTML = '';
        users.forEach(user => {
            const div = document.createElement('div');
            div.className = 'user-search-result';
            div.innerHTML = `
                    <img src="${user.profilePicturePath || '/images/default/default-profile.png'}" alt="${user.fullName}">
                    <span>${user.fullName}</span>
                    <button class="btn btn-sm btn-primary" onclick="addNewMemberToGroup('${user.id}', '${user.fullName}')">
                        Add
                    </button>
                `;
            resultsContainer.appendChild(div);
        });
    } catch (error) {
        console.error('Error searching users:', error);
    }
}

async function addNewMemberToGroup(userId, userName) {
    const groupId = document.getElementById('addMembersGroupId').value;

    try {
        const response = await fetch(`/chat/groups/${groupId}/members`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            showToast(`${userName} added to the group`, 'success');

            // Properly close the add members modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('addGroupMembersModal'));
            modal.hide();

            // Remove modal backdrop and cleanup
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';

            // Reopen the members modal with updated list
            setTimeout(() => {
                showGroupMembers(groupId);
            }, 100);
        } else {
            showToast('Failed to add member', 'error');
        }
    } catch (error) {
        console.error('Error adding member:', error);
        showToast('Failed to add member', 'error');
    }
}

function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : 'success'} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

function scrollToBottom() {
    const chatBox = document.getElementById('chatBox');
    chatBox.scrollTop = chatBox.scrollHeight;
}

// Add SignalR connection handler for group name updates
connection.on("GroupNameUpdated", function (groupId, newName) {
    const groupItem = document.querySelector(`.user-recent-chat[data-group-id="${groupId}"]`);
    if (groupItem) {
        const nameDiv = groupItem.querySelector('.user-name');
        if (nameDiv) {
            const currentUserId = document.getElementById('currentUserId').value;
            const isCreator = groupItem.querySelector('.edit-group-name') !== null;

            if (isCreator) {
                nameDiv.innerHTML = `
                        <span style="cursor: pointer;" onclick="openGroupChat(${groupId})">${newName}</span>
                        <i class="bi bi-pencil-square edit-group-name" onclick="openEditGroupName(event, ${groupId}, '${newName}')"></i>
                    `;
            } else {
                nameDiv.textContent = newName;
            }
        }
    }

    if (selectedGroupId === groupId) {
        const headerTitle = document.querySelector('.group-chat-header h4');
        if (headerTitle) {
            headerTitle.textContent = newName;
        }
    }
});

// Add these new SignalR handlers after the existing connection.on handlers
connection.on("GroupMemberRemoved", function (groupId, memberId) {
    // Update member count in group chat list
    const groupItem = document.querySelector(`.user-recent-chat[data-group-id="${groupId}"]`);
    if (groupItem) {
        const memberCountElement = groupItem.querySelector('.member-count');
        if (memberCountElement) {
            const currentCount = parseInt(memberCountElement.textContent);
            memberCountElement.textContent = `${currentCount - 1} members`;
        }
    }

    // Update member count in chat header if the group is currently open
    if (selectedGroupId === groupId) {
        const headerMemberCount = document.querySelector('.group-chat-header .member-count');
        if (headerMemberCount) {
            const currentCount = parseInt(headerMemberCount.textContent);
            headerMemberCount.textContent = `${currentCount - 1} members`;
        }
    }
});

connection.on("GroupMemberAdded", function (groupId, memberId) {
    // Update member count in group chat list
    const groupItem = document.querySelector(`.user-recent-chat[data-group-id="${groupId}"]`);
    if (groupItem) {
        const memberCountElement = groupItem.querySelector('.member-count');
        if (memberCountElement) {
            const currentCount = parseInt(memberCountElement.textContent);
            memberCountElement.textContent = `${currentCount + 1} members`;
        }
    }

    // Update member count in chat header if the group is currently open
    if (selectedGroupId === groupId) {
        const headerMemberCount = document.querySelector('.group-chat-header .member-count');
        if (headerMemberCount) {
            const currentCount = parseInt(headerMemberCount.textContent);
            headerMemberCount.textContent = `${currentCount + 1} members`;
        }
    }
});

// Format timestamp for chat messages
function formatChatTime(date) {
    const now = new Date();
    const diffMs = now - date;
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
        // Today - show time only
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (diffDays === 1) {
        // Yesterday
        return 'Yesterday';
    } else if (diffDays < 7) {
        // Within last week - show day name
        const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        return days[date.getDay()];
    } else {
        // Older - show date
        return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    }
}

// Mark messages as read
function markMessagesAsRead(senderId) {
    fetch(`/chat/mark-as-read/${senderId}`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        }
    })
        .then(response => response.json())
        .then(data => {
            console.log("Messages marked as read:", data);
        })
        .catch(err => {
            console.error("Error marking messages as read:", err);
        });
}