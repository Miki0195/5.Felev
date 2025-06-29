let currentCharts = {
    projectProgress: null,
    taskDistribution: null
};

// Bootstrap toast instances container
let toastInstances = [];

// Track current task distribution view mode
let currentTaskView = 'status';

document.addEventListener('DOMContentLoaded', function() {
    try {
    initializeFilters();
        
        // Apply and clear filter buttons
        const applyFiltersBtn = document.getElementById("applyFilters");
        if (applyFiltersBtn) {
            applyFiltersBtn.addEventListener("click", loadReportData);
        }
        
        const clearFiltersBtn = document.getElementById("clearFilters");
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener("click", clearFilters);
        }
        
        // Date range filter
        const dateRangeFilter = document.getElementById("dateRangeFilter");
        if (dateRangeFilter) {
            dateRangeFilter.addEventListener("change", handleDateRangeChange);
        }
        
        // Task distribution view toggle
        const viewButtons = document.querySelectorAll('.btn-group [data-view]');
        viewButtons.forEach(button => {
            button.addEventListener('click', function() {
                viewButtons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');
                updateTaskDistributionView(this.dataset.view);
            });
        });

        // Initial data load
    loadReportData();
    } catch (error) {
        console.error("Error initializing page:", error);
        showToast("Error initializing page: " + error.message, "error");
    }
});

function handleDateRangeChange() {
    try {
        const dateRange = document.getElementById('dateRangeFilter').value;
        const datePickerContainers = document.querySelectorAll('.date-picker-container');
        
        if (dateRange === 'custom') {
            datePickerContainers.forEach(container => container.style.display = 'block');
        } else {
            datePickerContainers.forEach(container => container.style.display = 'none');
        }
    } catch (error) {
        console.error("Error handling date range change:", error);
    }
}

function clearFilters() {
    try {
        const dateRangeFilter = document.getElementById('dateRangeFilter');
        if (dateRangeFilter) dateRangeFilter.value = '30';
        
        const projectFilter = document.getElementById('projectFilter');
        if (projectFilter) projectFilter.value = '';
        
        const teamFilter = document.getElementById('teamFilter');
        if (teamFilter) teamFilter.value = 'none';
        
        if (window.jQuery && $.fn.select2) {
            $('#projectFilter, #teamFilter').trigger('change');
        }
        
        document.querySelectorAll('.date-picker-container').forEach(container => {
            container.style.display = 'none';
        });
        
        const customDateFrom = document.getElementById('customDateFrom');
        if (customDateFrom) customDateFrom.value = '';
        
        const customDateTo = document.getElementById('customDateTo');
        if (customDateTo) customDateTo.value = '';
        
        // Clear active filters display
        const activeFilters = document.getElementById('activeFilters');
        if (activeFilters) activeFilters.innerHTML = '';
        
        loadReportData();
    } catch (error) {
        console.error("Error clearing filters:", error);
        showToast("Error clearing filters: " + error.message, "error");
    }
}

async function initializeFilters() {
    try {
        // Initialize Select2 for filters if jQuery and Select2 are available
        if (window.jQuery && $.fn.select2) {
            $('#projectFilter, #teamFilter').select2({
                placeholder: 'Select option',
            allowClear: true
        });
        }

        // Load projects for filter
        const projectResponse = await fetch('/api/reports/projects');
        if (!projectResponse.ok) throw new Error(`Failed to fetch projects: ${projectResponse.status}`);
        const projects = await projectResponse.json();
        
        const projectFilter = document.getElementById('projectFilter');
        if (projectFilter && projects && Array.isArray(projects)) {
            projects.forEach(project => {
                const option = new Option(project.name, project.id);
                projectFilter.appendChild(option);
            });
        }

        // Load team members for filter
        try {
        const teamResponse = await fetch('/api/projectsapi/team-members');
            if (teamResponse.ok) {
        const members = await teamResponse.json();
        
        const teamFilter = document.getElementById('teamFilter');
                if (teamFilter && members && Array.isArray(members)) {
            members.forEach(member => {
                        const option = new Option(`${member.name} ${member.lastName || ''}`, member.id);
                teamFilter.appendChild(option);
            });
        }
            }
        } catch (error) {
            console.warn('Could not load team members:', error);
        }

    } catch (error) {
        console.error('Error initializing filters:', error);
            showToast('Error initializing filters: ' + error.message, 'error');
    }
}

