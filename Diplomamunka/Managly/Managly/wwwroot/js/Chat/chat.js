//const connection = new signalR.HubConnectionBuilder()
//    .withUrl("/chathub")
//    .build();

//connection.start().catch(err => console.error(err.toString()));

let selectedUserId = null;

//document.addEventListener("DOMContentLoaded", function () {
//    loadRecentChats();
//});

document.getElementById("searchInput").addEventListener("input", function () {
    let query = this.value;
    if (query.length > 2) {
        fetch(`/api/chat/search/${query}`)
            .then(response => response.json())
            .then(users => {
                let resultsDiv = document.getElementById("searchResults");
                resultsDiv.innerHTML = "";

                if (users.length === 0) {
                    resultsDiv.innerHTML = "<p>No users found</p>";
                    return;
                }

                users.forEach(user => {
                    let userDiv = document.createElement("div");
                    userDiv.textContent = user.userName;
                    userDiv.classList.add("user-search-result");

                    userDiv.onclick = function () {
                        selectedUserId = user.id;
                        loadMessages(selectedUserId);
                        document.getElementById("chatBox").style.display = "block";
                        document.getElementById("messageInput").focus();
                    };

                    resultsDiv.appendChild(userDiv);
                });
            })
            .catch(err => console.error("Search error: ", err));
    }
});

function loadMessages(userId) {
    fetch(`/api/chat/messages/${userId}`)
        .then(response => response.json())
        .then(messages => {
            let chatBox = document.getElementById("chatBox");
            chatBox.innerHTML = "";

            messages.forEach(msg => {
                let messageDiv = document.createElement("div");
                messageDiv.classList.add(msg.senderId === document.getElementById("currentUserId").value ? "my-message" : "other-message");
                messageDiv.textContent = msg.content;
                chatBox.appendChild(messageDiv);
            });
        })
        .catch(err => console.error("Load messages error: ", err));
}

document.getElementById("sendMessageBtn").addEventListener("click", function () {
    const message = document.getElementById("messageInput").value.trim();
    const senderId = document.getElementById("currentUserId").value;

    if (!selectedUserId) {
        alert("Select a user to chat with first!");
        return;
    }

    if (message === "") {
        alert("Message cannot be empty!");
        return;
    }

    fetch("/api/chat/messages", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(messageData)
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => { throw new Error(JSON.stringify(err)); });
            }
            return response.json();
        })
        .then(data => {
            console.log("✅ Message saved successfully!", data);
        })
        .catch(err => console.error("❌ Error saving message: ", err));

    let chatBox = document.getElementById("chatBox");
    let myMessageDiv = document.createElement("div");
    myMessageDiv.classList.add("my-message");
    myMessageDiv.textContent = message;
    chatBox.appendChild(myMessageDiv);

    document.getElementById("messageInput").value = "";
});



connection.on("ReceiveMessage", function (senderId, message) {
    if (selectedUserId === senderId) {
        let chatBox = document.getElementById("chatBox");
        let otherMessageDiv = document.createElement("div");
        otherMessageDiv.classList.add("other-message");
        otherMessageDiv.textContent = message;
        chatBox.appendChild(otherMessageDiv);
    }
});

//function loadRecentChats() {
//    fetch(`/api/chat/recent-chats`)
//        .then(response => response.json())
//        .then(users => {
//            let recentChatsDiv = document.getElementById("recentChats");
//            recentChatsDiv.innerHTML = ""; // Clear old chats

//            if (users.length === 0) {
//                recentChatsDiv.innerHTML = "<p>No recent chats</p>";
//                return;
//            }

//            users.forEach(user => {
//                let userDiv = document.createElement("div");
//                userDiv.textContent = user.userName;
//                userDiv.classList.add("user-recent-chat");

//                // Clicking a recent chat loads messages
//                userDiv.onclick = function () {
//                    selectedUserId = user.id;
//                    loadMessages(selectedUserId);
//                    document.getElementById("chatBox").style.display = "block";
//                    document.getElementById("messageInput").focus();
//                };

//                recentChatsDiv.appendChild(userDiv);
//            });
//        })
//        .catch(err => console.error("Error loading recent chats:", err));
//}

