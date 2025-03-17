// ========== GLOBAL VARIABLES ==========
let editMode = false;
let selectedDates = [];
let calendar = null;
let domCache = {};
let currentUserId = '';
let remainingVacationDays = 0;
let totalVacationDays = 0;
let usedVacationDays = 0;
let vacationInfo = {
    totalDays: 0,
    usedDays: 0,
    remainingDays: 0
};

// ========== INITIALIZATION ==========
document.addEventListener('DOMContentLoaded', function () {
    cacheDOM();
    
    loadNotifications();
    initializeCalendar();
    setupEventListeners();
    fetchUserVacationInfo();
});

/**
 * Cache frequently accessed DOM elements for better performance
 */
function cacheDOM() {
    domCache = {
        calendar: document.getElementById('calendar'),
        editScheduleBtn: document.getElementById('editScheduleBtn'),
        doneEditingBtn: document.getElementById('doneEditingBtn'),
        cancelSelection: document.getElementById('cancelSelection'),
        selectionCounter: document.getElementById('selectionCounter'),
        legendText: document.querySelector('.legend small'),
        scheduleDate: document.getElementById('scheduleDate'),
        scheduleTime: document.getElementById('scheduleTime'),
        scheduleComment: document.getElementById('scheduleComment'),
        leaveRequestModal: document.getElementById('leaveRequestModal'),
        leaveStartDate: document.getElementById('leaveStartDate'),
        leaveEndDate: document.getElementById('leaveEndDate'),
        leaveType: document.getElementById('leaveType'),
        leaveReason: document.getElementById('leaveReason'),
        submitLeaveRequest: document.getElementById('submitLeaveRequest'),
        vacationDaysInfo: document.getElementById('vacationDaysInfo')
    };
}

/**
 * Initializes the calendar with configuration
 */
function initializeCalendar() {
    calendar = new FullCalendar.Calendar(domCache.calendar, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: ''
        },
        events: fetchCalendarEvents,
        eventDidMount: renderCalendarEvent,
        eventClick: handleEventClick,
        selectable: true,
        selectMirror: true,
        unselectAuto: false,
        select: handleDateSelect,
        lazyFetching: true,
        eventLimit: true,
        eventLimitClick: 'popover'
    });

    calendar.render();
}

/**
 * Sets up event listeners for buttons and other interactive elements
 */
function setupEventListeners() {
    domCache.editScheduleBtn.addEventListener("click", startVacationSelection);
    domCache.doneEditingBtn.addEventListener("click", submitVacationSelection);
    domCache.cancelSelection.addEventListener("click", cancelVacationSelection);

    if (domCache.leaveRequestModal) {
        domCache.leaveRequestModal.addEventListener('show.bs.modal', function () {
            resetLeaveRequestForm();
        });
    }

    if (domCache.leaveStartDate && domCache.leaveEndDate) {
        domCache.leaveStartDate.addEventListener('change', updateVacationDaysCount);
        domCache.leaveEndDate.addEventListener('change', updateVacationDaysCount);
        domCache.leaveType.addEventListener('change', updateVacationDaysCount);
    }

    if (domCache.submitLeaveRequest) {
        domCache.submitLeaveRequest.addEventListener('click', handleLeaveRequestSubmission);
    }
}

// ========== EVENT HANDLERS ==========
/**
 * Handles the click event on calendar events
 * @param {Object} info - The event information
 */
function handleEventClick(info) {
    const event = info.event;
    const modal = new bootstrap.Modal(document.getElementById('scheduleModal'));

    const date = new Date(event.start);
    const formattedDate = date.toLocaleDateString('en-US', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });

    const eventTitle = info.event.title;
    const parts = eventTitle.split("\n");
    let timeText = parts[0] || "";
    let commentText = parts.length > 1 ? parts[1] : "";
    let statusText = "";

    if (eventTitle.includes("Vacation")) {
        const statusMatch = eventTitle.match(/\((.*?)\)/);
        statusText = statusMatch ? statusMatch[1] : "Approved";
        
        domCache.scheduleDate.textContent = formattedDate;
        domCache.scheduleTime.textContent = timeText.replace(/\s*\(.*?\)/, "");
        domCache.scheduleComment.textContent = statusText;
    } else {
        domCache.scheduleDate.textContent = formattedDate;
        domCache.scheduleTime.textContent = timeText;
        domCache.scheduleComment.textContent = commentText || 'No comment';
    }

    modal.show();
}

