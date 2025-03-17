// ========== GLOBAL VARIABLES ==========
let selectedWorkers = new Set();
let selectedProjects = new Set();
let calendar = null;

const colors = {
    '#3498db': '#2980b9',  // blue
    '#e74c3c': '#c0392b',  // red
    '#2ecc71': '#27ae60',  // green
    '#f39c12': '#d35400',  // orange
    '#9b59b6': '#8e44ad',  // purple
    '#1abc9c': '#16a085'   // turquoise
};

let domCache = {};

// ========== INITIALIZATION ==========
document.addEventListener('DOMContentLoaded', function () {
    cacheDOM();
    
    loadNotifications();
    loadActiveProjects();
    initializeWorkerSearch();
    
    setupEventListeners();
});

/**
 * Cache frequently accessed DOM elements for better performance
 */
function cacheDOM() {
    domCache = {
        searchResults: document.getElementById('searchResults'),
        workerSearch: document.getElementById('workerSearch'),
        selectedWorkers: document.getElementById('selectedWorkers'),
        projectFilter: document.getElementById('projectFilter'),
        activeFilters: document.getElementById('activeFilters'),
        activeFiltersText: document.querySelector('.active-filters-container .text-muted'),
        exportButtons: document.getElementById('exportButtons'),
        mergedCalendar: document.getElementById('mergedCalendar')
    };
}

/**
 * Set up event listeners for various elements
 */
function setupEventListeners() {
    document.getElementById('exportPdfBtn').addEventListener('click', exportToPDF);
    document.getElementById('exportExcelBtn').addEventListener('click', exportToExcel);
    
    domCache.projectFilter.addEventListener('change', handleProjectFilterChange);
    
    document.addEventListener('click', function(e) {
        if (!domCache.workerSearch.contains(e.target) && !domCache.searchResults.contains(e.target)) {
            domCache.searchResults.classList.add('d-none');
        }
    });
}

// ========== FILTER MANAGEMENT ==========
/**
 * Removes a worker from the selected workers list
 * @param {string} workerId - The ID of the worker to remove
 */
function removeWorker(workerId) {
    selectedWorkers.delete(workerId);
    const chip = document.querySelector(`.user-chip[data-worker-id="${workerId}"]`);
    if (chip) chip.remove();

    if (!shouldShowCalendar()) {
        if (calendar) {
            calendar.destroy();
            calendar = null;
            hideExportButtons();
        }
    } else {
        updateCalendarEvents();
    }
    updateActiveFilters();
}

/**
 * Removes a project from the selected projects list
 * @param {string} projectId - The ID of the project to remove
 */
function removeProject(projectId) {
    const option = Array.from(domCache.projectFilter.options).find(opt => opt.value === projectId);
    if (option) {
        option.selected = false;
    }
    domCache.projectFilter.dispatchEvent(new Event('change'));
    hideExportButtons();
}

/**
 * Clears all active filters (workers and projects)
 */
function clearAllFilters() {
    Array.from(selectedWorkers).forEach(workerId => {
        removeWorker(workerId);
    });

    Array.from(domCache.projectFilter.selectedOptions).forEach(option => {
        option.selected = false;
    });
    domCache.projectFilter.dispatchEvent(new Event('change'));

    updateActiveFilters();
    hideExportButtons();
}

/**
 * Adds a worker to the selected workers list
 * @param {Object} worker - The worker object to add
 */
function addWorker(worker) {
    if (selectedWorkers.has(worker.id)) return;

    Array.from(domCache.projectFilter.selectedOptions).forEach(option => {
        option.selected = false;
    });
    selectedProjects.clear();

    const workerColor = getWorkerColor(worker.id);
    selectedWorkers.add(worker.id);
    
    const chip = document.createElement('div');
    chip.className = 'user-chip';
    chip.dataset.workerId = worker.id;
    chip.style.backgroundColor = workerColor;
    chip.style.borderColor = colors[workerColor];
    chip.innerHTML = `
        ${worker.name} ${worker.lastName}
        <span class="remove-user">×</span>
    `;

    chip.querySelector('.remove-user').addEventListener('click', () => {
        removeWorker(worker.id);
    });

    domCache.selectedWorkers.appendChild(chip);
    domCache.workerSearch.value = '';
    domCache.searchResults.classList.add('d-none');

    if (shouldShowCalendar()) {
        updateCalendarEvents();
    }
    updateActiveFilters();
}

