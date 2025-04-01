let screenStream = null;
let isScreenSharing = false;

function setupSignalRHandlers() {
    // ... existing handlers ...

    // Add screen sharing signal handler
    signalRConnection.on("ReceiveScreenShareSignal", async (senderId, signal) => {
        try {
            const parsedSignal = JSON.parse(signal);
            
            if (parsedSignal.type === "screen-share-start") {
                // Handle screen share start
                if (remoteVideo.srcObject !== parsedSignal.stream) {
                    // Create a new MediaStream from the received stream data
                    const stream = new MediaStream();
                    if (parsedSignal.stream && parsedSignal.stream.tracks) {
                        parsedSignal.stream.tracks.forEach(track => {
                            stream.addTrack(track);
                        });
                    }
                    
                    remoteVideo.srcObject = stream;
                    remoteVideo.style.display = 'block';
                    localVideo.style.display = 'none';
                    
                    // Update UI to show screen sharing is active
                    const screenShareBtn = document.getElementById("screenShareBtn");
                    if (screenShareBtn) {
                        screenShareBtn.classList.add("screen-sharing");
                    }
                }
            } else if (parsedSignal.type === "screen-share-stop") {
                // Handle screen share stop
                if (remoteVideo.srcObject) {
                    remoteVideo.srcObject.getTracks().forEach(track => track.stop());
                    remoteVideo.srcObject = null;
                    remoteVideo.style.display = 'none';
                    localVideo.style.display = 'block';
                    
                    // Update UI to show screen sharing is inactive
                    const screenShareBtn = document.getElementById("screenShareBtn");
                    if (screenShareBtn) {
                        screenShareBtn.classList.remove("screen-sharing");
                    }
                }
            }
        } catch (error) {
            console.error("Error handling screen share signal:", error);
        }
    });
}

async function toggleScreenShare() {
    const screenShareBtn = document.getElementById("screenShareBtn");
    
    try {
        if (!isScreenSharing) {
            // Start screen sharing
            screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    cursor: "always"
                },
                audio: false
            });

            // Handle screen share stop
            screenStream.getVideoTracks()[0].onended = () => {
                stopScreenSharing();
            };

            // Add screen share track to peer connection
            screenStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, screenStream);
            });

            // Update UI
            screenShareBtn.classList.add("screen-sharing");
            isScreenSharing = true;

            // Send screen share start signal
            await sendSignalSafely(targetUserId, JSON.stringify({
                type: "screen-share-start",
                stream: {
                    tracks: screenStream.getTracks().map(track => ({
                        kind: track.kind,
                        label: track.label,
                        enabled: track.enabled,
                        muted: track.muted,
                        readyState: track.readyState
                    }))
                }
            }));

            // Show screen share in main video area
            localVideo.srcObject = screenStream;
            localVideo.style.display = 'block';
            remoteVideo.style.display = 'none';
        } else {
            // Stop screen sharing
            stopScreenSharing();
        }
    } catch (error) {
        console.error("Error toggling screen share:", error);
        // Handle user cancellation
        if (error.name === 'NotAllowedError') {
            console.log("Screen sharing was cancelled by user");
        }
    }
}

function stopScreenSharing() {
    if (screenStream) {
        // Remove screen share tracks from peer connection
        screenStream.getTracks().forEach(track => {
            peerConnection.removeTrack(track);
        });

        // Stop all tracks
        screenStream.getTracks().forEach(track => track.stop());
        screenStream = null;

        // Update UI
        document.getElementById("screenShareBtn").classList.remove("screen-sharing");
        isScreenSharing = false;

        // Send screen share stop signal
        sendSignalSafely(targetUserId, JSON.stringify({
            type: "screen-share-stop"
        }));

        // Restore video display
        if (localStream) {
            localVideo.srcObject = localStream;
            localVideo.style.display = 'block';
        }
    }
}

function cleanupCallResources() {
    // ... existing cleanup code ...
    
    // Stop screen sharing if active
    if (screenStream) {
        screenStream.getTracks().forEach(track => track.stop());
        screenStream = null;
    }
    
    // ... rest of cleanup code ...
}

document.addEventListener("DOMContentLoaded", function() {
    // ... existing event listeners ...
    
    const screenShareBtn = document.getElementById("screenShareBtn");
    if (screenShareBtn) {
        screenShareBtn.addEventListener("click", toggleScreenShare);
    }
}); 