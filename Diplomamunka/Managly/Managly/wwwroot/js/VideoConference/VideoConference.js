const startCallBtn = document.getElementById("startCallBtn");
const endCallBtn = document.getElementById("endCallBtn");
const localVideo = document.getElementById("localVideo");
const remoteVideo = document.getElementById("remoteVideo");

let localStream;
let peerConnection;
let signalRConnection;
let currentUserId = document.getElementById('videoConferenceScript').getAttribute('data-user-id'); 
let targetUserId = null;
let callId = null;
let selectedContact = null;
selectedUsers = [];
let screenStream = null;
let isScreenSharing = false;

const muteAudioBtn = document.getElementById("muteAudioBtn");
const toggleVideoBtn = document.getElementById("toggleVideoBtn");
let isMuted = false;
let isVideoOff = false;

const smallSelfView = document.getElementById("localVideoSmall");
const selfViewVideo = document.getElementById("selfView");
let isSwapped = false;

let missedCallTimer;

muteAudioBtn.addEventListener("click", function () {
    if (localStream) {
        localStream.getAudioTracks()[0].enabled = isMuted;
        isMuted = !isMuted;
        this.classList.toggle("bg-red-500");
        this.innerHTML = this.classList.contains("bg-red-500") ? '<i class="fas fa-microphone-slash mr-2"></i>' : '<i class="fas fa-microphone mr-2"></i>';
    }
});

toggleVideoBtn.addEventListener("click", function () {
    if (localStream) {
        localStream.getVideoTracks()[0].enabled = isVideoOff;
        isVideoOff = !isVideoOff;
        this.classList.toggle("bg-red-500");
        this.innerHTML = this.classList.contains("bg-red-500") ? '<i class="fas fa-video-slash mr-2"></i>' : '<i class="fas fa-video mr-2"></i>';
    }
});


async function connectToSignalR() {
    if (signalRConnection) {
        try {
            await signalRConnection.stop();
        } catch (error) {
            console.error("Error stopping existing SignalR connection:", error);
        }
    }

    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/videocallhub")
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 15000])
        .build();

    try {
        await signalRConnection.start();

        signalRConnection.onclose(async (error) => {
            console.log("SignalR connection closed.", error ? `Error: ${error}` : "");

            if (!window.intentionalDisconnect) {
                await tryReconnect();
            }
        });

        setupSignalRHandlers();
        return true;
    } catch (error) {
        await tryReconnect();
        return false;
    }
}

async function tryReconnect() {
    let attempts = 0;
    const maxAttempts = 5;

    while (attempts < maxAttempts) {
        attempts++;
        try {
            await signalRConnection.start();
            setupSignalRHandlers();
            return true;
        } catch (error) {
            if (attempts < maxAttempts) {
                await new Promise(resolve => setTimeout(resolve, 2000 * attempts));
            }
        }
    }

    return false;
}

