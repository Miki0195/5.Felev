let startTime;
let timerInterval;

document.addEventListener("DOMContentLoaded", function() {
    loadWorkHistory();
    checkSessionStatus();
    loadWeeklyHours();
    
    setupTabNavigation();
    
    setupFormHandlers();
});

function setupTabNavigation() {
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');
    const teamViewTab = document.getElementById('teamViewTab');
    
    fetch('/api/attendance/user/is-admin')
        .then(response => response.json())
        .then(data => {
            if (!data.isAdmin) {
                teamViewTab.style.display = 'none';
            }
        })
        .catch(error => {
            teamViewTab.style.display = 'none';
        });
    
    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetTab = btn.getAttribute('data-tab');
            
            tabBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            tabContents.forEach(content => {
                content.classList.remove('active');
            });
            document.getElementById(`${targetTab}-tab`).classList.add('active');
            
            if (targetTab === 'team') {
                loadTeamData();
            }
        });
    });
}

function setupFormHandlers() {
    const teamSearch = document.getElementById('teamSearch');
    if (teamSearch) {
        teamSearch.addEventListener('input', filterTeamMembers);
    }
    
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', filterTeamMembers);
    }
    
    document.getElementById("clockInBtn").addEventListener("click", clockIn);
    document.getElementById("clockOutBtn").addEventListener("click", clockOut);
    
    const saveTimeEditBtn = document.getElementById('saveTimeEdit');
    if (saveTimeEditBtn) {
        saveTimeEditBtn.addEventListener('click', saveTimeEdit);
    }
}

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

async function clockIn() {
    try {
        const response = await fetch("/api/attendance/clock-in", { method: "POST" });
        const data = await response.json();
        
        if (data.success) {
            document.getElementById("clockOutBtn").classList.remove("d-none");
            document.getElementById("clockInBtn").classList.add("d-none");
            startTimer();
            loadWorkHistory();
            loadWeeklyHours();
            showToast("You have successfully clocked in", "success");
        } else {
            showToast(data.error || "Failed to clock in", "error");
        }
    } catch (error) {
        showToast("Error connecting to the server", "error");
    }
}

async function clockOut() {
    try {
        const response = await fetch("/api/attendance/clock-out", { method: "POST" });
        const data = await response.json();
        
        if (data.success) {
            document.getElementById("clockInBtn").classList.remove("d-none");
            document.getElementById("clockOutBtn").classList.add("d-none");
            stopTimer();
            loadWorkHistory();
            loadWeeklyHours();
            showToast("You have successfully clocked out", "success");
        } else {
            showToast(data.error || "Failed to clock out", "error");
        }
    } catch (error) {
        showToast("Error connecting to the server", "error");
    }
}

async function checkSessionStatus() {
    try {
        const response = await fetch("/api/attendance/current-session");
        const data = await response.json();
        
        if (data.active) {
            document.getElementById("clockOutBtn").classList.remove("d-none");
            document.getElementById("clockInBtn").classList.add("d-none");
            startTimer(Math.round(data.elapsedTime));
        }
    } catch (error) {
        showToast("Error checking session.", "error");
    }
}

async function loadWorkHistory() {
    try {
        const response = await fetch("/api/attendance/work-history");
        if (!response.ok) throw new Error("Failed to fetch work history");

        const data = await response.json();
        let tableBody = document.getElementById("workHistory");
        tableBody.innerHTML = "";

        if (!data || data.length === 0 || data.message) {
            tableBody.innerHTML = "<tr><td colspan='3'>No records found</td></tr>";
            return;
        }
        
        data.forEach(session => {
            const row = document.createElement('tr');
            
            const clockInCell = document.createElement('td');
            clockInCell.textContent = new Date(session.checkInTime).toLocaleString();
            row.appendChild(clockInCell);
            
            const clockOutCell = document.createElement('td');
            clockOutCell.textContent = session.checkOutTime ? 
                new Date(session.checkOutTime).toLocaleString() : "Active";
            row.appendChild(clockOutCell);
            
            const durationCell = document.createElement('td');
            durationCell.textContent = session.duration;
            row.appendChild(durationCell);
            
            tableBody.appendChild(row);
        });
    } catch (error) {
        showToast("Error loading history.", "error")
    }
}

