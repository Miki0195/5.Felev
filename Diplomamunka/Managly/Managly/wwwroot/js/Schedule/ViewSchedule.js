// ========== GLOBAL VARIABLES ==========
let editMode = false;
let selectedDates = [];
let calendar = null;
let domCache = {};

// ========== INITIALIZATION ==========
document.addEventListener('DOMContentLoaded', function () {
    cacheDOM();
    
    loadNotifications();
    initializeCalendar();
    setupEventListeners();
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
        scheduleComment: document.getElementById('scheduleComment')
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
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            clearSelectedDatesHighlighting();
            
            selectedDates = [];
            domCache.selectionCounter.classList.add('d-none');

            calendar.refetchEvents();
            
            showToast("Vacation request submitted successfully!", "success");
        } else {
            showToast("Failed to submit leave request.", "error");
        }
    })
    .catch(error => {
        showToast("An error occurred while submitting your request.", "error");
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
        domCache.selectionCounter.innerHTML = `
            <i class="fas fa-calendar-check"></i>
            ${count} day${count !== 1 ? 's' : ''} selected
        `;
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