function setupSignalRHandlers() {
    signalRConnection.off("ReceiveCallRequest");
    signalRConnection.off("ReceiveSignal");
    signalRConnection.off("CallEnded");
    signalRConnection.off("ReceiveScreenShareSignal");

    signalRConnection.on("ReceiveCallRequest", (callerId, callerName, receivedCallId) => {
        targetUserId = callerId;
        callId = receivedCallId;

        document.getElementById("noVideoText").style.display = "none";

        const incomingCallElement = document.getElementById("incomingCall");
        incomingCallElement.classList.remove("hidden");
        incomingCallElement.style.display = "flex";

        document.getElementById("callMessage").innerText = `${callerName} is calling you`;

        const joinCallBtn = document.getElementById("joinCallBtn");
        joinCallBtn.setAttribute("data-sender-id", callerId);
        joinCallBtn.style.display = "block";
    });

    signalRConnection.on("ReceiveSignal", async (senderId, signal) => {
        try {
            const parsedSignal = JSON.parse(signal);

            if (!peerConnection) {
                initializeWebRTC();
            }

            if (parsedSignal.type === "offer") {
                await peerConnection.setRemoteDescription(new RTCSessionDescription(parsedSignal));

                if (localStream) {
                    const answer = await peerConnection.createAnswer();
                    await peerConnection.setLocalDescription(answer);

                    await signalRConnection.invoke("SendSignal", senderId, JSON.stringify(peerConnection.localDescription))
                        .catch(err => console.error("Error sending answer:", err));
                } else {
                    console.log("⚠️ Not creating answer yet - waiting for user to accept call");
                }
            } else if (parsedSignal.type === "answer") {
                await peerConnection.setRemoteDescription(new RTCSessionDescription(parsedSignal));

                document.getElementById("waitingIndicator").classList.add("hidden");
            } else if (parsedSignal.candidate) {
                try {
                    await peerConnection.addIceCandidate(parsedSignal.candidate);
                } catch (e) {
                    console.error("Error adding received ice candidate:", e);
                }
            }
        } catch (error) {
            console.error("Error handling received signal:", error);
        }
    });

    signalRConnection.on("ReceiveScreenShareSignal", async (senderId, signal) => {
        try {
            const parsedSignal = JSON.parse(signal);
            
            if (parsedSignal.type === "screen-share-start") {
                if (localStream) {
                    selfViewVideo.srcObject = localStream;
                    selfViewVideo.muted = true;
                }
                
                const screenShareBtn = document.getElementById("screenShareBtn");
                if (screenShareBtn) {
                    screenShareBtn.classList.add("screen-sharing");
                }
            } 
            else if (parsedSignal.type === "screen-share-stop") {
                const screenShareBtn = document.getElementById("screenShareBtn");
                if (screenShareBtn) {
                    screenShareBtn.classList.remove("screen-sharing");
                }
                
                if (remoteVideo.srcObject && localStream) {
                    selfViewVideo.srcObject = localStream;
                    selfViewVideo.muted = true;
                }
            }
        } catch (error) {
            console.error("Error handling screen share signal:", error);
        }
    });

    signalRConnection.on("CallEnded", (endedCallId, durationText) => {
        document.getElementById("waitingIndicator").classList.add("hidden");

        const incomingCallElement = document.getElementById("incomingCall");
        if (!incomingCallElement.classList.contains("hidden")) {
            incomingCallElement.classList.add("hidden");
            incomingCallElement.style.display = "none";
        }

        const rejoinCallElement = document.getElementById("rejoinCall");
        if (!rejoinCallElement.classList.contains("hidden")) {
            rejoinCallElement.classList.add("hidden");
        }

        const statusBanner = document.getElementById("callStatusBanner");
        statusBanner.innerHTML = `<span>Call ended: ${durationText}</span>`;
        statusBanner.style.display = "block";

        const noVideoText = document.getElementById("noVideoText");
        noVideoText.style.display = "flex";

        noVideoText.innerHTML = `
                <div class="placeholder-content">
                    <i class="fas fa-phone-slash text-red-500"></i>
                    <h3 class="text-xl font-bold">Call Ended</h3>
                    <p class="text-lg">${durationText}</p>
                </div>
            `;

        cleanupCallResources();

        setTimeout(() => {
            window.intentionalDisconnect = false; 
            window.location.reload();
        }, 5000);
    });

    signalRConnection.on("CallStarted", async (receivedCallId) => {
        callId = receivedCallId;

        try {
            if (!localStream) {
                localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            }

            if (!peerConnection) {
                initializeWebRTC();
            }

            remoteVideo.srcObject = localStream;
            remoteVideo.muted = true;
            remoteVideo.style.display = 'block';

            const pipContainer = document.getElementById('localVideoSmall');
            pipContainer.style.opacity = '0';
            pipContainer.style.transform = 'scale(0.95)';

            document.getElementById('noVideoText').style.display = 'none';
            document.getElementById('incomingCall').classList.add('hidden');
        } catch (error) {
            console.error("❌ Error in CallStarted handler:", error);
        }
    });
}

