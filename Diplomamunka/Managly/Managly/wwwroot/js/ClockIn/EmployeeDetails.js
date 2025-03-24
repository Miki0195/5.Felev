document.addEventListener('DOMContentLoaded', function() {
    const container = document.getElementById('employeeDetailsContainer');
    
    if (container) {
        const userId = container.getAttribute('data-user-id');
        const employeeName = container.getAttribute('data-employee-name');
        
        loadEmployeeDetails(userId);
        
        const saveTimeEditBtn = document.getElementById('saveTimeEdit');
        if (saveTimeEditBtn) {
            saveTimeEditBtn.addEventListener('click', saveTimeEdit);
        }
        
        document.addEventListener('click', function(event) {
            if (event.target.closest('.edit-btn')) {
                const button = event.target.closest('.edit-btn');
                const recordId = button.getAttribute('data-record-id');
                const userId = button.getAttribute('data-user-id');
                openEditModal(recordId, userId, employeeName);
            }
        });
    }
});

async function loadEmployeeDetails(userId) {
    try {
        const response = await fetch(`/api/attendance/employee-details/${userId}`);

        if (!response.ok) {
            throw new Error("There was an error loading employee details.");
        }

        const data = await response.json();

        updateEmployeeStatus(data.isActive, data.currentSessionStart);
        renderWeeklySummary(data.weeklySummary || []);
        renderAttendanceHistory(data.attendanceHistory || [], userId);
    }
    catch (apiError) {
        document.getElementById('weeklySummary').innerHTML =
            `<div class="alert alert-danger">
                    <h5>Error loading employee details</h5>
                    <p>${apiError.message || 'An unexpected error occurred'}</p>
                    <p>Please try refreshing the page or contact support if the issue persists.</p>
                </div>`;

        document.getElementById('attendanceHistory').innerHTML =
            '<tr><td colspan="5" class="text-center text-danger">Error loading attendance data</td></tr>';

        showToast(apiError.message || 'Failed to load employee details', 'error');
    }
}

function updateEmployeeStatus(isActive, sessionStart) {
    const statusText = document.getElementById('statusText');
    const statusDetail = document.getElementById('statusDetail');
    const statusIcon = document.getElementById('statusIcon').querySelector('i');
    const statusContainer = document.getElementById('currentStatus');

    if (isActive && sessionStart) {
        statusText.textContent = 'Currently Working';

        const startTime = new Date(sessionStart);
        const now = new Date();
        const duration = formatDuration(now - startTime);

        statusDetail.textContent = `Started at ${startTime.toLocaleTimeString()} (${duration})`;
        statusIcon.className = 'bi bi-play-circle text-success';
        statusContainer.style.backgroundColor = 'rgba(25, 135, 84, 0.1)';

        if (window.statusTimer) clearInterval(window.statusTimer);

        window.statusTimer = setInterval(() => {
            const now = new Date();
            const updatedDuration = formatDuration(now - startTime);
            statusDetail.textContent = `Started at ${startTime.toLocaleTimeString()} (${updatedDuration})`;
        }, 1000);
    } else {
        statusText.textContent = 'Not Working';
        statusDetail.textContent = 'Currently clocked out';
        statusIcon.className = 'bi bi-pause-circle text-secondary';
        statusContainer.style.backgroundColor = '#f8f9fa';

        if (window.statusTimer) {
            clearInterval(window.statusTimer);
            window.statusTimer = null;
        }
    }
}