/**
 * Handles changes to the project filter
 */
function handleProjectFilterChange() {
    const selectedOptions = Array.from(this.selectedOptions);
    if (selectedOptions.length > 0) {
        selectedWorkers.forEach(workerId => {
            const chip = document.querySelector(`.user-chip[data-worker-id="${workerId}"]`);
            if (chip) chip.remove();
        });
        selectedWorkers.clear();

        selectedProjects = new Set(selectedOptions.map(option => option.value));
    } else {
        selectedProjects.clear();
    }

    if (shouldShowCalendar()) {
        updateCalendarEvents();
    } else if (calendar) {
        calendar.destroy();
        calendar = null;
    }
    updateActiveFilters();
}

/**
 * Updates the active filters display
 */
function updateActiveFilters() {
    const fragment = document.createDocumentFragment();
    domCache.activeFilters.innerHTML = '';

    selectedWorkers.forEach(workerId => {
        const workerChip = document.querySelector(`.user-chip[data-worker-id="${workerId}"]`);
        if (workerChip) {
            const filterChip = document.createElement('div');
            filterChip.className = 'filter-chip';
            filterChip.style.backgroundColor = workerChip.style.backgroundColor;
            filterChip.innerHTML = `
                Worker: ${workerChip.textContent.replace('×', '')}
                <span class="remove-filter" onclick="removeWorker('${workerId}')">×</span>
            `;
            fragment.appendChild(filterChip);
        }
    });

    Array.from(domCache.projectFilter.selectedOptions).forEach(option => {
        const filterChip = document.createElement('div');
        filterChip.className = 'filter-chip';
        filterChip.style.backgroundColor = '#4299e1';
        filterChip.innerHTML = `
            Project: ${option.text}
            <span class="remove-filter" onclick="removeProject('${option.value}')">×</span>
        `;
        fragment.appendChild(filterChip);
    });

    domCache.activeFilters.appendChild(fragment);

    if (selectedWorkers.size === 0 && domCache.projectFilter.selectedOptions.length === 0) {
        domCache.activeFiltersText.textContent = 'No active filters';
    } else {
        domCache.activeFiltersText.textContent = 'Active Filters:';
    }
}

// ========== CALENDAR MANAGEMENT ==========
/**
 * Determines if the calendar should be displayed based on active filters
 * @returns {boolean} True if calendar should be shown
 */
function shouldShowCalendar() {
    return selectedWorkers.size > 0 || selectedProjects.size > 0;
}

/**
 * Updates the calendar events based on current filters
 */
function updateCalendarEvents() {
    if (!calendar) {
        initializeCalendar();
    } else {
        calendar.refetchEvents();
    }
    domCache.exportButtons.style.display = shouldShowCalendar() ? 'flex' : 'none';
}

/**
 * Initializes the calendar with configuration
 */
function initializeCalendar() {
    calendar = new FullCalendar.Calendar(domCache.mergedCalendar, {
        initialView: "dayGridMonth",
        editable: false,
        selectable: false,
        headerToolbar: {
            left: "prev,next today",
            center: "title",
            right: ""
        },
        events: fetchCalendarEvents,
        eventDidMount: renderCalendarEvent,
        lazyFetching: true,
        eventLimit: true,
        eventLimitClick: 'popover'
    });

    calendar.render();

    if (shouldShowCalendar()) {
        domCache.exportButtons.classList.remove('d-none');
        domCache.exportButtons.style.display = 'flex';
    } else {
        hideExportButtons();
    }
}

/**
 * Fetches calendar events based on current filters
 * @param {Object} fetchInfo - FullCalendar fetch info
 * @param {Function} successCallback - Success callback
 * @param {Function} failureCallback - Failure callback
 */
async function fetchCalendarEvents(fetchInfo, successCallback, failureCallback) {
    try {
        const params = new URLSearchParams();
        
        if (selectedWorkers.size > 0) {
            params.append('workers', Array.from(selectedWorkers).join(','));
        }
        if (selectedProjects.size > 0) {
            params.append('projects', Array.from(selectedProjects).join(','));
        }
        
        params.append('start', fetchInfo.startStr);
        params.append('end', fetchInfo.endStr);

        const response = await fetch(`/api/schedule/filtered?${params}`);
        
        if (!response.ok) {
            throw new Error(`Server returned ${response.status}: ${await response.text()}`);
        }
        
        const schedules = await response.json();
        successCallback(schedules);
    } catch (error) {
        failureCallback(error);
    }
}