async function sendSignalSafely(userId, signal) {
    if (!userId) {
        return;
    }

    if (!signalRConnection) {
        try {
            await connectToSignalR();
        } catch (error) {
            return; 
        }
    }

    if (signalRConnection.state !== signalR.HubConnectionState.Connected) {
        try {
            if (signalRConnection.state === signalR.HubConnectionState.Disconnected) {
                await signalRConnection.start();
                setupSignalRHandlers();
            } else {
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
        } catch (error) {
            console.error("❌ Failed to reconnect SignalR:", error);
            return; 
        }
    }

    try {
        await signalRConnection.invoke("SendSignal", userId, signal);
    } catch (error) {
        if (!error.message.includes("already tried")) {
            error.message += " (already tried reconnecting)";

            await new Promise(resolve => setTimeout(resolve, 1000));

            try {
                await connectToSignalR();
                await signalRConnection.invoke("SendSignal", userId, signal);
            } catch (retryError) {
                console.error("❌ Final attempt to send signal failed:", retryError);
            }
        }
    }
}

async function startCall() {
    if (!targetUserId || !callId) {
        return;
    }

    try {
        if (!localStream) {
            localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        }

        remoteVideo.srcObject = null;
        selfViewVideo.srcObject = null;

        localVideo.srcObject = localStream;
        localVideo.style.display = 'block';
        localVideo.classList.remove('hidden');
        remoteVideo.style.display = 'none';

        const pipContainer = document.getElementById('localVideoSmall');
        pipContainer.style.opacity = '0';
        pipContainer.style.transform = 'scale(0.95)';

        document.getElementById('noVideoText').style.display = 'none';
        
        initializeWebRTC();

        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        await sendSignalSafely(targetUserId, JSON.stringify(offer));

    } catch (error) {
        console.error("❌ Error starting call:", error);
    }
}

async function reconnectToCall() {
    if (!callId) {
        return;
    }

    try {
        if (!signalRConnection || signalRConnection.state !== signalR.HubConnectionState.Connected) {
            try {
                await connectToSignalR();
                await new Promise(resolve => setTimeout(resolve, 1000));
            } catch (error) {
                document.getElementById("noVideoText").innerHTML = `
                        <p class="text-red-500">Failed to reconnect to SignalR</p>
                        <button onclick="reconnectToCall()" class="primary-btn mt-3">Try Again</button>
                    `;
                return;
            }
        }

        const response = await fetch(`/api/videoconference/get-call/${callId}`);
        const call = await response.json();

        if (!call || call.Status === "Ended") {
            document.getElementById("noVideoText").innerHTML = `<p class="text-red-500">The call has ended. You cannot rejoin.</p>`;
            return;
        }


        if (call.CallerId && call.ReceiverId) {
            if (call.CallerId !== currentUserId) {
                targetUserId = call.CallerId;
            } else {
                targetUserId = call.ReceiverId;
            }
        } else {
            console.error("❌ Call data missing user IDs:", call);
        }

        if (!targetUserId) {
            document.getElementById("noVideoText").innerHTML = `
                    <p class="text-red-500">Failed to identify the other participant</p>
                    <p class="text-sm text-gray-400">Call data: ${JSON.stringify(call)}</p>
                    <p class="text-sm text-gray-400">Current user: ${currentUserId}</p>
                    <button onclick="reconnectToCall()" class="primary-btn mt-3">Try Again</button>
                `;
            return;
        }

        try {
            if (peerConnection) {
                peerConnection.close();
                peerConnection = null;
            }

            if (localStream) {
                localStream.getTracks().forEach(track => track.stop());
                localStream = null;
            }

            remoteVideo.srcObject = null;
            localVideo.srcObject = null;
            selfViewVideo.srcObject = null;

            try {
                localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            } catch (mediaError) {
                document.getElementById("noVideoText").innerHTML = `
                        <p class="text-red-500">Failed to access camera or microphone</p>
                        <button onclick="reconnectToCall()" class="primary-btn mt-3">Try Again</button>
                    `;
                return;
            }

            localVideo.srcObject = localStream;
            localVideo.style.display = 'block';
            localVideo.classList.remove('hidden');
            remoteVideo.style.display = 'none';

            document.getElementById('noVideoText').style.display = 'none';
            document.getElementById('rejoinCall').classList.add('hidden');

            peerConnection = new RTCPeerConnection({
                iceServers: [
                    { urls: "stun:stun.l.google.com:19302" },
                    { urls: "turn:openrelay.metered.ca:443", username: "openrelayproject", credential: "openrelayproject" }
                ]
            });

            peerConnection.onicecandidate = async (event) => {
                if (event.candidate) {
                    try {
                        await sendSignalSafely(targetUserId, JSON.stringify({
                            type: "candidate",
                            candidate: event.candidate
                        }));
                    } catch (error) {
                        console.error("❌ Error sending ICE candidate:", error);
                    }
                }
            };

            peerConnection.ontrack = (event) => {
                if (!event.streams || event.streams.length === 0) {
                    return;
                }
                
                const isScreenTrack = event.track.kind === 'video' && event.track.label && 
                                     (event.track.label.includes('screen') || 
                                      event.track.label.includes('display') ||
                                      event.track.label.includes('web contents'));
                
                if (isScreenTrack) {
                    remoteVideo.srcObject = event.streams[0];
                    remoteVideo.muted = false;
                    remoteVideo.style.display = 'block';
                    localVideo.style.display = 'none';

                } else {
                    if (isScreenSharing) {
                        selfViewVideo.srcObject = event.streams[0];
                        selfViewVideo.muted = true;
                    } else if (remoteVideo.srcObject && remoteVideo.srcObject !== event.streams[0] && 
                              remoteVideo.srcObject.getVideoTracks().length > 0 && 
                              remoteVideo.srcObject.getVideoTracks()[0].label.includes('screen')) {
                        selfViewVideo.srcObject = event.streams[0];
                        selfViewVideo.muted = true;
                    } else {
                        remoteVideo.srcObject = event.streams[0];
                        remoteVideo.muted = false;
                        remoteVideo.style.display = 'block';
                        localVideo.style.display = 'none';
                        
                        if (localStream) {
                            selfViewVideo.srcObject = localStream;
                            selfViewVideo.muted = true;
                        }
                    }
                }
                
                const pipContainer = document.getElementById('localVideoSmall');
                pipContainer.classList.add('active');
                pipContainer.style.opacity = '1';
                pipContainer.style.transform = 'scale(1)';
                
                document.getElementById("waitingIndicator").classList.add("hidden");
            };

            const videoTrack = localStream.getVideoTracks()[0];
            const audioTrack = localStream.getAudioTracks()[0];

            if (videoTrack) {
                peerConnection.addTrack(videoTrack, localStream);
            }
            if (audioTrack) {
                peerConnection.addTrack(audioTrack, localStream);
            }

            const offer = await peerConnection.createOffer({
                offerToReceiveAudio: true,
                offerToReceiveVideo: true
            });

            await peerConnection.setLocalDescription(offer);

            await sendSignalSafely(targetUserId, JSON.stringify(offer));

            await fetch(`/api/videoconference/mark-call-active/${callId}`, { method: "POST" });
        } catch (error) {
            document.getElementById("noVideoText").innerHTML = `
                    <p class="text-red-500">Failed to rejoin the call</p>
                    <button onclick="reconnectToCall()" class="primary-btn mt-3">Try Again</button>
                `;
        }
    } catch (error) {
        console.error("❌ Error fetching call status:", error);
    }
}

function initializeWebRTC() {
    if (peerConnection) {
        peerConnection.close();
    }

    peerConnection = new RTCPeerConnection({
        iceServers: [
            { urls: "stun:stun.l.google.com:19302" },
            { urls: "turn:openrelay.metered.ca:443", username: "openrelayproject", credential: "openrelayproject" }
        ]
    });

    peerConnection.onicecandidate = async (event) => {
        if (event.candidate) {
            try {
                await sendSignalSafely(targetUserId, JSON.stringify({
                    type: "candidate",
                    candidate: event.candidate
                }));
            } catch (error) {
                console.error("❌ Error sending ICE candidate:", error);
            }
        }
    };

    peerConnection.ontrack = (event) => {
        if (!event.streams || event.streams.length === 0) {
            return;
        }
        
        const isScreenTrack = event.track.kind === 'video' && event.track.label && 
                             (event.track.label.includes('screen') || 
                              event.track.label.includes('display') ||
                              event.track.label.includes('web contents'));
        
        if (isScreenTrack) {
            remoteVideo.srcObject = event.streams[0];
            remoteVideo.muted = false;
            remoteVideo.style.display = 'block';
            localVideo.style.display = 'none';
            
        } else {
            if (isScreenSharing) {
                selfViewVideo.srcObject = event.streams[0];
                selfViewVideo.muted = true;
            } else if (remoteVideo.srcObject && remoteVideo.srcObject !== event.streams[0] && 
                      remoteVideo.srcObject.getVideoTracks().length > 0 && 
                      remoteVideo.srcObject.getVideoTracks()[0].label.includes('screen')) {
                selfViewVideo.srcObject = event.streams[0];
                selfViewVideo.muted = true;
            } else {
                remoteVideo.srcObject = event.streams[0];
                remoteVideo.muted = false;
                remoteVideo.style.display = 'block';
                localVideo.style.display = 'none';
                
                if (localStream) {
                    selfViewVideo.srcObject = localStream;
                    selfViewVideo.muted = true;
                }
            }
        }
        
        const pipContainer = document.getElementById('localVideoSmall');
        pipContainer.classList.add('active');
        pipContainer.style.opacity = '1';
        pipContainer.style.transform = 'scale(1)';
        
        document.getElementById("waitingIndicator").classList.add("hidden");
    };

    if (localStream) {
        const videoTrack = localStream.getVideoTracks()[0];
        const audioTrack = localStream.getAudioTracks()[0];

        if (videoTrack) {
            peerConnection.addTrack(videoTrack, localStream);
        }
        if (audioTrack) {
            peerConnection.addTrack(audioTrack, localStream);
        }
    }
}

smallSelfView.addEventListener("click", function () {
    if (!localStream || !remoteVideo.srcObject) {
        return;
    }

    if (isSwapped) {
        let tempStream = selfViewVideo.srcObject;
        selfViewVideo.srcObject = remoteVideo.srcObject;
        remoteVideo.srcObject = tempStream;

        remoteVideo.muted = false;
        selfViewVideo.muted = true;
    } else {
        let tempStream = selfViewVideo.srcObject;
        selfViewVideo.srcObject = remoteVideo.srcObject;
        remoteVideo.srcObject = tempStream;

        remoteVideo.muted = true;
        selfViewVideo.muted = false;
    }

    isSwapped = !isSwapped;
});

endCallBtn.addEventListener("click", function () {
    endCall();
});

function endCall() {
    if (!callId) {
        return;
    }

    fetch(`/api/videoconference/end-call/${callId}`, {
        method: "POST"
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(errorData => { throw new Error(errorData.error || "Unknown error"); });
            }
            return response.json();
        })
        .then(data => {
            cleanupCallResources();

            const statusBanner = document.getElementById("callStatusBanner");
            statusBanner.innerHTML = `<span>Call ended: ${data.message}</span>`;
            statusBanner.style.display = "block";

            const noVideoText = document.getElementById("noVideoText");
            noVideoText.style.display = "flex";

            loadRecentCalls();
        })
        .catch(error => {
            cleanupCallResources();
        });
}