function renderWeeklySummary(weeklySummary) {
    const container = document.getElementById('weeklySummary');
    container.innerHTML = '';

    if (!weeklySummary || weeklySummary.length === 0) {
        container.innerHTML = `
                <div class="empty-state">
                    <i class="bi bi-calendar-x"></i>
                    <p>No weekly data available</p>
                </div>`;
        return;
    }

    const summaryContainer = document.createElement('div');
    summaryContainer.className = 'weekly-summary-container';

    weeklySummary.forEach(week => {
        const weekStart = new Date(week.weekStart).toLocaleDateString();
        const weekEnd = new Date(week.weekEnd).toLocaleDateString();

        const completionPercentage = Math.min(Math.round(week.totalHours / 40 * 100), 100);

        let statusClass = 'status-low';
        let statusIcon = 'bi-exclamation-circle';
        let statusText = 'Under Target';

        if (week.totalHours >= 38 && week.totalHours < 40) {
            statusClass = 'status-near';
            statusIcon = 'bi-check-circle';
            statusText = 'Near Target';
        } else if (week.totalHours >= 40 && week.totalHours < 45) {
            statusClass = 'status-complete';
            statusIcon = 'bi-check-circle-fill';
            statusText = 'Complete';
        } else if (week.totalHours >= 45) {
            statusClass = 'status-overtime';
            statusIcon = 'bi-alarm';
            statusText = 'Significant Overtime';
        }

        const weekCard = document.createElement('div');
        weekCard.className = 'weekly-card';
        weekCard.innerHTML = `
                <div class="weekly-card-header">
                    <div class="week-dates">
                        <span class="date-range">${weekStart} - ${weekEnd}</span>
                    </div>
                    <div class="week-status ${statusClass}">
                        <i class="bi ${statusIcon}"></i>
                        <span>${statusText}</span>
                    </div>
                </div>
                
                <div class="weekly-card-body">
                    <div class="donut-chart-container ${statusClass}" style="--percentage: ${completionPercentage}">
                        <div class="donut-chart">
                            <div class="donut-center">
                                <div class="donut-hours">${week.totalHours}h</div>
                                <div class="donut-target">of 40h</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="hours-details">
                        <div class="hours-detail-item">
                            <span class="detail-label">Weekly Target</span>
                            <span class="detail-value">40h</span>
                        </div>
                        <div class="hours-detail-item">
                            <span class="detail-label">Completion</span>
                            <span class="detail-value">${completionPercentage}%</span>
                        </div>
                        ${week.overtime > 0 ? `
                        <div class="hours-detail-item overtime">
                            <span class="detail-label">Overtime</span>
                            <span class="detail-value">+${week.overtime}h</span>
                        </div>
                        ` : ''}
                    </div>
                </div>
            `;

        summaryContainer.appendChild(weekCard);
    });

    container.appendChild(summaryContainer);
}

function renderAttendanceHistory(history, userId) {
    const tableBody = document.getElementById('attendanceHistory');
    tableBody.innerHTML = '';

    if (!history || history.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="5" class="text-center">No attendance records found</td></tr>';
        return;
    }

    history.forEach(record => {
        const row = document.createElement('tr');

        const dateCell = document.createElement('td');
        dateCell.textContent = new Date(record.checkInTime).toLocaleDateString();
        row.appendChild(dateCell);

        const clockInCell = document.createElement('td');
        clockInCell.textContent = new Date(record.checkInTime).toLocaleTimeString();
        row.appendChild(clockInCell);

        const clockOutCell = document.createElement('td');
        clockOutCell.textContent = record.checkOutTime ?
            new Date(record.checkOutTime).toLocaleTimeString() : 'Active';
        row.appendChild(clockOutCell);

        const durationCell = document.createElement('td');
        const hours = Math.floor(record.duration);
        const minutes = Math.round((record.duration - hours) * 60);
        durationCell.textContent = `${hours}h ${minutes}m`;
        row.appendChild(durationCell);

        const actionsCell = document.createElement('td');
        actionsCell.innerHTML = `
                <button class="action-btn edit-btn" data-record-id="${record.id}" data-user-id="${userId}">
                    <i class="bi bi-pencil"></i>
                </button>
            `;
        row.appendChild(actionsCell);

        tableBody.appendChild(row);
    });
}

async function openEditModal(recordId, userId, name) {
    document.getElementById('editUserId').value = userId;
    document.getElementById('employeeName').value = name;
    document.getElementById('editRecordId').value = recordId;

    try {
        const response = await fetch(`/api/attendance/get-record/${recordId}`);
        if (!response.ok) throw new Error('Failed to fetch record details');

        const data = await response.json();

        const clockInDate = new Date(data.checkInTime);
        document.getElementById('clockInDate').value = formatDateTimeForInput(clockInDate);
        if (data.checkOutTime) {
            const clockOutDate = new Date(data.checkOutTime);
            document.getElementById('clockOutDate').value = formatDateTimeForInput(clockOutDate);
        } else {
            document.getElementById('clockOutDate').value = '';
        }

        const timeEditModal = new bootstrap.Modal(document.getElementById('timeEditModal'));
        timeEditModal.show();
    } catch (error) {
        showToast('Failed to load record details', 'error');
    }
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
        loadEmployeeDetails(userId);
    } catch (error) {
        showToast('Failed to update attendance record', 'error');
    }
}

function formatDateTimeForInput(date) {
    const adjustedDate = new Date(date);

    const year = adjustedDate.getFullYear();
    const month = String(adjustedDate.getMonth() + 1).padStart(2, '0');
    const day = String(adjustedDate.getDate()).padStart(2, '0');

    const hours = String(adjustedDate.getHours()).padStart(2, '0');
    const minutes = String(adjustedDate.getMinutes()).padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function formatDuration(durationMs) {
    const seconds = Math.floor(durationMs / 1000);
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}