/**
 * Handles date selection in the calendar
 * @param {Object} info - The selection information
 */
function handleDateSelect(info) {
    if (!editMode) return;

    const clickedDate = info.startStr; 
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (new Date(clickedDate) < today) {
        showCannotAddPastVacationModal();
        calendar.unselect();
        return;
    }

    const existingEvent = findEventOnDate(clickedDate);

    if (existingEvent) {
        if (existingEvent.title.includes("Vacation")) {
            showVacationExistsModal();
        } else {
            showShiftExistsModal();
        }
        calendar.unselect();
        return;
    }

    toggleDateSelection(clickedDate);
    
    updateSelectionCount();
    
    calendar.unselect();
}

/**
 * Efficiently finds an event on a specific date
 * @param {string} dateStr - The date string to check
 * @returns {Object|null} The event object or null if not found
 */
function findEventOnDate(dateStr) {
    const events = calendar.getEvents();
    return events.find(event => event.startStr === dateStr);
}

/**
 * Starts the vacation selection mode
 */
function startVacationSelection() {
    selectedDates = [];

    editMode = true;
    this.classList.add("d-none");
    domCache.doneEditingBtn.classList.remove("d-none");
    domCache.legendText.textContent = 'Click to select vacation dates';

    const days = document.querySelectorAll('.fc-day:not(.fc-day-past)');
    
    days.forEach(day => {
        day.style.cursor = 'pointer';
        day.classList.add('selectable');
        day.style.backgroundColor = '';
        const checkmark = day.querySelector('.vacation-checkmark');
        if (checkmark) checkmark.remove();
    });
}

/**
 * Submits the vacation selection
 */
function submitVacationSelection() {
    if (selectedDates.length === 0) {
        showToast("Please select at least one day for your vacation request.", "warning");
        return;
    }

    exitSelectionMode();
    
    submitLeaveRequest(selectedDates);
}

/**
 * Cancels the vacation selection process
 */
function cancelVacationSelection() {
    const tooltip = document.querySelector('.vacation-tooltip');
    if (tooltip) tooltip.remove();

    editMode = false;
    selectedDates = [];
    
    domCache.doneEditingBtn.classList.add("d-none");
    domCache.editScheduleBtn.classList.remove("d-none");
    document.querySelector('.vacation-selection-overlay').classList.add('d-none');

    document.querySelectorAll('.fc-day').forEach(day => {
        day.classList.remove('selectable', 'selected', 'selection-start', 'selection-end');
        day.style.backgroundColor = "";
        const checkmark = day.querySelector('.vacation-checkmark');
        if (checkmark) checkmark.remove();
    });
}

/**
 * Exits selection mode and resets UI
 */
function exitSelectionMode() {
    editMode = false;
    domCache.doneEditingBtn.classList.add("d-none");
    domCache.editScheduleBtn.classList.remove("d-none");
    domCache.selectionCounter.classList.add('d-none');
    domCache.legendText.textContent = 'Click on a day to view details';

    document.querySelectorAll('.fc-day').forEach(day => {
        day.style.cursor = '';
        day.classList.remove('selectable');
        day.style.backgroundColor = '';
        const checkmark = day.querySelector('.vacation-checkmark');
        if (checkmark) checkmark.remove();
    });
}

// ========== DATA OPERATIONS ==========
/**
 * Fetches calendar events from the API
 * @param {Object} fetchInfo - FullCalendar fetch info
 * @param {Function} successCallback - Success callback
 * @param {Function} failureCallback - Failure callback
 */
async function fetchCalendarEvents(fetchInfo, successCallback, failureCallback) {
    try {
        const params = new URLSearchParams({
            start: fetchInfo.startStr,
            end: fetchInfo.endStr
        });
        
        const [scheduleResponse, leaveResponse] = await Promise.all([
            fetch(`/api/schedule/myschedule?${params}`),
            fetch(`/api/leave/myleaves?${params}`)
        ]);

        if (!scheduleResponse.ok) {
            throw new Error(`Schedule API error: ${scheduleResponse.status}`);
        }
        if (!leaveResponse.ok) {
            throw new Error(`Leave API error: ${leaveResponse.status}`);
        }

        const [scheduleEvents, leaveEvents] = await Promise.all([
            scheduleResponse.json(),
            leaveResponse.json()
        ]);

        if (!Array.isArray(scheduleEvents) || !Array.isArray(leaveEvents)) {
            throw new Error("Invalid API response format");
        }

        successCallback([...scheduleEvents, ...leaveEvents]);
    } catch (error) {
        showToast("Failed to load schedule data. Please try refreshing the page.", "error");
        failureCallback(error);
    }
}