async function loadReportData() {
    try {
        updateActiveFilters();
        
        // Show loading states for all sections
        showLoadingState('totalProjects');
        showLoadingState('activeTasks');
        showLoadingState('teamProductivity');
        showLoadingState('overdueTasks');
        showLoadingState('completedTasks');
        showLoadingState('pendingTasks');
        showLoadingState('totalTasks');
        
        const teamPerformanceTable = document.getElementById('teamPerformanceTable');
        if (teamPerformanceTable) {
            const tbody = teamPerformanceTable.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `;
            }
        }

        // Build query parameters
        let queryParams = new URLSearchParams();
        
        // Get date range
        const dateRangeFilter = document.getElementById('dateRangeFilter');
        const dateRange = dateRangeFilter ? dateRangeFilter.value : '30';
        
        // Only add date parameters if not "all time"
        if (dateRange !== 'all') {
            if (dateRange === 'custom') {
                const fromDate = document.getElementById('customDateFrom')?.value;
                const toDate = document.getElementById('customDateTo')?.value;
                
                if (fromDate) queryParams.append('startDate', new Date(fromDate).toISOString());
                if (toDate) queryParams.append('endDate', new Date(toDate).toISOString());
            } else {
                const days = parseInt(dateRange);
                const endDate = new Date();
                const startDate = new Date();
                startDate.setDate(startDate.getDate() - days);
                
                queryParams.append('startDate', startDate.toISOString());
                queryParams.append('endDate', endDate.toISOString());
            }
        }
        
        // For logging/debugging - show the date range being used
        let dateRangeInfo = "Date Range: ";
        if (dateRange === 'all') {
            dateRangeInfo += "All Time";
        } else if (dateRange === 'custom') {
            const fromDate = document.getElementById('customDateFrom')?.value;
            const toDate = document.getElementById('customDateTo')?.value;
            dateRangeInfo += `Custom (${fromDate || 'none'} to ${toDate || 'none'})`;
        } else {
            dateRangeInfo += `Last ${dateRange} days`;
        }
        console.log(dateRangeInfo);
        
        // Get project filter
        const projectFilter = document.getElementById('projectFilter');
        const isProjectSelected = projectFilter && projectFilter.value;
        if (isProjectSelected) {
            queryParams.append('projectId', projectFilter.value);
        }
        
        // Get team member filter
        const teamFilter = document.getElementById('teamFilter');
        if (teamFilter && teamFilter.value && teamFilter.value !== 'none') {
            queryParams.append('teamMemberId', teamFilter.value);
        }

        console.log(`Fetching data with params: ${queryParams.toString()}`);

        // Fetch all data in parallel
        const [projectMetrics, userProductivity, taskDistribution] = await Promise.all([
            fetch(`/api/reports/project-metrics?${queryParams}`).then(r => {
                if (!r.ok) throw new Error(`Failed to fetch project metrics: ${r.status}`);
                return r.json();
            }),
            fetch(`/api/reports/user-productivity?${queryParams}`).then(r => {
                if (!r.ok) throw new Error(`Failed to fetch user productivity: ${r.status}`);
                return r.json();
            }),
            fetch(`/api/reports/task-distribution?${queryParams}`).then(r => {
                if (!r.ok) throw new Error(`Failed to fetch task distribution: ${r.status}`);
                return r.json();
            })
        ]);

        // Ensure taskDistribution is an array
        if (!Array.isArray(taskDistribution)) {
            console.error("Task distribution is not an array!", taskDistribution);
            throw new Error("Task distribution data is invalid");
        }
        
        // Generate the completed tasks data after we have the project metrics
        const completedTasks = simulateCompletedTasksData(projectMetrics, dateRange);

        // Show/hide UI elements based on filters
        updateUIVisibility(isProjectSelected);
        
        // Update quick stats using the task distribution by status
        updateQuickStats(projectMetrics, taskDistribution, isProjectSelected, dateRange);
        
        // Update charts (will internally call updateTaskDistributionView)
        updateCharts(projectMetrics, completedTasks, isProjectSelected, dateRange);
        
        // Update team performance table
        updateTeamPerformance(userProductivity);
    } catch (error) {
        console.error('Error loading report data:', error);
        showToast('Error loading report data: ' + error.message, 'error');

        // Show error states for all cards
        const totalProjects = document.getElementById('totalProjects');
        if (totalProjects) totalProjects.textContent = '-';
        
        const completedTasks = document.getElementById('completedTasks');
        if (completedTasks) completedTasks.textContent = '-';
        
        const activeTasks = document.getElementById('activeTasks');
        if (activeTasks) activeTasks.textContent = '-';
        
        const teamProductivity = document.getElementById('teamProductivity');
        if (teamProductivity) teamProductivity.textContent = '-';
        
        const overdueTasks = document.getElementById('overdueTasks');
        if (overdueTasks) overdueTasks.textContent = '-';
        
        const pendingTasks = document.getElementById('pendingTasks');
        if (pendingTasks) pendingTasks.textContent = '-';
        
        const totalTasks = document.getElementById('totalTasks');
        if (totalTasks) totalTasks.textContent = '-';

        const teamPerformanceTable = document.getElementById('teamPerformanceTable');
        if (teamPerformanceTable) {
            const tbody = teamPerformanceTable.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-danger">
                            <i class="fas fa-exclamation-circle me-2"></i>
                    Error loading data. Please try again.
                </td>
            </tr>
        `;
    }
}
    }
}