function cleanupCallResources() {
    window.intentionalDisconnect = true;

    if (peerConnection) {
        peerConnection.ontrack = null;
        peerConnection.onicecandidate = null;
        peerConnection.close();
        peerConnection = null;
    }

    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }

    if (screenStream) {
        screenStream.getTracks().forEach(track => track.stop());
        screenStream = null;
    }

    localVideo.srcObject = null;
    remoteVideo.srcObject = null;
    selfViewVideo.srcObject = null;

    const pipContainer = document.getElementById('localVideoSmall');
    pipContainer.style.opacity = '0';
    pipContainer.style.transform = 'scale(0.95)';

    callId = null;
    targetUserId = null;
    selectedUsers = [];
    selectedContact = null;

    document.getElementById("searchUsers").value = "";
    document.getElementById("userResults").innerHTML = "";

    document.getElementById("selectedContact").classList.add("hidden");
    document.getElementById("emptySelection").classList.remove("hidden");
    document.getElementById("startCallBtn").classList.add("hidden");

    document.getElementById("waitingIndicator").classList.add("hidden");

    document.getElementById("searchUsers").disabled = false;
    document.getElementById("searchUsers").classList.remove("disabled");
    document.getElementById("clearSearch").style.opacity = "1";

    let waitingRoomText = document.getElementById("waitingRoomText");
    if (waitingRoomText) {
        waitingRoomText.innerHTML = "";
    }

    if (signalRConnection) {
        signalRConnection.stop().catch(err => console.error("Error stopping SignalR:", err));
    }
}