/**
 * Submits a leave request to the server
 * @param {Array} selectedDates - Array of selected date strings
 */
function submitLeaveRequest(selectedDates) {
    let workingDays = 0;
    for (let dateStr of selectedDates) {
        const date = new Date(dateStr);
        if (date.getDay() !== 0 && date.getDay() !== 6) { 
            workingDays++;
        }
    }
    
    if (workingDays > remainingVacationDays) {
        showToast(`You don't have enough vacation days. You have ${remainingVacationDays} days available, but you're requesting ${workingDays} working days.`, "error");
        return;
    }

    const leaveRequests = selectedDates.map(date => ({
        LeaveDate: new Date(date).toISOString().split("T")[0],
        Reason: "Vacation",
        MedicalProof: ""
    }));

    fetch("/api/leave/request", {
        method: "POST",
        headers: { 
            "Content-Type": "application/json"
        },
        body: JSON.stringify(leaveRequests)
    })
    .then(response => {
        if (!response.ok) {
            return response.json().then(data => {
                throw new Error(data.message || 'Failed to submit leave request');
            });
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            clearSelectedDatesHighlighting();
            
            selectedDates = [];
            domCache.selectionCounter.classList.add('d-none');

            calendar.refetchEvents();
            
            showToast("Vacation request submitted successfully!", "success");
            
            fetchUserVacationInfo();
        } else {
            showToast(data.message || "Failed to submit leave request.", "error");
        }
    })
    .catch(error => {
        showToast(error.message || "An error occurred while submitting your request.", "error");
    });
}

// ========== UI OPERATIONS ==========
/**
 * Renders a calendar event with styling
 * @param {Object} info - FullCalendar event info
 */
function renderCalendarEvent(info) {
    if (info.el.querySelector(".event-content")) return;
    
    const eventTitle = info.event.title;
    const parts = eventTitle.split("\n");
    let timeText = parts[0] || "";
    const commentText = parts.length > 1 ? parts[1] : "";

    const fragment = document.createDocumentFragment();
    const contentDiv = document.createElement('div');
    contentDiv.className = 'event-content';
    
    if (eventTitle.includes("Vacation")) {
        const statusMatch = eventTitle.match(/\((.*?)\)/);
        const status = statusMatch ? statusMatch[1].trim() : "";

        timeText = timeText.replace(/\s*\(.*?\)/, "");

        const color = info.event.backgroundColor || (status === "" ? "green" : "orange");
        info.el.style.backgroundColor = color;
        info.el.style.borderColor = color;

        const timeDiv = document.createElement('div');
        timeDiv.style.color = 'white';
        timeDiv.style.marginBottom = '4px';
        timeDiv.innerHTML = `<strong>${timeText}</strong>`;
        contentDiv.appendChild(timeDiv);
        
        const statusDiv = document.createElement('div');
        statusDiv.style.fontSize = '0.8rem';
        statusDiv.style.color = 'white';
        statusDiv.style.opacity = '0.9';
        statusDiv.style.borderTop = '1px solid rgba(255,255,255,0.2)';
        statusDiv.style.paddingTop = '4px';
        statusDiv.textContent = status || 'Approved';
        contentDiv.appendChild(statusDiv);
    } 
    else {
        info.el.style.backgroundColor = '#4299e1';
        info.el.style.borderColor = '#2b6cb0';
        
        const timeDiv = document.createElement('div');
        timeDiv.style.color = 'white';
        timeDiv.style.marginBottom = '4px';
        timeDiv.innerHTML = `<strong>${timeText}</strong>`;
        contentDiv.appendChild(timeDiv);
        
        if (commentText) {
            const commentDiv = document.createElement('div');
            commentDiv.style.fontSize = '0.8rem';
            commentDiv.style.color = 'white';
            commentDiv.style.opacity = '0.9';
            commentDiv.style.borderTop = '1px solid rgba(255,255,255,0.2)';
            commentDiv.style.paddingTop = '4px';
            commentDiv.textContent = commentText;
            contentDiv.appendChild(commentDiv);
        }
    }
    
    info.el.style.transition = 'all 0.2s ease';
    
    info.el.addEventListener('mouseenter', handleEventHoverIn);
    info.el.addEventListener('mouseleave', handleEventHoverOut);
    
    fragment.appendChild(contentDiv);
    info.el.innerHTML = '';
    info.el.appendChild(fragment);
}