function updateActiveFilters() {
    try {
        const activeFiltersContainer = document.getElementById('activeFilters');
        if (!activeFiltersContainer) return;
        
        activeFiltersContainer.innerHTML = '';
        
        // Date range filter
        const dateRangeFilter = document.getElementById('dateRangeFilter');
        if (dateRangeFilter) {
            const dateRange = dateRangeFilter.value;
            let dateFilterText = '';
            
            if (dateRange === 'all') {
                dateFilterText = 'All Time';
            } else if (dateRange === 'custom') {
                const fromDate = document.getElementById('customDateFrom')?.value;
                const toDate = document.getElementById('customDateTo')?.value;
                
                if (fromDate && toDate) {
                    dateFilterText = `${formatDate(fromDate)} to ${formatDate(toDate)}`;
                } else if (fromDate) {
                    dateFilterText = `From ${formatDate(fromDate)}`;
                } else if (toDate) {
                    dateFilterText = `Until ${formatDate(toDate)}`;
                }
            } else {
                dateFilterText = `Last ${dateRange} days`;
            }
            
            if (dateFilterText) {
                addFilterChip(activeFiltersContainer, 'Date', dateFilterText);
            }
        }
        
        // Project filter
        const projectFilter = document.getElementById('projectFilter');
        if (projectFilter && projectFilter.value) {
            const selectedIndex = projectFilter.selectedIndex;
            if (selectedIndex >= 0) {
                const projectName = projectFilter.options[selectedIndex].text;
                addFilterChip(activeFiltersContainer, 'Project', projectName);
            }
        }
        
        // Team member filter
        const teamFilter = document.getElementById('teamFilter');
        if (teamFilter && teamFilter.value && teamFilter.value !== 'none') {
            const selectedIndex = teamFilter.selectedIndex;
            if (selectedIndex >= 0) {
                const teamName = teamFilter.options[selectedIndex].text;
                addFilterChip(activeFiltersContainer, 'Team Member', teamName);
            }
        }
    } catch (error) {
        console.error("Error updating active filters:", error);
    }
}

function addFilterChip(container, label, value) {
    try {
        const chip = document.createElement('div');
        chip.className = 'filter-chip';
        chip.innerHTML = `
            <strong>${label}:</strong> ${value}
            <span class="remove-filter">×</span>
        `;
        
        const removeButton = chip.querySelector('.remove-filter');
        if (removeButton) {
            removeButton.addEventListener('click', function() {
                if (label === 'Date') {
                    const dateRangeFilter = document.getElementById('dateRangeFilter');
                    if (dateRangeFilter) dateRangeFilter.value = '30';
                    
                    const customDateFrom = document.getElementById('customDateFrom');
                    if (customDateFrom) customDateFrom.value = '';
                    
                    const customDateTo = document.getElementById('customDateTo');
                    if (customDateTo) customDateTo.value = '';
                    
                    document.querySelectorAll('.date-picker-container').forEach(container => {
                        container.style.display = 'none';
                    });
                } else if (label === 'Project') {
                    const projectFilter = document.getElementById('projectFilter');
                    if (projectFilter) projectFilter.value = '';
                    
                    if (window.jQuery && $.fn.select2) {
                        $('#projectFilter').trigger('change');
                    }
                } else if (label === 'Team Member') {
                    const teamFilter = document.getElementById('teamFilter');
                    if (teamFilter) teamFilter.value = 'none';
                    
                    if (window.jQuery && $.fn.select2) {
                        $('#teamFilter').trigger('change');
                    }
                }
                
                chip.remove();
                loadReportData();
            });
        }
        
        container.appendChild(chip);
    } catch (error) {
        console.error("Error adding filter chip:", error);
    }
}

function formatDate(dateString) {
    try {
        return new Date(dateString).toLocaleDateString();
    } catch (error) {
        console.error("Error formatting date:", error);
        return dateString;
    }
}

function showLoadingState(elementId) {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = `<div class="spinner-border spinner-border-sm text-primary" role="status"><span class="visually-hidden">Loading...</span></div>`;
        }
    } catch (error) {
        console.error(`Error showing loading state for ${elementId}:`, error);
    }
}

function updateUIVisibility(isProjectSelected) {
    try {
        // Project progress chart and task distribution layout
        const progressChartContainer = document.getElementById('progressChartContainer');
        const taskDistributionContainer = document.getElementById('taskDistributionContainer');
        
        if (progressChartContainer && taskDistributionContainer) {
            if (isProjectSelected) {
                // When project is selected: Show both charts side by side
                progressChartContainer.style.display = 'block';
                progressChartContainer.className = 'col-md-7';
                taskDistributionContainer.className = 'col-md-5';
            } else {
                // When no project is selected: Hide progress chart, make task distribution full width
                progressChartContainer.style.display = 'none';
                taskDistributionContainer.className = 'col-md-12';
            }
        }
        
        // First card: Toggle between Total Projects and Completed Tasks
        const totalProjectsElement = document.getElementById('totalProjects');
        const completedTasksElement = document.getElementById('completedTasks');
        const statsCardTitle = document.getElementById('statsCardTitle');
        const statsCardIcon = document.getElementById('statsCardIcon');
        
        if (totalProjectsElement && completedTasksElement && statsCardTitle && statsCardIcon) {
            // Show appropriate counter and update title
            totalProjectsElement.style.display = isProjectSelected ? 'none' : 'block';
            completedTasksElement.style.display = isProjectSelected ? 'block' : 'none';
            
            // Update title and icon for the card
            statsCardTitle.textContent = isProjectSelected ? 'Completed Tasks' : 'Total Projects';
            statsCardIcon.className = isProjectSelected ? 'bi bi-check2-circle' : 'bi bi-briefcase';
        }
        
        // Show/hide project-specific cards row
        const projectSpecificCardsRow = document.getElementById('projectSpecificCardsRow');
        if (projectSpecificCardsRow) {
            projectSpecificCardsRow.style.display = isProjectSelected ? 'flex' : 'none';
        }
        
    } catch (error) {
        console.error("Error updating UI visibility:", error);
    }
}