function cleanupCallUI() {
    cleanupCallResources();

    setTimeout(() => {
        window.intentionalDisconnect = false; 
        window.location.reload();
    }, 5000);
}

function loadRecentCalls() {
    fetch("/api/videoconference/get-recent-calls")
        .then(response => {
            if (!response.ok) {
                return response.json().then(errorData => { throw new Error(errorData.error || "Unknown error"); });
            }
            return response.json();
        })
        .then(data => {
            let callsList = document.getElementById("recentCallsList");
            callsList.innerHTML = "";

            if (!Array.isArray(data) || data.length === 0) {
                callsList.innerHTML = `<li class="text-gray-400">No recent calls</li>`;
                return;
            }

            data.forEach(call => {
                let li = document.createElement("li");
                li.classList.add("p-3", "bg-gray-700", "rounded-lg", "cursor-pointer", "hover:bg-gray-600", "relative");

                let statusColor = call.status === "Missed" ? "text-red-500" : "text-gray-400";
                let icon = call.status === "Missed"
                    ? `<i class="fas fa-phone-slash ${statusColor} mr-2"></i>`
                    : `<i class="fas fa-phone-alt ${statusColor} mr-2"></i>`;

                li.innerHTML = `
                    <p class="font-semibold flex items-center">${icon}${call.otherUser}</p>
                    <p class="text-sm text-gray-400">${call.duration}</p>
                    <p class="text-xs text-gray-500">${call.timestamp}</p>
                `;

                callsList.appendChild(li);
            });
        })
        .catch(error => console.error("❌ Error fetching recent calls:", error));
}

async function checkForActiveCall() {
    fetch("/api/videoconference/get-active-call")
        .then(response => response.json())
        .then(data => {
            if (data.hasActiveCall) {
                callId = data.callId;
                targetUserId = data.targetUserId;
                showRejoinButton();
            } 
        })
        .catch(error => console.error("❌ Error checking active call:", error));
}