async function loadWeeklyHours() {
    try {
        const response = await fetch("/api/attendance/weekly-hours");
        if (!response.ok) throw new Error("Failed to fetch weekly hours");

        const data = await response.json();
        let totalHours = data.totalHours || 0;
        let totalMinutes = data.totalMinutes || 0;

        document.getElementById("weeklyHoursText").innerText = 
            `Total Hours Worked: ${totalHours}h ${totalMinutes}m`;

        let progressPercentage = ((totalHours * 60 + totalMinutes) / (40 * 60)) * 100;
        document.getElementById("workProgress").style.width = `${progressPercentage}%`;

        const progressBar = document.getElementById("workProgress");
        progressBar.classList.remove("bg-warning", "bg-success");
        if (totalHours === 0 && totalMinutes === 0) {
            progressBar.classList.add("bg-warning");
        } else {
            progressBar.classList.add("bg-success");
        }
    } catch (error) {
        showToast("Error loading weekly hours.", "error");
    }
}

async function loadTeamData() {
    try {
        document.getElementById('teamTableBody').innerHTML = 
            '<tr><td colspan="5" class="text-center">Loading team data...</td></tr>';
        
        const response = await fetch('/api/attendance/team-overview');
        if (!response.ok) throw new Error("Failed to fetch team data");
        
        const data = await response.json();
        
        document.getElementById('activeCount').textContent = data.stats.activeCount;
        document.getElementById('weeklyAvg').textContent = `${data.stats.weeklyAverageHours}h`;
        document.getElementById('overtimeCount').textContent = `${data.stats.totalOvertimeHours}h`;
        
        const tableBody = document.getElementById('teamTableBody');
        tableBody.innerHTML = '';
        
        if (!data.teamMembers || data.teamMembers.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5" class="text-center">No team members found</td></tr>';
            return;
        }
        
        data.teamMembers.forEach(member => {
            const row = document.createElement('tr');
            row.setAttribute('data-user-id', member.userId);
            row.setAttribute('data-status', member.isActive ? 'active' : 'inactive');
            row.setAttribute('data-name', member.name.toLowerCase());
            
            const employeeCell = document.createElement('td');
            employeeCell.innerHTML = `
                <div class="employee-info">
                    <div class="employee-avatar">
                        ${member.profilePicture ? 
                        `<img src="${member.profilePicture}" alt="${member.name}">` : 
                        `<i class="bi bi-person"></i>`}
                    </div>
                    <div class="employee-details">
                        <p class="employee-name">${member.name}</p>
                        <p class="employee-position">${member.position}</p>
                    </div>
                </div>
            `;
            
            const statusCell = document.createElement('td');
            statusCell.innerHTML = `
                <span class="status-badge ${member.isActive ? 'status-active' : 'status-inactive'}">
                    ${member.isActive ? 'Working' : 'Offline'}
                </span>
            `;
            
            const sessionCell = document.createElement('td');
            if (member.isActive) {
                sessionCell.innerHTML = `
                    <span class="session-timer" data-start-time="${member.currentSessionStart}">
                        ${formatDuration(new Date() - new Date(member.currentSessionStart))}
                    </span>
                `;
            } else {
                sessionCell.textContent = 'Not Active';
            }
            
            const weeklyHoursCell = document.createElement('td');
            const overtimeHours = Math.max(0, member.weeklyHours - 40);
            const regularHours = Math.min(40, member.weeklyHours);
            const percentage = (regularHours / 40) * 100;

            weeklyHoursCell.innerHTML = `
                <div class="hours-container">
                    <div class="hours-value">${Math.round(member.weeklyHours * 10) / 10}h</div>
                    <div class="hours-bar-container">
                        <div class="hours-bar-regular" style="width: ${percentage}%"></div>
                        ${overtimeHours > 0 ? `<div class="hours-bar-overtime" style="width: ${(overtimeHours / 10) * 100}%"></div>` : ''}
                    </div>
                    ${overtimeHours > 0 ?
                                `<div class="overtime-badge">+${Math.round(overtimeHours * 10) / 10}h OT</div>` :
                                ''}
                </div>
            `;
            
            const actionsCell = document.createElement('td');
            actionsCell.innerHTML = `
                <div class="action-buttons">
                    <a href="/EmployeeDetails/${member.userId}" class="action-btn view-btn" title="View Details">
                        <i class="bi bi-eye"></i>
                    </a>
                </div>
            `;
            
            row.appendChild(employeeCell);
            row.appendChild(statusCell);
            row.appendChild(sessionCell);
            row.appendChild(weeklyHoursCell);
            row.appendChild(actionsCell);
            
            tableBody.appendChild(row);
        });
        
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const userId = btn.getAttribute('data-user-id');
                const userName = btn.getAttribute('data-user-name');
                openTimeEdit(userId, userName);
            });
        });
        
        startTimerUpdates();
    } catch (error) {
        showToast("Error loading team data.", "error")
    }
}