function updateQuickStats(projectMetrics, taskDistribution, isProjectSelected, dateRange) {
    try {
        // Get date range label for display
        let dateLabel = '';
        if (dateRange === 'all') {
            dateLabel = 'All Time';
        } else if (dateRange === 'custom') {
            dateLabel = 'Custom Range';
        } else {
            dateLabel = `Last ${dateRange} days`;
        }

        // Update Total Projects count (visible when no project is selected)
        const totalProjectsElement = document.getElementById('totalProjects');
        if (totalProjectsElement) {
            totalProjectsElement.textContent = projectMetrics.length;
        }
        
        // Get all task distribution data
        const completedTasksCount = taskDistribution.find(t => t.status === 'Completed')?.count || 0;
        const activeTasksCount = taskDistribution.find(t => t.status === 'In Progress')?.count || 0;
        const pendingTasksCount = taskDistribution.find(t => t.status === 'Pending' || t.status === 'Not Started')?.count || 0;
        const totalTasksCount = taskDistribution.reduce((sum, t) => sum + t.count, 0);
        
        // Update Completed Tasks count (visible when a project is selected)
        const completedTasksElement = document.getElementById('completedTasks');
        if (completedTasksElement) {
            completedTasksElement.textContent = completedTasksCount;
            
            // Add date label to card title if applicable
            const completedTasksTitle = document.getElementById('statsCardTitle');
            if (completedTasksTitle && isProjectSelected) {
                completedTasksTitle.innerHTML = `Completed Tasks <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
        
        // Update Active Tasks
        const activeTasksElement = document.getElementById('activeTasks');
        if (activeTasksElement) {
            activeTasksElement.textContent = activeTasksCount;
            
            // Update card title with date range
            const activeTasksTitle = activeTasksElement.closest('.stat-card').querySelector('.text-muted.mb-1');
            if (activeTasksTitle) {
                activeTasksTitle.innerHTML = `Active Tasks <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
        
        // Update Team Productivity
        const teamProductivityElement = document.getElementById('teamProductivity');
        if (teamProductivityElement) {
            const productivity = totalTasksCount ? Math.round((completedTasksCount / totalTasksCount) * 100) : 0;
            teamProductivityElement.textContent = `${productivity}%`;
            
            // Update card title with date range
            const productivityTitle = teamProductivityElement.closest('.stat-card').querySelector('.text-muted.mb-1');
            if (productivityTitle) {
                productivityTitle.innerHTML = `Team Productivity <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
        
        // Update Overdue Tasks
        const overdueTasksElement = document.getElementById('overdueTasks');
        if (overdueTasksElement) {
            const overdueTasks = projectMetrics.reduce((sum, p) => sum + (p.overdueTasks || 0), 0);
            overdueTasksElement.textContent = overdueTasks;
            
            // Update card title with date range
            const overdueTitle = overdueTasksElement.closest('.stat-card').querySelector('.text-muted.mb-1');
            if (overdueTitle) {
                overdueTitle.innerHTML = `Overdue Tasks <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
        
        // Update project-specific cards (only shown when a project is selected)
        const pendingTasksElement = document.getElementById('pendingTasks');
        if (pendingTasksElement) {
            pendingTasksElement.textContent = pendingTasksCount;
            
            // Update card title with date range
            const pendingTitle = pendingTasksElement.closest('.stat-card').querySelector('.text-muted.mb-1');
            if (pendingTitle) {
                pendingTitle.innerHTML = `Pending Tasks <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
        
        const totalTasksElement = document.getElementById('totalTasks');
        if (totalTasksElement) {
            totalTasksElement.textContent = totalTasksCount;
            
            // Update card title with date range
            const totalTasksTitle = totalTasksElement.closest('.stat-card').querySelector('.text-muted.mb-1');
            if (totalTasksTitle) {
                totalTasksTitle.innerHTML = `Total Tasks <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
        }
    } catch (error) {
        console.error("Error updating quick stats:", error);
        showToast("Error updating statistics", "error");
    }
}

function updateCharts(projectMetrics, completedTasks, isProjectSelected, dateRange) {
    try {
        // Get date range label for display
        let dateLabel = '';
        if (dateRange === 'all') {
            dateLabel = 'All Time';
        } else if (dateRange === 'custom') {
            dateLabel = 'Custom Range';
        } else {
            dateLabel = `Last ${dateRange} days`;
        }
        
        // Destroy existing charts to prevent inconsistencies
        if (currentCharts.projectProgress) {
            currentCharts.projectProgress.destroy();
            currentCharts.projectProgress = null;
        }

        // Project Progress Chart (only if a project is selected)
        const progressCanvas = document.getElementById('projectProgressChart');
        if (progressCanvas && isProjectSelected) {
            const progressCtx = progressCanvas.getContext('2d');
            if (!progressCtx) return;
            
            // Clear the canvas to ensure fresh rendering
            progressCtx.clearRect(0, 0, progressCanvas.width, progressCanvas.height);
            
            // Update chart title with date range
            const progressChartTitle = document.querySelector('#progressChartContainer h5');
            if (progressChartTitle) {
                progressChartTitle.innerHTML = `Project Progress <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
            
            // Create a gradient fill for the line chart
            const gradient = progressCtx.createLinearGradient(0, 0, 0, 400);
            gradient.addColorStop(0, "rgba(78, 115, 223, 0.8)");
            gradient.addColorStop(1, "rgba(78, 115, 223, 0.1)");
            
            // Prepare data for the progress over time chart
            let progressData = [];
            let dateLabels = [];
            
            // Check if we have valid data to display
            if (completedTasks && completedTasks.length > 0) {
                // Sort tasks by completion date
                const sortedTasks = [...completedTasks].sort((a, b) => 
                    new Date(a.completionDate).getTime() - new Date(b.completionDate).getTime()
                );
                
                // Format dates for x-axis labels
                dateLabels = sortedTasks.map(task => {
                    const date = new Date(task.completionDate);
                    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
                });
                
                // Get progress values for y-axis
                progressData = sortedTasks.map(task => task.progress);
            } else {
                // Fallback if no completed tasks data within the date range
                dateLabels = ["No task data available in this date range"];
                progressData = [0];
            }
            
            // Create the chart with clear defaults
    currentCharts.projectProgress = new Chart(progressCtx, {
                type: 'line',
        data: {
                    labels: dateLabels,
            datasets: [{
                        label: "Cumulative Progress",
                        data: progressData,
                        lineTension: 0.3,
                        backgroundColor: gradient,
                        borderColor: "rgba(78, 115, 223, 1)",
                        pointRadius: 5,
                        pointBackgroundColor: "rgba(78, 115, 223, 1)",
                        pointBorderColor: "rgba(78, 115, 223, 1)",
                        pointHoverRadius: 7,
                        pointHoverBackgroundColor: "rgba(78, 115, 223, 1)",
                        pointHoverBorderColor: "rgba(240, 245, 255, 1)",
                        pointHitRadius: 10,
                        pointBorderWidth: 2,
                        fill: true
            }]
        },
        options: {
            maintainAspectRatio: false,
                    layout: {
                        padding: {
                            left: 10,
                            right: 25,
                            top: 25,
                            bottom: 0
                        }
                    },
            scales: {
                        x: {
                            grid: {
                                display: false,
                                drawBorder: false
                            },
                            ticks: {
                                color: "#64748b",
                                maxRotation: 45,
                                minRotation: 30,
                                autoSkip: true,
                                maxTicksLimit: 10
                            },
                            title: {
                                display: true,
                                text: 'Task Completion Date',
                                color: "#64748b"
                            }
                        },
                        y: {
                            ticks: {
                                color: "#64748b",
                    beginAtZero: true,
                                max: 100,
                                callback: function(value) {
                                    return value + '%';
                                }
                            },
                            grid: {
                                color: "rgba(234, 236, 244, 0.2)",
                                drawBorder: false,
                                borderDash: [2],
                                borderDashOffset: [2]
                            },
                            title: {
                                display: true,
                                text: 'Cumulative Progress (%)',
                                color: "#64748b"
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            backgroundColor: "rgb(45, 55, 72)",
                            bodyFont: {
                                size: 14
                            },
                            titleFont: {
                                size: 14
                            },
                            titleMarginBottom: 10,
                            padding: 15,
                            displayColors: false,
                            callbacks: {
                                title: function(tooltipItems) {
                                    const index = tooltipItems[0].dataIndex;
                                    if (completedTasks && completedTasks[index]) {
                                        const task = completedTasks[index];
                                        return `Task #${task.taskId}`;
                                    }
                                    return 'Task';
                                },
                                label: function(context) {
                                    const index = context.dataIndex;
                                    if (completedTasks && completedTasks[index]) {
                                        const task = completedTasks[index];
                                        const date = new Date(task.completionDate);
                                        return [
                                            `Date: ${date.toLocaleDateString()}`,
                                            `Progress: ${task.progress.toFixed(1)}%`
                                        ];
                                    }
                                    return [`Progress: ${context.raw}%`];
                                }
                            }
                }
            }
        }
    });
        } else if (isProjectSelected) {
            console.warn("Project Progress chart canvas not found, but a project is selected");
        }

        // Update the task distribution chart
        updateTaskDistributionView(currentTaskView);
    } catch (error) {
        console.error("Error updating charts:", error);
        showToast("Error updating charts", "error");
    }
}