/**
 * Renders a calendar event with styling
 * @param {Object} info - FullCalendar event info
 */
function renderCalendarEvent(info) {
    if (info.el.querySelector(".event-content")) return;
    
    const eventTitle = info.event.title;
    const parts = eventTitle.split("\n");
    const timeText = parts[0] || "";
    const commentText = parts.length > 1 ? parts[1] : "";
    const workerName = info.event.extendedProps.workerName;
    const workerId = info.event.extendedProps.workerId;

    const workerColor = getWorkerColor(workerId);
    info.el.style.backgroundColor = workerColor;
    info.el.style.borderColor = colors[workerColor];

    info.el.innerHTML = `
        <div class="event-content">
            <div style="color: white; font-weight: bold;">${workerName}</div>
            <div style="color: white;"><strong>${timeText}</strong></div>
            ${commentText ? `<div style="font-size: 0.8rem; color: white; opacity: 0.9">${commentText}</div>` : ""}
        </div>
    `;

    info.el.style.transition = 'transform 0.2s ease';
    info.el.addEventListener('mouseenter', function() {
        this.style.transform = 'scale(1.02)';
        this.style.zIndex = '1';
    });
    info.el.addEventListener('mouseleave', function() {
        this.style.transform = 'scale(1)';
        this.style.zIndex = '';
    });
}

/**
 * Hides the export buttons
 */
function hideExportButtons() {
    domCache.exportButtons.classList.add('d-none');
    domCache.exportButtons.style.display = 'none !important';
}

// ========== DATA LOADING ==========
/**
 * Loads active projects from the API
 */
function loadActiveProjects() {
    fetch('/api/Projects/active')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(projects => {
            domCache.projectFilter.innerHTML = '';

            if (!projects || projects.length === 0) {
                const option = new Option('No active projects', '');
                option.disabled = true;
                domCache.projectFilter.add(option);
            } else {
                const fragment = document.createDocumentFragment();
                projects.forEach(project => {
                    const option = new Option(project.name, project.id.toString());
                    fragment.appendChild(option);
                });
                domCache.projectFilter.appendChild(fragment);
            }
        })
        .catch(error => {
            domCache.projectFilter.innerHTML = '<option disabled>Error loading projects</option>';
            
            showErrorMessage('Failed to load projects. Please try refreshing the page.');
        });
}

/**
 * Shows a user-friendly error message
 * @param {string} message - The error message to display
 */
function showErrorMessage(message) {
    showToast(message, "error");
}

/**
 * Initializes the worker search functionality with debouncing
 */
function initializeWorkerSearch() {
    let debounceTimeout;
    const minSearchLength = 2;

    domCache.workerSearch.addEventListener('input', function() {
        clearTimeout(debounceTimeout);
        const query = this.value.trim();

        if (query.length < minSearchLength) {
            domCache.searchResults.classList.add('d-none');
            return;
        }

        debounceTimeout = setTimeout(() => {
            performWorkerSearch(query);
        }, 300);
    });
}

/**
 * Performs the worker search API call
 * @param {string} query - The search query
 */