/**
 * Handles mouse enter event for calendar events
 * @param {Event} e - The mouse event
 */
function handleEventHoverIn(e) {
    this.style.transform = 'scale(1.02)';
    this.style.zIndex = '1';
}

/**
 * Handles mouse leave event for calendar events
 * @param {Event} e - The mouse event
 */
function handleEventHoverOut(e) {
    this.style.transform = 'scale(1)';
    this.style.zIndex = '';
}

/**
 * Toggles selection for a specific date
 * @param {string} dateStr - The date string in YYYY-MM-DD format
 */
function toggleDateSelection(dateStr) {
    const dayEl = document.querySelector(`.fc-day[data-date="${dateStr}"]`);
    if (!dayEl) return;
    
    const existingIndex = selectedDates.indexOf(dateStr);

    if (existingIndex === -1) {
        selectedDates.push(dateStr);
        
        dayEl.style.backgroundColor = 'rgba(246, 173, 85, 0.2)';
        dayEl.style.position = 'relative';

        const checkmark = document.createElement('div');
        checkmark.innerHTML = '✓';
        checkmark.className = 'vacation-checkmark';
        checkmark.style.position = 'absolute';
        checkmark.style.top = '50%';
        checkmark.style.left = '50%';
        checkmark.style.transform = 'translate(-50%, -50%)';
        checkmark.style.color = '#dd6b20';
        checkmark.style.fontSize = '1.2rem';
        dayEl.appendChild(checkmark);
    } else {
        selectedDates.splice(existingIndex, 1);
        
        dayEl.style.backgroundColor = '';
        const checkmark = dayEl.querySelector('.vacation-checkmark');
        if (checkmark) checkmark.remove();
    }
}

/**
 * Updates the selection counter display
 */
function updateSelectionCount() {
    const count = selectedDates.length;

    if (count > 0) {
        let workingDays = 0;
        for (let dateStr of selectedDates) {
            const date = new Date(dateStr);
            if (date.getDay() !== 0 && date.getDay() !== 6) { 
                workingDays++;
            }
        }
        
        let counterHtml = `
            <i class="fas fa-calendar-check"></i>
            ${count} day${count !== 1 ? 's' : ''} selected (${workingDays} working day${workingDays !== 1 ? 's' : ''})
        `;
        
        if (workingDays > remainingVacationDays) {
            counterHtml += `
                <div class="text-danger mt-1">
                    <i class="fas fa-exclamation-triangle"></i>
                    Warning: You only have ${remainingVacationDays} vacation days remaining!
                </div>
            `;
        }
        
        domCache.selectionCounter.innerHTML = counterHtml;
        domCache.selectionCounter.classList.remove('d-none');
    } else {
        domCache.selectionCounter.classList.add('d-none');
    }
}

/**
 * Clears highlighting from all selected dates
 */
function clearSelectedDatesHighlighting() {
    selectedDates.forEach(date => {
        const dayEl = document.querySelector(`.fc-day[data-date="${date}"]`);
        if (dayEl) {
            dayEl.style.backgroundColor = "";
            const checkmark = dayEl.querySelector('.vacation-checkmark');
            if (checkmark) checkmark.remove();
        }
    });
}

// ========== MODAL OPERATIONS ==========
/**
 * Shows the modal indicating a vacation already exists on the selected date
 */
function showVacationExistsModal() {
    const modal = new bootstrap.Modal(document.getElementById("vacationExistsModal"));
    modal.show();
}

/**
 * Shows the modal indicating a shift already exists on the selected date
 */
function showShiftExistsModal() {
    const modal = new bootstrap.Modal(document.getElementById("shiftExistsModal"));
    modal.show();
}

/**
 * Shows the modal indicating that past dates cannot be selected for vacation
 */
function showCannotAddPastVacationModal() {
    const modal = new bootstrap.Modal(document.getElementById("cannotAddPastVacationModal"));
    modal.show();
}