async function updateTaskDistributionView(view) {
    try {
        // This function would update the task distribution chart based on status or priority
        console.log(`Changing task distribution view to: ${view}`);
        
        // Get current filters
        const projectFilter = document.getElementById('projectFilter');
        const projectId = projectFilter?.value;
        
        // Save the current view for future reference
        currentTaskView = view;
        
        // If no chart canvas exists, return early
        const distributionCanvas = document.getElementById('taskDistributionChart');
        if (!distributionCanvas) return;
        
        // Determine which endpoint to use based on view type
        const endpoint = view === 'priority' 
            ? '/api/reports/task-distribution-by-priority'
            : '/api/reports/task-distribution';
            
        // Build query params
        let queryParams = new URLSearchParams();
        if (projectId) {
            queryParams.append('projectId', projectId);
        }
        
        try {
            // Show loading state
            const container = document.querySelector('.task-distribution-container');
            if (container) {
                container.innerHTML = `
                    <div class="d-flex justify-content-center align-items-center" style="height: 300px;">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                `;
            }
            
            // Fetch the distribution data
            const response = await fetch(`${endpoint}?${queryParams}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch ${view} distribution: ${response.status}`);
            }
            
            const data = await response.json();
            
            // Recreate canvas (to avoid any lingering issues with the previous chart)
            if (container) {
                container.innerHTML = '<canvas id="taskDistributionChart" height="300"></canvas>';
            }
            
            // Get the date range for labeling
            const dateRangeFilter = document.getElementById('dateRangeFilter');
            const dateRange = dateRangeFilter ? dateRangeFilter.value : '30';
            let dateLabel = '';
            if (dateRange === 'all') {
                dateLabel = 'All Time';
            } else if (dateRange === 'custom') {
                dateLabel = 'Custom Range';
            } else {
                dateLabel = `Last ${dateRange} days`;
            }
            
            // Update chart title with date range
            const distributionChartTitle = document.querySelector('#taskDistributionContainer h5');
            if (distributionChartTitle) {
                distributionChartTitle.innerHTML = `Task Distribution <small class="text-muted fs-6 fw-normal">(${dateLabel})</small>`;
            }
            
            // Get the new canvas context
            const newCanvas = document.getElementById('taskDistributionChart');
            if (!newCanvas) return;
            
            const ctx = newCanvas.getContext('2d');
            if (!ctx) return;
            
            // Destroy existing chart if it exists
            if (currentCharts.taskDistribution) {
                currentCharts.taskDistribution.destroy();
                currentCharts.taskDistribution = null;
            }
            
            // Determine colors and labels based on the view type
            let labels, values, backgroundColors;
            
            if (view === 'priority') {
                // Map priority to a named color to ensure consistency
                const getPriorityColor = (priority) => {
                    const priorityColors = {
                        'Low': '#20c997',      // Teal
                        'Medium': '#fd7e14',   // Orange
                        'High': '#dc3545',     // Red
                        'Urgent': '#6610f2'    // Purple
                    };
                    return priorityColors[priority] || '#64748b'; // Default gray for unknown priority
                };
                
                labels = data.map(item => item.priority);
                values = data.map(item => item.count);
                backgroundColors = data.map(item => getPriorityColor(item.priority));
            } else {
                // Map status to a named color to ensure consistency
                const getStatusColor = (status) => {
                    const statusColors = {
                        'Completed': '#10b981',   // Green
                        'In Progress': '#f59e0b', // Amber
                        'Pending': '#3b82f6',     // Blue
                        'Not Started': '#3b82f6', // Blue (same as Pending)
                        'Overdue': '#ef4444'      // Red
                    };
                    return statusColors[status] || '#64748b'; // Default gray for unknown status
                };
                
                labels = data.map(item => item.status);
                values = data.map(item => item.count);
                backgroundColors = data.map(item => getStatusColor(item.status));
            }
            
            // Create a new chart
            currentCharts.taskDistribution = new Chart(ctx, {
        type: 'doughnut',
        data: {
                    labels: labels,
            datasets: [{
                        data: values,
                        backgroundColor: backgroundColors,
                        borderWidth: 0,
                        hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
                    maintainAspectRatio: false,
                    cutout: '70%',
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                usePointStyle: true,
                                padding: 20,
                                font: {
                                    size: 12
                                }
                            }
                        },
                        title: {
                            display: true,
                            text: view === 'priority' ? 'Tasks by Priority' : 'Tasks by Status',
                            font: {
                                size: 16,
                                weight: 'bold'
                            },
                            padding: {
                                top: 10,
                                bottom: 10
                            }
                        }
                    },
                    animation: {
                        duration: 1000 // Consistent animation duration
                    }
                }
            });
        } catch (error) {
            console.error(`Error fetching ${view} distribution:`, error);
            showToast(`Failed to load task distribution by ${view}: ${error.message}`, 'error');
            
            // Revert to the previous view
            const buttons = document.querySelectorAll('.btn-group [data-view]');
            buttons.forEach(btn => {
                btn.classList.remove('active');
                if (btn.dataset.view === 'status') {
                    btn.classList.add('active');
                }
            });
        }
    } catch (error) {
        console.error("Error updating task distribution view:", error);
        showToast(`Error updating chart view: ${error.message}`, 'error');
    }
}