function showRejoinButton() {
    const rejoinUI = document.getElementById("rejoinCall");
    rejoinUI.classList.remove("hidden");

    document.getElementById("noVideoText").style.display = "none";
    document.getElementById("incomingCall").classList.add("hidden");

    document.getElementById("rejoinCallBtn").addEventListener("click", function () {
        rejoinUI.classList.add("hidden");
        reconnectToCall();
    });
}


startCallBtn.addEventListener("click", startCall);

document.addEventListener("DOMContentLoaded", function () {
    connectToSignalR();
    loadNotifications();
    checkForInvites();
    loadRecentCalls();
    checkForActiveCall();

    document.getElementById("clearSearch").addEventListener("click", function () {
        document.getElementById("searchUsers").value = "";
        document.getElementById("userResults").innerHTML = "";
        this.style.opacity = "0";
    });

    document.getElementById("searchUsers").addEventListener("input", function () {
        let query = this.value.trim();

        const clearBtn = document.getElementById("clearSearch");
        clearBtn.style.opacity = query.length > 0 ? "1" : "0";

        if (query.length < 2) {
            document.getElementById("userResults").innerHTML = "";
            return;
        }

        fetch(`/api/videoconference/search-users?query=${encodeURIComponent(query)}`)
            .then(response => {
                if (!response.ok) {
                    return response.json().then(err => { throw new Error(err.error || "Unknown error"); });
                }
                return response.json();
            })
            .then(data => {
                let resultsList = document.getElementById("userResults");
                resultsList.innerHTML = "";

                if (!Array.isArray(data) || data.length === 0) {
                    resultsList.innerHTML = `<div class="no-results">No contacts found</div>`;
                    return;
                }

                data.forEach(user => {
                    if (!user.id || !user.fullName) return;

                    let contactCard = document.createElement("div");
                    contactCard.className = "contact-card";
                    contactCard.setAttribute("data-user-id", user.id);
                    contactCard.setAttribute("data-user-name", user.fullName);

                    contactCard.innerHTML = `
                            <div class="contact-avatar">
                                <i class="fas fa-user"></i>
                            </div>
                            <div class="contact-info">
                                <h3>${user.fullName}</h3>
                            </div>
                        `;

                    contactCard.addEventListener("click", function () {
                        selectContact(user.id, user.fullName);
                    });

                    resultsList.appendChild(contactCard);
                });
            })
            .catch(error => {
                document.getElementById("userResults").innerHTML = `<div class="no-results">Error: ${error.message}</div>`;
            });
    });

    function selectContact(userId, fullName) {
        const previouslySelected = document.querySelector(".contact-card.active");
        if (previouslySelected) {
            previouslySelected.classList.remove("active");
        }

        const selectedCard = document.querySelector(`.contact-card[data-user-id="${userId}"]`);
        if (selectedCard) {
            selectedCard.classList.add("active");
        }

        document.getElementById("selectedContactName").textContent = fullName;
        document.getElementById("selectedContact").classList.remove("hidden");
        document.getElementById("emptySelection").classList.add("hidden");

        document.getElementById("startCallBtn").classList.remove("hidden");

        selectedContact = { id: userId, name: fullName };
        targetUserId = userId;

        document.getElementById("userResults").innerHTML = "";
        document.getElementById("searchUsers").value = "";
    }

    document.getElementById("removeSelectedContact").addEventListener("click", function () {
        document.getElementById("selectedContact").classList.add("hidden");
        document.getElementById("emptySelection").classList.remove("hidden");
        document.getElementById("startCallBtn").classList.add("hidden");

        selectedContact = null;
        targetUserId = null;
    });

    document.getElementById("startCallBtn").addEventListener("click", function () {
        if (!selectedContact) {
            showToast("Please select a contact before starting a call!", "danger");
            return;
        }

        sendCallInvitation(selectedContact.id);
    });

    function sendCallInvitation(userId) {
        if (!userId) {
            showToast("Please select a contact before starting a call!", "danger");
            return;
        }

        if (callId) {
            showToast("You are already in a call!", "danger");
            return;
        }

        targetUserId = userId;

        document.getElementById("selectedContact").classList.add("hidden");
        document.getElementById("emptySelection").classList.add("hidden");
        document.getElementById("startCallBtn").classList.add("hidden");

        document.getElementById("searchUsers").disabled = true;
        document.getElementById("searchUsers").classList.add("disabled");
        document.getElementById("clearSearch").style.opacity = "0";

        document.getElementById("waitingIndicator").classList.remove("hidden");
        document.getElementById("waitingRoomText").innerHTML = "";

        let requestBody = JSON.stringify({ ReceiverId: userId });
        fetch("/api/videoconference/invite", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: requestBody
        })
            .then(async response => {
                let responseBody = await response.json();
                if (!response.ok) {
                    throw new Error(`Server Error: ${JSON.stringify(responseBody)}`);
                }

                callId = responseBody.callId;

                return signalRConnection.invoke("SendCallRequest", userId);
            })
            .then(() => {
                missedCallTimer = setTimeout(() => {
                    markMissedCall();
                }, 60000);

                startCall();
            })
            .catch(error => {
                document.getElementById("waitingIndicator").classList.add("hidden");

                document.getElementById("searchUsers").disabled = false;
                document.getElementById("searchUsers").classList.remove("disabled");
                document.getElementById("clearSearch").style.opacity = "1";

                document.getElementById("selectedContact").classList.remove("hidden");
                document.getElementById("startCallBtn").classList.remove("hidden");
            });
    }

    function markMissedCall() {
        if (!callId) {
            return;
        }

        fetch(`/api/videoconference/get-call/${callId}`)
            .then(response => response.json())
            .then(call => {
                if (call.Status === "Active") {
                    return;
                }

                fetch(`/api/videoconference/missed-call/${callId}`, { method: "POST" })
                    .then(response => response.json())
                    .then(data => {
                        if (!data.success) {
                            return;
                        }

                        const noVideoText = document.getElementById("noVideoText");
                        noVideoText.style.display = "flex";

                        noVideoText.innerHTML = `
                                <div class="placeholder-content">
                                    <i class="fas fa-user-clock text-yellow-500"></i>
                                    <h3 class="text-xl font-bold text-yellow-500">User Not Available</h3>
                                    <p class="text-lg text-white">The other user didn't join the call.</p>
                                    <p class="text-md text-gray-300">Please try again later.</p>
                                </div>
                            `;

                        document.getElementById("incomingCall").classList.add("hidden");
                        document.getElementById("rejoinCall").classList.add("hidden");
                        document.getElementById("waitingRoomText").innerHTML = "";

                        cleanupCallResources();

                        loadNotifications();
                        loadRecentCalls();

                        setTimeout(() => {
                            window.location.reload();
                        }, 5000);
                    })
                    .catch(error => console.error("❌ Failed to mark missed call:", error));
            })
            .catch(error => console.error("❌ Error fetching call status:", error));
    }



    function checkForInvites() {
        fetch("/api/videoconference/check-invite")
            .then(response => {
                if (!response.ok) {
                    return response.json().then(errorData => {
                        throw new Error(`Server Error: ${JSON.stringify(errorData)}`);
                    });
                }
                return response.json();
            })
            .then(data => {
                if (data.hasInvite) {
                    targetUserId = data.senderId;

                    fetch(`/api/videoconference/get-call-by-users?senderId=${data.senderId}`)
                        .then(response => response.json())
                        .then(callData => {
                            if (callData && callData.callId) {
                                callId = callData.callId;

                                if (callData.status !== "Ended" && callData.status !== "Missed") {
                                    document.getElementById("noVideoText").style.display = "none";

                                    document.getElementById("incomingCall").classList.remove("hidden");
                                    document.getElementById("callMessage").innerText = `You have been invited to a video call by ${data.senderName}`;
                                    document.getElementById("joinCallBtn").setAttribute("data-sender-id", data.senderId);
                                } else {
                                    document.getElementById("noVideoText").style.display = "flex";
                                    document.getElementById("incomingCall").classList.add("hidden");
                                }
                            } else {
                                document.getElementById("noVideoText").style.display = "flex";
                                document.getElementById("incomingCall").classList.add("hidden");
                            }
                        })
                        .catch(error => {
                            document.getElementById("noVideoText").style.display = "none";
                            document.getElementById("incomingCall").classList.remove("hidden");
                            document.getElementById("callMessage").innerText = `You have been invited to a video call by ${data.senderName}`;
                            document.getElementById("joinCallBtn").setAttribute("data-sender-id", data.senderId);
                        });
                } else {
                    document.getElementById("noVideoText").style.display = "flex";

                    document.getElementById("incomingCall").classList.add("hidden");
                    document.getElementById("callMessage").innerText = "";
                }
            })
            .catch(error => console.error("❌ Error checking for invites:", error.message));
    }

    document.getElementById("joinCallBtn").addEventListener("click", async function () {
        let senderId = this.getAttribute("data-sender-id");

        try {
            const response = await fetch(`/api/videoconference/accept-invite/${senderId}`, {
                method: "POST"
            });

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.error || "Failed to accept call");
            }

            if (data.success) {
                targetUserId = senderId;
                callId = data.callId;

                const incomingCallElement = document.getElementById("incomingCall");
                incomingCallElement.classList.add("hidden");
                incomingCallElement.style.display = "none";
                document.getElementById("callMessage").innerText = "";

                document.getElementById("searchUsers").disabled = true;
                document.getElementById("searchUsers").classList.add("disabled");
                document.getElementById("clearSearch").style.opacity = "0";

                document.getElementById("selectedContact").classList.add("hidden");
                document.getElementById("emptySelection").classList.add("hidden");
                document.getElementById("startCallBtn").classList.add("hidden");

                if (missedCallTimer) {
                    clearTimeout(missedCallTimer);
                    missedCallTimer = null;
                }

                try {
                    localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

                    localVideo.srcObject = localStream;
                    localVideo.style.display = 'block';
                    localVideo.classList.remove('hidden');
                    remoteVideo.style.display = 'none';

                    document.getElementById('noVideoText').style.display = 'none';

                    initializeWebRTC();

                    const offer = await peerConnection.createOffer();
                    await peerConnection.setLocalDescription(offer);
                    await sendSignalSafely(targetUserId, JSON.stringify(offer));

                    await fetch(`/api/videoconference/mark-call-active/${callId}`, { method: "POST" });
                } catch (error) {
                    document.getElementById("searchUsers").disabled = false;
                    document.getElementById("searchUsers").classList.remove("disabled");
                    document.getElementById("clearSearch").style.opacity = "1";

                    document.getElementById("emptySelection").classList.remove("hidden");
                }
            }
        } catch (error) {
            document.getElementById("searchUsers").disabled = false;
            document.getElementById("searchUsers").classList.remove("disabled");
            document.getElementById("clearSearch").style.opacity = "1";

            document.getElementById("emptySelection").classList.remove("hidden");
        }
    });

    const screenShareBtn = document.getElementById("screenShareBtn");
    if (screenShareBtn) {
        screenShareBtn.addEventListener("click", toggleScreenShare);
    }
});