/**
 * Fetch current user's vacation information
 */
function fetchUserVacationInfo() {
    fetch('/api/schedule/myvacationinfo')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            currentUserId = data.id;
            totalVacationDays = data.totalVacationDays;
            usedVacationDays = data.usedVacationDays;
            remainingVacationDays = data.remainingVacationDays;
            
            vacationInfo.totalDays = totalVacationDays;
            vacationInfo.usedDays = usedVacationDays;
            vacationInfo.remainingDays = remainingVacationDays;
            
            updateVacationDaysDisplay();
        })
        .catch(error => {
            console.error('Error fetching user vacation info:', error);
            showToast("Failed to load vacation information. Please refresh the page.", "error");
        });
}

/**
 * Update the vacation days display in the UI
 */
function updateVacationDaysDisplay() {
    if (domCache.vacationDaysInfo) {
        const percentage = (vacationInfo.usedDays / vacationInfo.totalDays) * 100 || 0;
        
        domCache.vacationDaysInfo.innerHTML = `
            <div class="card mb-3">
                <div class="card-body">
                    <h5 class="card-title">Vacation Days</h5>
                    <div class="row">
                        <div class="col-md-4">
                            <p class="mb-1">Total: <strong>${vacationInfo.totalDays}</strong></p>
                        </div>
                        <div class="col-md-4">
                            <p class="mb-1">Used: <strong>${vacationInfo.usedDays}</strong></p>
                        </div>
                        <div class="col-md-4">
                            <p class="mb-1">Remaining: <strong>${vacationInfo.remainingDays}</strong></p>
                        </div>
                    </div>
                    <div class="progress mt-2">
                        <div class="progress-bar ${percentage > 75 ? 'bg-danger' : percentage > 50 ? 'bg-warning' : 'bg-success'}" 
                             role="progressbar" 
                             style="width: ${percentage}%" 
                             aria-valuenow="${vacationInfo.usedDays}" 
                             aria-valuemin="0" 
                             aria-valuemax="${vacationInfo.totalDays}">
                            ${Math.round(percentage)}%
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
}

/**
 * Updates the count of the vacation days
 */
function updateVacationDaysCount() {
    const startDate = new Date(domCache.leaveStartDate.value);
    const endDate = new Date(domCache.leaveEndDate.value);
    const leaveType = domCache.leaveType.value;
    
    const messageElement = document.getElementById('leaveDaysMessage');
    if (messageElement) {
        messageElement.remove();
    }
    
    if (isNaN(startDate.getTime()) || isNaN(endDate.getTime()) || startDate > endDate) {
        return;
    }
    
    const workingDays = countWorkingDays(startDate, endDate);
    
    const newMessage = document.createElement('div');
    newMessage.id = 'leaveDaysMessage';
    newMessage.className = 'alert mt-3';
    
    if (leaveType === 'Vacation') {
        if (workingDays > vacationInfo.remainingDays) {
            newMessage.className += ' alert-danger';
            newMessage.innerHTML = `
                <i class="fas fa-exclamation-circle"></i> 
                This request is for <strong>${workingDays}</strong> working days, but you only have 
                <strong>${vacationInfo.remainingDays}</strong> vacation days remaining.
            `;
        } else {
            newMessage.className += ' alert-info';
            newMessage.innerHTML = `
                <i class="fas fa-info-circle"></i> 
                This request is for <strong>${workingDays}</strong> working days. 
                You have <strong>${vacationInfo.remainingDays}</strong> vacation days remaining.
            `;
        }
    } else {
        newMessage.className += ' alert-info';
        newMessage.innerHTML = `
            <i class="fas fa-info-circle"></i> 
            This request is for <strong>${workingDays}</strong> working days.
        `;
    }
    
    domCache.leaveReason.parentNode.after(newMessage);
}

/**
 * Counts have many actual working days the user selected for vacation
 */
function countWorkingDays(startDate, endDate) {
    let workingDays = 0;
    const currentDate = new Date(startDate);
    
    while (currentDate <= endDate) {
        const dayOfWeek = currentDate.getDay();
        if (dayOfWeek !== 0 && dayOfWeek !== 6) {
            workingDays++;
        }
        
        currentDate.setDate(currentDate.getDate() + 1);
    }
    
    return workingDays;
}