function updateTeamPerformance(userProductivity) {
    try {
        const teamPerformanceTable = document.getElementById('teamPerformanceTable');
        if (!teamPerformanceTable) return;
        
        const tbody = teamPerformanceTable.querySelector('tbody');
        if (!tbody) return;
    
    if (!userProductivity || userProductivity.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center">No data available</td></tr>';
        return;
    }

    tbody.innerHTML = userProductivity.map(user => {
        const productivityPercentage = user.tasksAssigned > 0 
            ? Math.round((user.tasksCompleted / user.tasksAssigned) * 100) 
            : 0;

        return `
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <img src="${user.profilePicturePath || '/images/default/default-profile.png'}" 
                             class="rounded-circle me-2" 
                             width="32" 
                             height="32"
                             alt="${user.name}">
                        <div>
                                <div class="fw-bold">${user.name} ${user.lastName || ''}</div>
                            <small class="text-muted">Tasks: ${user.tasksAssigned}</small>
                        </div>
                    </div>
                </td>
                <td>
                    <span class="fw-bold">${user.tasksCompleted}</span> / ${user.tasksAssigned}
                </td>
                <td>
                    <span class="fw-bold">${Math.round(user.totalWorkingHours)}</span> hrs
                </td>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="productivity-bar flex-grow-1 me-2">
                            <div class="productivity-bar-fill" 
                                 style="width: ${productivityPercentage}%">
                            </div>
                        </div>
                        <span>${productivityPercentage}%</span>
                    </div>
                </td>
                <td>
                    <div class="task-distribution">
                        <span class="task-distribution-pill task-pill-completed">
                            ${user.tasksCompleted} Done
                        </span>
                        <span class="task-distribution-pill task-pill-pending">
                            ${user.tasksAssigned - user.tasksCompleted} Pending
                        </span>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
    } catch (error) {
        console.error("Error updating team performance:", error);
        showToast("Error updating team performance data", "error");
    }
}

function exportTeamData(format) {
    try {
        const projectFilter = document.getElementById('projectFilter');
        const projectId = projectFilter?.value;
        const projectName = projectId && projectFilter.selectedIndex >= 0
            ? projectFilter.options[projectFilter.selectedIndex].text 
            : 'All Projects';
        
        const teamPerformanceTable = document.getElementById('teamPerformanceTable');
        if (!teamPerformanceTable) return;
        
        const rows = Array.from(teamPerformanceTable.querySelectorAll('tbody tr'));
        if (rows.length === 0) {
            showToast('No data to export', 'warning');
            return;
        }
    
    if (format === 'csv') {
            let csv = 'Team Member,Tasks Completed,Working Hours,Productivity Score\n';
        
        rows.forEach(row => {
            const cells = row.querySelectorAll('td');
                if (cells.length < 4) return;
                
                const nameEl = cells[0].querySelector('.fw-bold');
                const name = nameEl ? nameEl.textContent.trim() : 'Unknown';
                
            const tasksCompleted = cells[1].textContent.trim();
            const hours = cells[2].textContent.trim();
                
                const productivityEl = cells[3].querySelector('span:last-child');
                const productivity = productivityEl ? productivityEl.textContent.trim() : '0%';
            
                csv += `"${name}",${tasksCompleted},${hours},${productivity}\n`;
        });
        
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
            a.download = `team-performance-${projectName.replace(/\s+/g, '-')}-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
    } else if (format === 'pdf') {
            // Get the jsPDF instance
            const { jsPDF } = window.jspdf;
            if (!jsPDF) {
                showToast('PDF library not loaded. Please check your internet connection and try again.', 'error');
                return;
            }
            
            // Create a new PDF document
            const doc = new jsPDF({
                orientation: 'landscape',
                unit: 'mm',
                format: 'a4'
            });
            
            const dateRangeFilter = document.getElementById('dateRangeFilter');
            const dateRange = dateRangeFilter?.value || '30';
            let dateLabel = '';
            if (dateRange === 'all') {
                dateLabel = 'All Time';
            } else if (dateRange === 'custom') {
                const fromDate = document.getElementById('customDateFrom')?.value;
                const toDate = document.getElementById('customDateTo')?.value;
                if (fromDate && toDate) {
                    dateLabel = `${fromDate} to ${toDate}`;
                } else {
                    dateLabel = 'Custom Range';
                }
            } else {
                dateLabel = `Last ${dateRange} days`;
            }
            
            // Add title
            const title = `Team Performance Report - ${projectName}`;
            doc.setFontSize(16);
            doc.setTextColor(40, 40, 40);
            doc.text(title, 14, 22);
            
            // Add date range
            doc.setFontSize(11);
            doc.setTextColor(100, 100, 100);
            doc.text(`Date Range: ${dateLabel}`, 14, 30);
            doc.text(`Generated: ${new Date().toLocaleString()}`, 14, 36);

            // Create table data
            const tableColumn = ["Team Member", "Tasks Completed", "Working Hours", "Productivity Score"];
            const tableRows = [];
            
            rows.forEach(row => {
                const cells = row.querySelectorAll('td');
                if (cells.length < 4) return;
                
                const nameEl = cells[0].querySelector('.fw-bold');
                const name = nameEl ? nameEl.textContent.trim() : 'Unknown';
                
                const tasksCompleted = cells[1].textContent.trim();
                const hours = cells[2].textContent.trim();
                
                const productivityEl = cells[3].querySelector('span:last-child');
                const productivity = productivityEl ? productivityEl.textContent.trim() : '0%';
                
                tableRows.push([name, tasksCompleted, hours, productivity]);
            });
            
            // Add table to document
            doc.autoTable({
                head: [tableColumn],
                body: tableRows,
                startY: 45,
                theme: 'grid',
                headStyles: {
                    fillColor: [59, 130, 246],
                    textColor: 255,
                    fontStyle: 'bold'
                },
                alternateRowStyles: {
                    fillColor: [240, 247, 255]
                },
                margin: { top: 45 }
            });
            
            // Save PDF
            const filename = `team-performance-${projectName.replace(/\s+/g, '-')}-${new Date().toISOString().split('T')[0]}.pdf`;
            doc.save(filename);
            
            showToast('PDF exported successfully', 'success');
        }
    } catch (error) {
        console.error("Error exporting team data:", error);
        showToast("Error exporting data: " + error.message, "error");
    }
}

function showToast(message, type = 'info') {
    try {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;
        
        // Clean up any previously created toasts that have been dismissed
        toastInstances.forEach((toast, index) => {
            if (toast._element && !document.body.contains(toast._element)) {
                toastInstances.splice(index, 1);
            }
        });
        
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'error' ? 'bg-danger' : 
                      type === 'warning' ? 'bg-warning' :
                      type === 'success' ? 'bg-success' : 'bg-info';
                      
        const textClass = (type === 'warning') ? 'text-dark' : 'text-white';
        
        const toastHTML = `
            <div id="${toastId}" class="toast align-items-center ${textClass} ${bgClass}" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHTML);
        
        const toastElement = document.getElementById(toastId);
        if (!toastElement) return;
        
        const toast = new bootstrap.Toast(toastElement, { 
            autohide: true, 
            delay: 5000 
        });
        
        toast.show();
        toastInstances.push(toast);
        
        toastElement.addEventListener('hidden.bs.toast', function() {
            if (toastElement.parentNode) {
                toastElement.parentNode.removeChild(toastElement);
            }
        });
    } catch (error) {
        console.error("Error showing toast:", error);
    }
}

// Simulate completed tasks data since we don't have a real API for this
function simulateCompletedTasksData(projectMetrics, dateRange) {
    try {
        // For demonstration, we'll create a simulated dataset
        const now = new Date();
        const data = [];
        
        // Generate a seed based on project ID (if available) for deterministic results
        const projectId = document.getElementById('projectFilter')?.value;
        const seed = projectId ? parseInt(projectId) : Math.floor(Math.random() * 1000);
        
        // Simple deterministic pseudo-random number generator using the seed
        const pseudoRandom = (max) => {
            const x = Math.sin(seed + data.length) * 10000;
            return Math.floor((x - Math.floor(x)) * max);
        };
        
        // Setup date limits based on selected date range
        let startDate = null;
        let endDate = new Date(); // Default to today
        
        if (dateRange !== 'all') {
            if (dateRange === 'custom') {
                const fromDateStr = document.getElementById('customDateFrom')?.value;
                const toDateStr = document.getElementById('customDateTo')?.value;
                
                if (fromDateStr) {
                    startDate = new Date(fromDateStr);
                }
                
                if (toDateStr) {
                    endDate = new Date(toDateStr);
                    // Set to end of day
                    endDate.setHours(23, 59, 59, 999);
                }
            } else {
                // Parse days from date range
                const days = parseInt(dateRange);
                startDate = new Date();
                startDate.setDate(startDate.getDate() - days);
            }
        }
        
        console.log(`Date range for task completion simulation: ${startDate ? startDate.toISOString() : 'All time'} to ${endDate.toISOString()}`);
        
        // We'll take the total completed tasks and create events distributed over time
        const totalCompletedTasks = projectMetrics.reduce((sum, project) => sum + project.completedTasks, 0);
        let cumulativeProgress = 0;
        
        // Determine the range for the random dates
        // For "all time" use 365 days as a reasonable simulation range
        const maxDaysBack = dateRange === 'all' ? 365 : (startDate ? Math.ceil((endDate - startDate) / (1000 * 60 * 60 * 24)) : 30);
        
        for (let i = 0; i < totalCompletedTasks; i++) {
            // Create a deterministic date within the specified range
            const completionDate = new Date(endDate);
            const daysBack = pseudoRandom(maxDaysBack);
            completionDate.setDate(completionDate.getDate() - daysBack);
            
            // Skip if the date is outside the specified range
            if (startDate && completionDate < startDate) {
                continue;
            }
            
            // Calculate progress percentage (each task contributes to overall progress)
            const totalTasks = projectMetrics.reduce((sum, project) => sum + project.tasksCount, 0);
            const taskProgressIncrement = totalTasks > 0 ? (1 / totalTasks) * 100 : 0;
            cumulativeProgress += taskProgressIncrement;
            
            // Add this task completion to our dataset
            data.push({
                taskId: i + 1,
                completionDate: completionDate,
                progress: cumulativeProgress
            });
        }
        
        // If we have no data after filtering, create a placeholder
        if (data.length === 0) {
            console.log("No tasks found within the selected date range");
            return [];
        }
        
        // Sort by date
        data.sort((a, b) => a.completionDate - b.completionDate);
        
        // Normalize progress values to ensure the final value is 100% of completed tasks
        if (data.length > 0) {
            const normalizationFactor = (data.length / projectMetrics.reduce((sum, p) => sum + p.tasksCount, 0)) * 100 / data[data.length - 1].progress;
            data.forEach(task => {
                task.progress = task.progress * normalizationFactor;
            });
        }
        
        console.log(`Generated ${data.length} completed tasks for the simulation`);
        return data;
    } catch (error) {
        console.error("Error simulating completed tasks data:", error);
        return [];
    }
} 