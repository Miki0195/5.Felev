document.addEventListener("DOMContentLoaded", function () {
    // ========== VARIABLE DECLARATIONS ==========
    loadNotifications();
    const workerSearch = document.getElementById("workerSearch");
    const searchResults = document.getElementById("searchResults");
    const scheduleContainer = document.getElementById("scheduleContainer");
    const workerName = document.getElementById("workerName");
    let selectedDate = null;
    let selectedWorkerId = null;
    let existingShiftId = null;
    let selectedShiftId = null;
    let selectedVacationId = null;
    let calendar = null;
    let debounceTimeout;

    // ========== WORKER SEARCH FUNCTIONALITY ==========
    workerSearch.addEventListener("input", function () {
        clearTimeout(debounceTimeout);
        const query = this.value.trim();

        if (query.length < 2) {
            searchResults.classList.add("d-none");
            return;
        }

        debounceTimeout = setTimeout(() => {
            fetch(`/api/schedule/search-workers?query=${encodeURIComponent(query)}`)
                .then(response => response.json())
                .then(workers => {
                    searchResults.innerHTML = "";
                    searchResults.classList.remove("d-none");

                    if (workers.length === 0) {
                        searchResults.innerHTML = `
                                <div class="no-results">
                                    No workers found
                                </div>`;
                        return;
                    }

                    workers.forEach(worker => {
                        const div = document.createElement("div");
                        div.className = "search-result-item";
                        div.textContent = `${worker.name} ${worker.lastName}`;
                        div.addEventListener("click", () => {
                            selectedWorkerId = worker.id;
                            workerName.textContent = `${worker.name} ${worker.lastName}`;
                            workerSearch.value = `${worker.name} ${worker.lastName}`;
                            searchResults.classList.add("d-none");
                            scheduleContainer.classList.remove("d-none");
                            loadSchedule(selectedWorkerId);
                        });
                        searchResults.appendChild(div);
                    });
                })
                .catch(error => console.error("Error searching workers:", error));
        }, 300); // Debounce delay
    });

    document.addEventListener("click", function (e) {
        if (!workerSearch.contains(e.target) && !searchResults.contains(e.target)) {
            searchResults.classList.add("d-none");
        }
    });

    // ========== CALENDAR FUNCTIONALITY ==========
    function loadSchedule(workerId) {
        var calendarEl = document.getElementById("calendar");

        if (calendar) {
            calendar.destroy();
        }

        calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: "dayGridMonth",
            editable: true,
            selectable: true,
            eventStartEditable: true,
            droppable: true,
            headerToolbar: {
                left: "prev,next today",
                center: "title",
                right: ""
            },
            events: async function (fetchInfo, successCallback, failureCallback) {
                try {
                    let scheduleResponse = await fetch(`/api/schedule/${workerId}`);
                    let leaveResponse = await fetch(`/api/leave/${workerId}`);

                    let scheduleEvents = await scheduleResponse.json();
                    let leaveEvents = await leaveResponse.json();

                    if (!Array.isArray(scheduleEvents) || !Array.isArray(leaveEvents)) {
                        throw new Error("Invalid API response format");
                    }

                    successCallback([...scheduleEvents, ...leaveEvents]);
                } catch (error) {
                    failureCallback(error);
                }
            },
            eventDrop: function (info) {
                let eventId = info.event.id;
                let newDate = info.event.startStr;
                let today = new Date().toISOString().split("T")[0];
                let isVacation = info.event.title.includes("Vacation");
                let isPendingVacation = info.event.title.includes("Pending");
                let isApprovedVacation = info.event.title.includes("Approved");

                if (newDate < today) {
                    showToast("Cannot move events to a past date!", "error");
                    info.revert();
                    calendar.refetchEvents();
                    return;
                }

                if (isVacation && isPendingVacation) {
                    showToast("Pending vacations cannot be moved!", "error");
                    info.revert();
                    calendar.refetchEvents();
                    return;
                }

                Promise.all([
                    fetch(`/api/schedule/${selectedWorkerId}`).then(response => response.json()),
                    fetch(`/api/leave/${selectedWorkerId}`).then(response => response.json())
                ])
                    .then(([scheduleEvents, leaveEvents]) => {
                        let shiftExists = scheduleEvents.find(event => event.start === newDate);
                        let vacationExists = leaveEvents.find(event => event.start === newDate && (event.color === "orange" || event.color === "green"));

                        if (isVacation && vacationExists) {
                            showToast("Cannot move vacation to a day that already has a vacation!", "error");
                            info.revert();
                            calendar.refetchEvents();
                            return;
                        }

                        if (!isVacation && (shiftExists || vacationExists)) {
                            showToast("A shift or vacation already exists on this date!", "error");
                            info.revert();
                            calendar.refetchEvents();
                            return;
                        }

                        let deleteShiftPromise = shiftExists
                            ? fetch(`/api/schedule/delete/${shiftExists.id}`, { method: "DELETE" }).then(res => res.json())
                            : Promise.resolve({ success: true });

                        deleteShiftPromise.then(shiftDeleteResult => {
                            if (!shiftDeleteResult.success) {
                                showToast("Failed to remove conflicting shift.", "error");
                                info.revert();
                                calendar.refetchEvents();
                                return;
                            }

                            if (isVacation) {
                                fetch(`/api/leave/${eventId}/move`, {
                                    method: "PUT",
                                    headers: { "Content-Type": "application/json" },
                                    body: JSON.stringify(newDate)
                                })
                                    .then(response => {
                                        if (!response.ok) {
                                            throw new Error("Vacation move failed");
                                        }
                                        return response.json();
                                    })
                                    .then((data) => {
                                        if (data.success) {
                                            showToast("Vacation moved successfully!", "success");
                                            calendar.refetchEvents();
                                        } else {
                                            showToast("Failed to move vacation.", "error");
                                            info.revert();
                                            calendar.refetchEvents();
                                        }
                                    })
                                    .catch(error => {
                                        showToast("Failed to move vacation.", "error");
                                        info.revert();
                                        calendar.refetchEvents();
                                    });
                                return;
                            }

                            let shiftData = extractShiftDetails(info);

                            fetch(`/api/schedule/update/${eventId}`, {
                                method: "PUT",
                                headers: { "Content-Type": "application/json" },
                                body: JSON.stringify(shiftData)
                            })
                                .then(response => {
                                    if (!response.ok) {
                                        throw new Error("Shift move failed");
                                    }
                                    return response.json();
                                })
                                .then((data) => {
                                    if (data.success) {
                                        showToast("Shift moved successfully!", "success");
                                        calendar.refetchEvents();
                                    } else {
                                        showToast("Failed to move shift.", "error");
                                        info.revert();
                                        calendar.refetchEvents();
                                    }
                                })
                                .catch(error => {
                                    showToast("Failed to move shift.", "error");
                                    info.revert();
                                    calendar.refetchEvents();
                                });

                        });

                    })
                    .catch(error => {
                        showToast("Failed to verify event move.", "error");
                        info.revert();
                        calendar.refetchEvents();
                    });
            },

            eventDidMount: function (info) {
                let eventTitle = info.event.title;
                let parts = eventTitle.split("\n");

                let timeText = parts[0] || "";
                let commentText = parts.length > 1 ? parts[1] : "";

                if (info.el.querySelector(".event-content")) return;

                let contentHtml = "";

                const eventDate = info.event.startStr;
                const today = new Date().toISOString().split("T")[0];
                const isPastEvent = eventDate < today && !eventTitle.includes("Vacation");

                if (isPastEvent) {
                    info.el.style.opacity = "0.7";
                    info.el.style.backgroundColor = "#6c757d";
                }

                if (eventTitle.includes("Vacation")) {
                    contentHtml = `<div class="event-content" style="color: white; font-weight: bold;">Vacation</div>`;
                } else {
                    contentHtml = `<div class="event-content" style="color: white;"><strong>${timeText}</strong></div>
                                   <div class="event-content" style="font-size: 0.8rem; color: white; border-top: 1px solid rgba(255,255,255,0.2);">${commentText}</div>`;

                    if (isPastEvent) {
                        contentHtml += `<div class="event-content" style="font-size: 0.7rem; color: white; font-style: italic;">(Past)</div>`;
                    }
                }

                info.el.innerHTML = contentHtml;
            },
            dateClick: function (info) {
                let today = new Date().toISOString().split("T")[0];
                selectedDate = info.dateStr;

                Promise.all([
                    fetch(`/api/schedule/${selectedWorkerId}`).then(response => response.json()),
                    fetch(`/api/leave/${selectedWorkerId}`).then(response => response.json())
                ])
                    .then(([scheduleEvents, leaveEvents]) => {
                        let shiftExists = scheduleEvents.find(event => event.start === selectedDate);
                        let vacationExists = leaveEvents.find(event => event.start === selectedDate);

                        if (vacationExists) {
                            const status = vacationExists.title.includes("(Pending)") ? "pending" : "approved";
                            showCannotAddShiftModal(status);
                            return;
                        }

                        if (info.dateStr < today) {
                            if (shiftExists) {
                                existingShiftId = shiftExists.id;
                                showDeleteShiftModal();
                            } else {
                                showCannotAddPastShiftModal();
                            }
                            return;
                        }

                        if (shiftExists) {
                            existingShiftId = shiftExists.id;
                            showDeleteShiftModal();
                        } else {
                            showTimePickerModal();
                        }
                    })
                    .catch(error => console.error("Error:", error));
            },
            eventClick: function (info) {
                let eventTitle = info.event.title;

                selectedShiftId = info.event.id;

                let shiftParts = eventTitle.split("\n");

                if (eventTitle.includes("Vacation") && eventTitle.includes("Approved")) {
                    selectedVacationId = info.event.id;
                    document.getElementById("vacationDateText").textContent = info.event.startStr;

                    updateVacationStatus();
                }

                if (!eventTitle.includes("Vacation")) {
                    const shiftDate = info.event.startStr;
                    const today = new Date().toISOString().split("T")[0];

                    if (shiftDate < today) {
                        showCannotEditPastShiftModal();
                        return;
                    }

                    let shiftTime = shiftParts[0].split(" - ");
                    document.getElementById("editShiftStartTime").value = shiftTime[0];
                    document.getElementById("editShiftEndTime").value = shiftTime[1];
                    document.getElementById("editShiftComment").value = shiftParts[1] || "";

                    showEditShiftModal();
                } else {
                    if (eventTitle.includes("Pending")) {
                        document.getElementById("vacationDate").textContent = info.event.startStr;
                        selectedVacationId = info.event.id;

                        approveRejectVacationRequest();
                    }
                }
            }
        });

        calendar.render();
    }

    // ========== HELPER FUNCTIONS ==========
    function extractShiftDetails(info) {
        let shiftParts = info.event.title.split("\n");
        let shiftTime = shiftParts[0].split(" - ");
        let comment = shiftParts.length > 1 ? shiftParts[1] : "";

        if (!shiftTime[0] || !shiftTime[1]) {
            showToast("Invalid shift time format.", "error");
            info.revert();
            return null;
        }

        return {
            shiftDate: info.event.startStr,
            startTime: shiftTime[0],
            endTime: shiftTime[1],
            comment: comment
        };
    }

    function checkExistingShift(workerId, date) {
        fetch(`/api/schedule/${workerId}`)
            .then(response => response.json())
            .then(events => {
                let shiftExists = events.find(event => event.start === date);
                if (shiftExists) {
                    existingShiftId = shiftExists.id;
                    showDeleteShiftModal();
                } else {
                    showTimePickerModal();
                }
            })
            .catch(error => console.error("Error:", error));
    }

    // ========== MODAL MANAGEMENT FUNCTIONS ==========
    function showTimePickerModal() {
        let modal = new bootstrap.Modal(document.getElementById("timePickerModal"));
        modal.show();
    }

    function showEditShiftModal() {
        let modal = new bootstrap.Modal(document.getElementById("editShiftModal"));
        modal.show();
    }

    function showDeleteShiftModal() {
        let modal = new bootstrap.Modal(document.getElementById("deleteShiftModal"));
        modal.show();
    }

    function showCannotAddShiftModal(vacationStatus = 'approved') {
        const modalBody = document.querySelector('#cannotAddShiftModal .modal-body p');
        modalBody.innerHTML = `You cannot add a shift on this date because the user has a <strong>${vacationStatus} vacation</strong> request.`;

        let modal = new bootstrap.Modal(document.getElementById("cannotAddShiftModal"));
        modal.show();
    }

    function approveRejectVacationRequest() {
        let modal = new bootstrap.Modal(document.getElementById("approveRejectVacationModal"));
        modal.show();
    }

    function updateVacationStatus() {
        let modal = new bootstrap.Modal(document.getElementById("updateVacationModal"));
        modal.show();
    }

    function showCannotAddPastShiftModal() {
        let modal = new bootstrap.Modal(document.getElementById("cannotAddPastShiftModal"));
        modal.show();
    }

    function showCannotEditPastShiftModal() {
        let modal = new bootstrap.Modal(document.getElementById("cannotEditPastShiftModal"));
        modal.show();
    }

    // ========== SHIFT OPERATIONS ==========
    document.getElementById("saveShift").addEventListener("click", function () {
        let startTime = document.getElementById("shiftStartTime").value;
        let endTime = document.getElementById("shiftEndTime").value;

        if (!startTime || !endTime) {
            alert("Please select both start and end times.");
            return;
        }

        saveShift(selectedWorkerId, selectedDate, startTime, endTime);
    });

    function saveShift(workerId, date, startTime, endTime) {
        let comment = document.getElementById("shiftComment").value;

        fetch("/api/schedule", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: workerId, shiftDate: date, startTime: startTime, endTime: endTime, comment: comment })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    calendar.refetchEvents();
                    var modal = bootstrap.Modal.getInstance(document.getElementById("timePickerModal"));
                    modal.hide();
                } else {
                    alert("Failed to add shift.");
                }
            });
    }

    document.getElementById("saveEditShift").addEventListener("click", function () {
        let newStartTime = document.getElementById("editShiftStartTime").value;
        let newEndTime = document.getElementById("editShiftEndTime").value;
        let newComment = document.getElementById("editShiftComment").value;

        if (!newStartTime || !newEndTime) {
            showToast("Please select both start and end times.", "warning");
            return;
        }

        if (!selectedShiftId) {
            showToast("Error: No shift selected for editing.", "error");
            return;
        }

        let shiftEvent = calendar.getEventById(selectedShiftId);
        if (!shiftEvent) {
            showToast("Error: Shift not found in calendar.", "error");
            return;
        }

        let shiftDate = shiftEvent.startStr;

        const today = new Date().toISOString().split("T")[0];
        if (shiftDate < today) {
            showToast("Cannot edit shifts from past dates!", "error");
            var modal = bootstrap.Modal.getInstance(document.getElementById("editShiftModal"));
            modal.hide();
            return;
        }

        updateShift(selectedShiftId, newStartTime, newEndTime, shiftDate, newComment);
    });

    function updateShift(shiftId, startTime, endTime, shiftDate, comment) {
        const payload = {
            startTime: startTime,
            endTime: endTime,
            shiftDate: shiftDate,
            comment: comment || ""
        };

        fetch(`/api/schedule/update/${shiftId}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(response => {
                if (!response.ok) {
                    return response.text().then(text => {
                        throw new Error(`Server returned ${response.status}: ${text}`);
                    });
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    calendar.refetchEvents();

                    var modal = bootstrap.Modal.getInstance(document.getElementById("editShiftModal"));
                    modal.hide();

                    showToast("Shift updated successfully!", "success");
                } else {
                    showToast("Failed to update shift: " + (data.message || "Unknown error"), "error");
                }
            })
            .catch(error => {
                showToast("Error updating shift: " + error.message, "error");
            });
    }

    document.getElementById("confirmDeleteShift").addEventListener("click", function () {
        deleteShift(existingShiftId);
    });

    function deleteShift(shiftId) {
        fetch(`/api/schedule/delete/${shiftId}`, {
            method: "DELETE"
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showToast("Shift deleted succefully.", "success");
                    calendar.refetchEvents();
                    var modal = bootstrap.Modal.getInstance(document.getElementById("deleteShiftModal"));
                    modal.hide();
                } else {
                    alert("Failed to delete shift.");
                }
            })
            .catch(error => console.error("Error:", error));
    }

    // ========== VACATION OPERATIONS ==========
    document.getElementById("deleteVacationBtn").addEventListener("click", function () {
        deleteVacation(selectedVacationId);
    });

    function deleteVacation(vacationId) {
        fetch(`/api/leave/delete/${vacationId}`, {
            method: "DELETE"
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    calendar.refetchEvents();
                    let event = calendar.getEventById(vacationId);
                    if (event) {
                        event.remove();
                    }

                    var modal = bootstrap.Modal.getInstance(document.getElementById("updateVacationModal"));
                    modal.hide();
                } else {
                    alert("Failed to delete vacation.");
                }
            })
            .catch(error => console.error("Error:", error));
    }

    document.getElementById("approveVacationBtn").addEventListener("click", function () {
        updateLeaveStatus(selectedVacationId, "Approved");
    });

    document.getElementById("rejectVacationBtn").addEventListener("click", function () {
        updateLeaveStatus(selectedVacationId, "Rejected");
    });

    function updateLeaveStatus(leaveId, status) {
        fetch(`/api/leave/${leaveId}/status`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(status)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showToast("Vacation status changed.", "success");
                    calendar.refetchEvents();

                    let modalElement = document.getElementById("approveRejectVacationModal");
                    let modalInstance = bootstrap.Modal.getInstance(modalElement);
                    if (modalInstance) {
                        modalInstance.hide();
                    }
                } else {
                    alert("Failed to update leave status.");
                }
            })
            .catch(error => console.error("Error:", error));
    }

    // ========== RECURRING SHIFTS FUNCTIONALITY ==========
    const today = new Date();
    const nextMonth = new Date(today);
    nextMonth.setMonth(today.getMonth() + 1);

    document.getElementById('rangeStart').valueAsDate = today;
    document.getElementById('rangeEnd').valueAsDate = nextMonth;

    document.getElementById('rangeStart').min = today.toISOString().split('T')[0];
    document.getElementById('rangeEnd').min = today.toISOString().split('T')[0];

    function updatePreview() {
        const startDate = new Date(document.getElementById('rangeStart').value);
        const endDate = new Date(document.getElementById('rangeEnd').value);
        const startTime = document.getElementById('recurringStartTime').value;
        const endTime = document.getElementById('recurringEndTime').value;
        const selectedDays = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
            .filter(day => document.getElementById(day).checked);

        const previewDates = document.getElementById('previewDates');
        previewDates.innerHTML = '';

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (startDate < today) {
            previewDates.innerHTML = '<div class="text-danger">Start date cannot be in the past</div>';
            return;
        }

        if (!startTime || !endTime || selectedDays.length === 0) {
            previewDates.innerHTML = '<div class="text-muted">Select time and days to see preview</div>';
            return;
        }

        checkAvailabilityForPreview(startDate, endDate, selectedDays, startTime, endTime);
    }

    async function checkAvailabilityForPreview(startDate, endDate, selectedDays, startTime, endTime) {
        const previewDates = document.getElementById('previewDates');
        previewDates.innerHTML = '<div class="text-muted">Checking availability...</div>';

        try {
            const [scheduleResponse, leaveResponse] = await Promise.all([
                fetch(`/api/schedule/${selectedWorkerId}`),
                fetch(`/api/leave/${selectedWorkerId}`)
            ]);

            const scheduleEvents = await scheduleResponse.json();
            const leaveEvents = await leaveResponse.json();

            const unavailableDates = new Map();

            scheduleEvents.forEach(event => {
                unavailableDates.set(event.start, 'shift');
            });

            leaveEvents.forEach(event => {
                const status = event.title.includes('Pending') ? 'pending vacation' : 'approved vacation';
                unavailableDates.set(event.start, status);
            });

            let currentDate = new Date(startDate);
            let shiftsCount = 0;
            let conflictsCount = 0;

            while (currentDate <= endDate && shiftsCount < 50) {
                const dayName = currentDate.toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
                if (selectedDays.includes(dayName)) {
                    const dateString = currentDate.toLocaleDateString('en-US', {
                        weekday: 'short',
                        month: 'short',
                        day: 'numeric'
                    });

                    const dateISOString = currentDate.toISOString().split('T')[0];
                    const conflictType = unavailableDates.get(dateISOString);

                    if (conflictType) {
                        previewDates.innerHTML += `
                                <div class="preview-date-item text-danger">
                                    ${dateString} - Cannot add shift (${conflictType} exists)
                                </div>`;
                        conflictsCount++;
                    } else {
                        previewDates.innerHTML += `
                                <div class="preview-date-item">
                                    ${dateString} (${startTime} - ${endTime})
                                </div>`;
                        shiftsCount++;
                    }
                }
                currentDate.setDate(currentDate.getDate() + 1);
            }

            if (shiftsCount === 0 && conflictsCount === 0) {
                previewDates.innerHTML = '<div class="text-muted">No shifts in selected range</div>';
            } else if (shiftsCount === 0 && conflictsCount > 0) {
                previewDates.innerHTML += `
                        <div class="text-danger mt-2">
                            All selected dates have conflicts. Please select different days or date range.
                        </div>`;
            } else if (conflictsCount > 0) {
                previewDates.innerHTML += `
                        <div class="text-warning mt-2">
                            Note: ${conflictsCount} date(s) with conflicts will be skipped.
                        </div>`;
            }
        } catch (error) {
            previewDates.innerHTML = '<div class="text-danger">Error checking availability. Please try again.</div>';
        }
    }

    document.getElementById('saveRecurringShifts').addEventListener('click', async function () {
        const startDate = new Date(document.getElementById('rangeStart').value);
        const endDate = new Date(document.getElementById('rangeEnd').value);
        const startTime = document.getElementById('recurringStartTime').value;
        const endTime = document.getElementById('recurringEndTime').value;
        const comment = document.getElementById('recurringComment').value;
        const selectedDays = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
            .filter(day => document.getElementById(day).checked);

        if (!startTime || !endTime || selectedDays.length === 0) {
            showToast('Please fill in all required fields', 'error');
            return;
        }

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (startDate < today) {
            showToast('Cannot add shifts for past dates', 'error');
            return;
        }

        if (endDate < startDate) {
            showToast('End date must be after start date', 'error');
            return;
        }

        try {
            const [scheduleResponse, leaveResponse] = await Promise.all([
                fetch(`/api/schedule/${selectedWorkerId}`),
                fetch(`/api/leave/${selectedWorkerId}`)
            ]);

            const scheduleEvents = await scheduleResponse.json();
            const leaveEvents = await leaveResponse.json();

            const unavailableDates = new Map();

            scheduleEvents.forEach(event => {
                unavailableDates.set(event.start, true);
            });

            leaveEvents.forEach(event => {
                unavailableDates.set(event.start, true);
            });

            const shifts = [];
            let currentDate = new Date(startDate);
            let skippedCount = 0;

            while (currentDate <= endDate) {
                const dayName = currentDate.toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
                const dateISOString = currentDate.toISOString().split('T')[0];

                if (selectedDays.includes(dayName) && !unavailableDates.has(dateISOString)) {
                    shifts.push({
                        userId: selectedWorkerId,
                        shiftDate: dateISOString,
                        startTime: startTime,
                        endTime: endTime,
                        comment: comment
                    });
                } else if (selectedDays.includes(dayName) && unavailableDates.has(dateISOString)) {
                    skippedCount++;
                }

                currentDate.setDate(currentDate.getDate() + 1);
            }

            if (shifts.length === 0) {
                if (skippedCount > 0) {
                    showToast('All selected dates have conflicts. No shifts were added.', 'warning');
                } else {
                    showToast('No shifts to add in the selected range', 'warning');
                }
                return;
            }

            const response = await fetch('/api/schedule/bulk', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(shifts)
            });

            const result = await response.json();
            if (result.success) {
                const modal = document.getElementById('recurringShiftModal');
                const bsModal = bootstrap.Modal.getInstance(modal);
                bsModal.hide();

                document.querySelectorAll('.modal-backdrop').forEach(backdrop => {
                    backdrop.remove();
                });

                // Reset body
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
                document.body.style.paddingRight = '';

                calendar.refetchEvents();

                if (skippedCount > 0) {
                    showToast(`Recurring shifts added successfully! (${skippedCount} conflicting dates were skipped)`, 'success');
                } else {
                    showToast('Recurring shifts added successfully!', 'success');
                }
            } else {
                showToast('Failed to add recurring shifts', 'error');
            }
        } catch (error) {
            showToast('An error occurred while adding shifts', 'error');
        }
    });

    ['rangeStart', 'rangeEnd', 'recurringStartTime', 'recurringEndTime'].forEach(id => {
        document.getElementById(id).addEventListener('change', function () {
            if (id === 'rangeStart' || id === 'rangeEnd') {
                const selectedDate = new Date(this.value);
                const today = new Date();
                today.setHours(0, 0, 0, 0);

                if (selectedDate < today) {
                    showToast('Cannot select dates in the past', 'error');
                    this.valueAsDate = today;
                }

                if (id === 'rangeEnd') {
                    const startDate = new Date(document.getElementById('rangeStart').value);
                    if (selectedDate < startDate) {
                        showToast('End date cannot be before start date', 'error');
                        const newEndDate = new Date(startDate);
                        newEndDate.setDate(startDate.getDate() + 1);
                        this.valueAsDate = newEndDate;
                    }
                }
            }

            updatePreview();
        });
    });

    ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'].forEach(day => {
        document.getElementById(day).addEventListener('change', function () {
            updatePreview();
        });
    });

    function resetRecurringShiftModal() {
        document.getElementById('recurringStartTime').value = '';
        document.getElementById('recurringEndTime').value = '';

        const today = new Date();
        const nextMonth = new Date(today);
        nextMonth.setMonth(today.getMonth() + 1);

        const todayStr = today.toISOString().split('T')[0];

        document.getElementById('rangeStart').min = todayStr;
        document.getElementById('rangeEnd').min = todayStr;

        document.getElementById('rangeStart').valueAsDate = today;
        document.getElementById('rangeEnd').valueAsDate = nextMonth;

        ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'].forEach(day => {
            document.getElementById(day).checked = false;
        });

        document.getElementById('recurringComment').value = '';

        document.getElementById('previewDates').innerHTML = '<div class="text-muted">Select time and days to see preview</div>';
    }

    document.getElementById('recurringShiftModal').addEventListener('show.bs.modal', function () {
        resetRecurringShiftModal();
    });
});