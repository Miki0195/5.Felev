let startTime;
let timerInterval;
let liveUpdateInterval;

document.addEventListener("DOMContentLoaded", function () {
    loadWorkHistory();
    checkSessionStatus();
    loadWeeklyHours();
});

function startTimer(elapsedSeconds = 0) {
    startTime = new Date();
    startTime.setSeconds(startTime.getSeconds() - elapsedSeconds);
    if (timerInterval) clearInterval(timerInterval);
    timerInterval = setInterval(updateTimer, 1000);
    document.getElementById("workTimer").style.color = "green";
}

function stopTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
    document.getElementById("workTimer").innerText = "00:00:00";
    document.getElementById("workTimer").style.color = "#42a5f5";
}

function updateTimer() {
    const now = new Date();
    const elapsedTime = new Date(now - startTime);
    const hours = elapsedTime.getUTCHours().toString().padStart(2, '0');
    const minutes = elapsedTime.getUTCMinutes().toString().padStart(2, '0');
    const seconds = elapsedTime.getUTCSeconds().toString().padStart(2, '0');
    document.getElementById("workTimer").innerText = `${hours}:${minutes}:${seconds}`;
}

document.getElementById("clockInBtn").addEventListener("click", async () => {
    const response = await fetch("/api/attendance/clock-in", { method: "POST" });
    const data = await response.json();
    if (data.success) {
        document.getElementById("clockOutBtn").classList.remove("d-none");
        document.getElementById("clockInBtn").classList.add("d-none");

        startTimer();
        loadWorkHistory();
        loadWeeklyHours(true);

        localStorage.setItem('clockInStatus', JSON.stringify({
            active: true,
            checkInTime: data.checkInTime
        }));

        showToast("You successfuly clocked in!", "success");
    }
});

document.getElementById("clockOutBtn").addEventListener("click", async () => {
    const response = await fetch("/api/attendance/clock-out", { method: "POST" });
    const data = await response.json();
    if (data.success) {
        document.getElementById("clockInBtn").classList.remove("d-none");
        document.getElementById("clockOutBtn").classList.add("d-none");
        stopTimer();

        loadWorkHistory();
        loadWeeklyHours(false);

        localStorage.setItem('clockInStatus', JSON.stringify({
            active: false
        }));

        showToast("You successfuly clocked out!", "success");
    }
});

async function loadWorkHistory() {
    try {
        const response = await fetch("/api/attendance/work-history");
        if (!response.ok) throw new Error("Failed to fetch work history");

        const data = await response.json();
        let tableBody = document.getElementById("workHistory");
        tableBody.innerHTML = "";

        if (!data || data.length === 0 || data.message) {
            tableBody.innerHTML = "<tr><td colspan='3'>No records found</td></tr>";
        } else {
            data.forEach(session => {
                let row = `<tr>
                        <td>${new Date(session.checkInTime).toLocaleString()}</td>
                        <td>${session.checkOutTime ? new Date(session.checkOutTime).toLocaleString() : "Active"}</td>
                        <td>${session.duration}</td>
                    </tr>`;
                tableBody.innerHTML += row;
            });
        }
    } catch (error) {
        showToast("There was an error loading the data.", "error");
        document.getElementById("workHistory").innerHTML = "<tr><td colspan='3'>Error loading data</td></tr>";
    }
}

async function checkSessionStatus() {
    try {
        const response = await fetch("/api/attendance/current-session");
        if (!response.ok) throw new Error("Failed to fetch session status");

        const data = await response.json();
        if (data.active) {
            document.getElementById("clockOutBtn").classList.remove("d-none");
            document.getElementById("clockInBtn").classList.add("d-none");

            let elapsedSeconds = Math.round(data.elapsedTime);
            startTimer(elapsedSeconds);

            localStorage.setItem('clockInStatus', JSON.stringify({
                active: true,
                checkInTime: data.checkInTime
            }));

            loadWeeklyHours(true);
        }
    } catch (error) {
        throw new Error(error);
    }
}

async function loadWeeklyHours(updateLive = false) {
    try {
        const response = await fetch("/api/attendance/weekly-hours");
        if (!response.ok) {
            throw new Error("Failed to fetch weekly hours");
        }

        const data = await response.json();
        let totalHours = data.totalHours || 0;
        let totalMinutes = data.totalMinutes || 0;

        document.getElementById("weeklyHoursText").innerText = `Total Hours Worked: ${totalHours}h ${totalMinutes}m`;

        let progressPercentage = ((totalHours * 60 + totalMinutes) / (40 * 60)) * 100;
        document.getElementById("workProgress").style.width = `${progressPercentage}%`;

        if (totalHours === 0 && totalMinutes === 0) {
            document.getElementById("workProgress").classList.add("bg-warning");
        } else {
            document.getElementById("workProgress").classList.remove("bg-warning");
            document.getElementById("workProgress").classList.add("bg-success");
        }
    } catch (error) {
        throw new Error(error);
    }
}

window.addEventListener('storage', (e) => {
    if (e.key === 'clockInStatus') {
        const status = JSON.parse(e.newValue);
        if (status.active) {
            document.getElementById("clockOutBtn").classList.remove("d-none");
            document.getElementById("clockInBtn").classList.add("d-none");
            startTimer(Math.round((new Date() - new Date(status.checkInTime)) / 1000));
        } else {
            document.getElementById("clockInBtn").classList.remove("d-none");
            document.getElementById("clockOutBtn").classList.add("d-none");
            stopTimer();
        }
    }
});