function performWorkerSearch(query) {
    fetch(`/api/schedule/search-workers?query=${encodeURIComponent(query)}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(workers => {
            const fragment = document.createDocumentFragment();
            domCache.searchResults.innerHTML = '';
            domCache.searchResults.classList.remove('d-none');

            if (workers.length === 0) {
                const noResults = document.createElement('div');
                noResults.className = 'no-results';
                noResults.textContent = 'No workers found';
                fragment.appendChild(noResults);
            } else {
                workers.forEach(worker => {
                    if (!selectedWorkers.has(worker.id)) {
                        const div = document.createElement('div');
                        div.className = 'search-result-item';
                        div.textContent = `${worker.name} ${worker.lastName}`;
                        div.addEventListener('click', () => addWorker(worker));
                        fragment.appendChild(div);
                    }
                });
            }
            
            domCache.searchResults.appendChild(fragment);
        })
        .catch(error => {
            domCache.searchResults.innerHTML = '<div class="no-results text-danger">Error searching workers</div>';
            domCache.searchResults.classList.remove('d-none');
        });
}

/**
 * Placeholder for loading notifications
 */
function loadNotifications() {
    // This function can be implemented to load any pending notifications
    // For now, it's just a placeholder
}

// ========== EXPORT FUNCTIONALITY ==========
/**
 * Exports the current calendar view to PDF
 */
function exportToPDF() {
    const { jsPDF } = window.jspdf;
    if (!jsPDF) {
        showErrorMessage('PDF export library not loaded. Please refresh the page and try again.');
        return;
    }

    try {
        const doc = new jsPDF('landscape');
        const currentDate = calendar.getDate();
        const monthYear = currentDate.toLocaleString('default', { month: 'long', year: 'numeric' });

        doc.setFontSize(18);
        doc.setTextColor(44, 62, 80);
        doc.text(`Schedule - ${monthYear}`, 15, 15);

        doc.setFontSize(10);
        doc.setTextColor(108, 117, 125);
        
        const filterInfo = getFilterInfo();
        doc.text(`Filters: ${filterInfo || 'None'}`, 15, 25);

        let yPosition = 35;

        const events = getEventsForCurrentMonth(currentDate);
        const workerEvents = groupEventsByWorker(events);

        Object.entries(workerEvents).forEach(([workerName, workerEventList], index) => {
            if (index > 0) {
                doc.addPage();
                yPosition = 15;
            }

            doc.setFillColor(236, 240, 241);
            doc.rect(15, yPosition, doc.internal.pageSize.width - 30, 10, 'F');
            doc.setFontSize(14);
            doc.setTextColor(44, 62, 80);
            doc.text(`Employee: ${workerName}`, 20, yPosition + 7);
            yPosition += 15;

            const tableData = prepareTableData(workerEventList);

            doc.autoTable({
                head: [['Date', 'Activity', 'Comment']],
                body: tableData,
                startY: yPosition,
                styles: {
                    fontSize: 10,
                    cellPadding: 3
                },
                headStyles: {
                    fillColor: [66, 153, 225],
                    textColor: 255,
                    fontStyle: 'bold'
                },
                alternateRowStyles: {
                    fillColor: [245, 247, 250]
                },
                margin: { top: 15 }
            });

            yPosition = doc.lastAutoTable.finalY + 20;
        });

        doc.save(`${getExportFileName(monthYear)}.pdf`);
    } catch (error) {
        showErrorMessage('Failed to export PDF. Please try again.');
    }
}

/**
 * Exports the current calendar view to Excel
 */
function exportToExcel() {
    if (!window.XLSX) {
        showErrorMessage('Excel export library not loaded. Please refresh the page and try again.');
        return;
    }

    try {
        const currentDate = calendar.getDate();
        const monthYear = currentDate.toLocaleString('default', { month: 'long', year: 'numeric' });

        const events = getEventsForCurrentMonth(currentDate);
        const workerEvents = groupEventsByWorker(events);

        const wb = XLSX.utils.book_new();

        const summaryData = createSummaryData(monthYear);
        const summaryWs = XLSX.utils.aoa_to_sheet(summaryData);
        XLSX.utils.book_append_sheet(wb, summaryWs, 'Summary');

        Object.entries(workerEvents).forEach(([workerName, workerEventList]) => {
            const excelData = workerEventList
                .sort((a, b) => new Date(a.start) - new Date(b.start))
                .map(event => {
                    const date = event.start.toLocaleDateString();
                    const title = event.title.split('\n')[0];
                    const comment = event.title.split('\n')[1] || '';
                    const type = event.title.includes('Vacation') ? 'Vacation' : 'Shift';

                    return {
                        'Date': date,
                        'Activity': type === 'Vacation' ? 'Vacation' : title,
                        'Comment': comment
                    };
                });

            const safeSheetName = workerName.substring(0, 31);
            const ws = XLSX.utils.json_to_sheet(excelData);
            XLSX.utils.book_append_sheet(wb, ws, safeSheetName);
        });

        XLSX.writeFile(wb, `${getExportFileName(monthYear)}.xlsx`);
    } catch (error) {
        showErrorMessage('Failed to export Excel. Please try again.');
    }
}

/**
 * Creates summary data for Excel export
 * @param {string} monthYear - The month and year
 * @returns {Array} Summary data
 */
function createSummaryData(monthYear) {
    return [
        ['Schedule Summary'],
        [`Month: ${monthYear}`],
        [''],
        ['Filtered Workers:'],
        ...Array.from(document.querySelectorAll('.user-chip'))
            .map(chip => [chip.textContent.replace('×', '').trim()]),
        [''],
        ['Filtered Projects:'],
        ...Array.from(domCache.projectFilter.selectedOptions)
            .map(option => [option.text])
    ];
}

/**
 * Gets filter information for exports
 * @returns {string} Filter information text
 */
function getFilterInfo() {
    const selectedWorkerNames = Array.from(document.querySelectorAll('.user-chip'))
        .map(chip => chip.textContent.replace('×', '').trim())
        .join(', ');
    
    const selectedProjectNames = Array.from(domCache.projectFilter.selectedOptions)
        .map(option => option.text)
        .join(', ');

    let filterText = '';
    if (selectedWorkerNames) filterText += `Workers: ${selectedWorkerNames}`;
    if (selectedProjectNames) {
        if (filterText) filterText += ' | ';
        filterText += `Projects: ${selectedProjectNames}`;
    }
    
    return filterText;
}

/**
 * Prepares table data for PDF export
 * @param {Array} workerEventList - List of events for a worker
 * @returns {Array} Formatted table data
 */
function prepareTableData(workerEventList) {
    return workerEventList
        .sort((a, b) => new Date(a.start) - new Date(b.start))
        .map(event => {
            const date = event.start.toLocaleDateString();
            const title = event.title.split('\n')[0];
            const comment = event.title.split('\n')[1] || '';
            const type = event.title.includes('Vacation') ? 'Vacation' : 'Shift';
            const activity = type === 'Vacation' ? 'Vacation' : title;
            return [date, activity, comment];
        });
}

// ========== UTILITY FUNCTIONS ==========
/**
 * Gets a color for a worker based on their ID
 * @param {string} workerId - The worker ID
 * @returns {string} The color code
 */
function getWorkerColor(workerId) {
    const colorKeys = Object.keys(colors);
    const colorIndex = Math.abs(hashString(workerId)) % colorKeys.length;
    return colorKeys[colorIndex];
}

/**
 * Creates a hash from a string
 * @param {string} str - The string to hash
 * @returns {number} The hash value
 */
function hashString(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        const char = str.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash;
    }
    return hash;
}

/**
 * Gets events for the current month
 * @param {Date} currentDate - The current date
 * @returns {Array} Filtered events
 */
function getEventsForCurrentMonth(currentDate) {
    return calendar.getEvents()
        .filter(event => {
            const eventDate = new Date(event.start);
            return eventDate.getMonth() === currentDate.getMonth() &&
                eventDate.getFullYear() === currentDate.getFullYear();
        });
}

/**
 * Groups events by worker
 * @param {Array} events - The events to group
 * @returns {Object} Events grouped by worker
 */
function groupEventsByWorker(events) {
    return events.reduce((groups, event) => {
        const workerName = event.extendedProps.workerName;
        if (!groups[workerName]) {
            groups[workerName] = [];
        }
        groups[workerName].push(event);
        return groups;
    }, {});
}

/**
 * Generates a filename for exports based on current filters
 * @param {string} monthYear - The month and year
 * @returns {string} The generated filename
 */
function getExportFileName(monthYear) {
    const selectedWorkerNames = Array.from(document.querySelectorAll('.user-chip'))
        .map(chip => chip.textContent.replace('×', '').trim());

    const selectedProjectNames = Array.from(domCache.projectFilter.selectedOptions)
        .map(option => option.text);

    let fileName = `schedule-${monthYear.toLowerCase().replace(' ', '-')}`;

    if (selectedWorkerNames.length > 0) {
        fileName += `-${selectedWorkerNames.join('-')}`;
    }

    if (selectedProjectNames.length > 0) {
        fileName += `-${selectedProjectNames.join('-')}`;
    }

    return fileName.replace(/[^a-z0-9-]/gi, '-').toLowerCase();
}

window.removeWorker = removeWorker;
window.removeProject = removeProject;
window.clearAllFilters = clearAllFilters;
window.updateCalendarEvents = updateCalendarEvents;
window.shouldShowCalendar = shouldShowCalendar;
window.updateActiveFilters = updateActiveFilters;