async function toggleScreenShare() {
    const screenShareBtn = document.getElementById("screenShareBtn");
    
    try {
        if (!isScreenSharing) {
            screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: true,
                audio: false
            });
            
            screenStream.getVideoTracks()[0].onended = () => {
                stopScreenSharing();
            };
            
            const videoSenders = peerConnection.getSenders().filter(sender => 
                sender.track && sender.track.kind === 'video');
                
            if (videoSenders.length > 0) {
                await videoSenders[0].replaceTrack(screenStream.getVideoTracks()[0]);
            } else {
                peerConnection.addTrack(screenStream.getVideoTracks()[0], screenStream);
            }
            
            localVideo.srcObject = screenStream;
            localVideo.style.display = 'block';
            remoteVideo.style.display = 'none';
            
            if (remoteVideo.srcObject) {
                selfViewVideo.srcObject = remoteVideo.srcObject;
                selfViewVideo.muted = true;
            }
            
            isScreenSharing = true;
            screenShareBtn.classList.add("screen-sharing");
            
            await sendSignalSafely(targetUserId, JSON.stringify({
                type: "screen-share-start"
            }));

        } else {
            stopScreenSharing();
        }
    } catch (error) {
        console.error("Error toggling screen share:", error);
        if (error.name === 'NotAllowedError') {
            console.log("Screen sharing was cancelled by user");
        }
    }
}

function stopScreenSharing() {
    if (!isScreenSharing || !screenStream) return;
    
    try {
        screenStream.getTracks().forEach(track => track.stop());
        
        const videoSenders = peerConnection.getSenders().filter(sender => 
            sender.track && sender.track.kind === 'video');
            
        if (videoSenders.length > 0 && localStream && localStream.getVideoTracks().length > 0) {
            videoSenders[0].replaceTrack(localStream.getVideoTracks()[0]);
        }
        
        screenStream = null;
        isScreenSharing = false;
        
        document.getElementById("screenShareBtn").classList.remove("screen-sharing");
        
        if (remoteVideo.srcObject) {
            remoteVideo.style.display = 'block';
            localVideo.style.display = 'none';
        } else {
            localVideo.srcObject = localStream;
            localVideo.style.display = 'block';
        }
        
        selfViewVideo.srcObject = localStream;
        selfViewVideo.muted = true;
        
        sendSignalSafely(targetUserId, JSON.stringify({
            type: "screen-share-stop"
        }));

    } catch (error) {
        console.error("Error stopping screen share:", error);
    }
}