function startTimerUpdates() {
    if (window.teamUpdateInterval) {
        clearInterval(window.teamUpdateInterval);
    }
    
    window.teamUpdateInterval = setInterval(() => {
        const sessionTimers = document.querySelectorAll('.session-timer[data-start-time]');
        
        sessionTimers.forEach(timer => {
            const startTime = new Date(timer.getAttribute('data-start-time'));
            const duration = new Date() - startTime;
            timer.textContent = formatDuration(duration);
        });
    }, 1000);
}

function formatDuration(durationMs) {
    const seconds = Math.floor(durationMs / 1000);
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}

function filterTeamMembers() {
    const searchTerm = document.getElementById('teamSearch').value.toLowerCase();
    const statusFilter = document.getElementById('statusFilter').value;
    
    const rows = document.querySelectorAll('#teamTableBody tr[data-user-id]');
    
    rows.forEach(row => {
        const userName = row.getAttribute('data-name');
        const status = row.getAttribute('data-status');
        
        const matchesSearch = userName.includes(searchTerm);
        const matchesStatus = statusFilter === 'all' || 
                             (statusFilter === 'active' && status === 'active') || 
                             (statusFilter === 'inactive' && status === 'inactive');
        
        row.style.display = matchesSearch && matchesStatus ? '' : 'none';
    });
}

async function openTimeEdit(userId, name) {
    document.getElementById('employeeName').value = name;
    document.getElementById('editUserId').value = userId;
    
    try {
        const response = await fetch(`/api/attendance/latest-record/${userId}`);
        if (!response.ok) throw new Error("Failed to fetch record");
        
        const data = await response.json();
        
        if (data && data.id) {
            document.getElementById('editRecordId').value = data.id;
            
            const clockInDate = new Date(data.checkInTime);
            document.getElementById('clockInDate').value = formatDateTimeForInput(clockInDate);
            
            if (data.checkOutTime) {
                const clockOutDate = new Date(data.checkOutTime);
                document.getElementById('clockOutDate').value = formatDateTimeForInput(clockOutDate);
            } else {
                document.getElementById('clockOutTime').value = '';
            }
        } else {
            document.getElementById('editRecordId').value = '';
            document.getElementById('clockInDate').value = '';
            document.getElementById('clockOutDate').value = '';
        }
        
        const timeEditModal = new bootstrap.Modal(document.getElementById('timeEditModal'));
        timeEditModal.show();
    } catch (error) {
        showToast('Failed to load attendance record', 'error');
    }
}

function formatDateTimeForInput(date) {
    return date.toISOString().slice(0, 16);
}

async function saveTimeEdit() {
    const userId = document.getElementById('editUserId').value;
    const recordId = document.getElementById('editRecordId').value;
    const clockInDate = document.getElementById('clockInDate').value;
    const clockOutDate = document.getElementById('clockOutDate').value;
    const notes = document.getElementById('editNotes').value;
    
    if (!clockInDate) {
        showToast('Clock-in time is required', 'error');
        return;
    }
    
    try {
        const response = await fetch('/api/attendance/update-time', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                recordId: recordId,
                userId: userId,
                checkInTime: clockInDate,
                checkOutTime: clockOutDate || null,
                adminNotes: notes
            })
        });
        
        if (!response.ok) {
            throw new Error('Failed to update attendance record');
        }
        
        const timeEditModal = bootstrap.Modal.getInstance(document.getElementById('timeEditModal'));
        timeEditModal.hide();
        
        showToast('Attendance record updated successfully', 'success');
        loadTeamData();
    } catch (error) {
        showToast('Failed to update attendance record', 'error');
    }
}