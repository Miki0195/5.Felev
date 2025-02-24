let currentCharts = {
    projectProgress: null,
    taskDistribution: null
};

document.addEventListener('DOMContentLoaded', function() {
    initializeFilters();
    loadReportData();
});

async function initializeFilters() {
    try {
        // Initialize Select2 for multiple select filters
        $('#statusFilter, #priorityFilter, #teamFilter').select2({
            placeholder: 'Select options',
            allowClear: true
        });

        // Load projects for filter
        const projectResponse = await fetch('/api/reports/projects');
        if (!projectResponse.ok) throw new Error(`Failed to fetch projects: ${projectResponse.status}`);
        const projects = await projectResponse.json();
        
        const projectFilter = document.getElementById('projectFilter');
        if (projects && Array.isArray(projects)) {
            projects.forEach(project => {
                const option = new Option(project.name, project.id);
                projectFilter.appendChild(option);
            });
        }

        // Load team members for filter
        const teamResponse = await fetch('/api/projectsapi/team-members');
        if (!teamResponse.ok) throw new Error(`Failed to fetch team members: ${teamResponse.status}`);
        const members = await teamResponse.json();
        
        const teamFilter = document.getElementById('teamFilter');
        if (members && Array.isArray(members)) {
            members.forEach(member => {
                const option = new Option(`${member.name} ${member.lastName}`, member.id);
                teamFilter.appendChild(option);
            });
        }

        // Add change event listeners
        ['dateRangeFilter', 'projectFilter', 'statusFilter', 'priorityFilter', 'teamFilter'].forEach(filterId => {
            const element = document.getElementById(filterId);
            if (element) {
                element.addEventListener('change', loadReportData);
            }
        });
    } catch (error) {
        console.error('Error initializing filters:', error);
        const toastContainer = document.getElementById('toastContainer');
        if (toastContainer) {
            showToast('Error initializing filters: ' + error.message, 'error');
        }
    }
}

async function loadReportData() {
    try {
        const projectId = document.getElementById('projectFilter').value;
        const dateRange = document.getElementById('dateRangeFilter').value;
        
        // Show loading state
        document.getElementById('teamPerformanceTable').querySelector('tbody').innerHTML = `
            <tr>
                <td colspan="5" class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `;
        
        let startDate, endDate;
        if (dateRange !== 'custom') {
            const days = parseInt(dateRange);
            endDate = new Date();
            startDate = new Date();
            startDate.setDate(startDate.getDate() - days);
        }

        const queryParams = new URLSearchParams();
        if (startDate) queryParams.append('startDate', startDate.toISOString());
        if (endDate) queryParams.append('endDate', endDate.toISOString());
        if (projectId) queryParams.append('projectId', projectId);

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

        updateQuickStats(projectMetrics, taskDistribution);
        updateCharts(projectMetrics, taskDistribution);
        updateTeamPerformance(userProductivity);
    } catch (error) {
        console.error('Error loading report data:', error);
        const toastContainer = document.getElementById('toastContainer');
        if (toastContainer) {
            showToast('Error loading report data: ' + error.message, 'error');
        }
        
        // Show error state in table
        document.getElementById('teamPerformanceTable').querySelector('tbody').innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-danger">
                    Error loading data. Please try again.
                </td>
            </tr>
        `;
    }
}

function updateQuickStats(projectMetrics, taskDistribution) {
    document.getElementById('totalProjects').textContent = projectMetrics.length;
    document.getElementById('activeTasks').textContent = 
        taskDistribution.find(t => t.status === 'Active')?.count || 0;
    
    const totalTasks = taskDistribution.reduce((sum, t) => sum + t.count, 0);
    const completedTasks = taskDistribution.find(t => t.status === 'Completed')?.count || 0;
    const productivity = totalTasks ? Math.round((completedTasks / totalTasks) * 100) : 0;
    
    document.getElementById('teamProductivity').textContent = `${productivity}%`;
    document.getElementById('overdueTasks').textContent = 
        projectMetrics.reduce((sum, p) => sum + p.overdueTasks, 0);
}

function updateCharts(projectMetrics, taskDistribution) {
    // Destroy existing charts before creating new ones
    if (currentCharts.projectProgress) {
        currentCharts.projectProgress.destroy();
    }
    if (currentCharts.taskDistribution) {
        currentCharts.taskDistribution.destroy();
    }

    // Project Progress Chart
    const progressCtx = document.getElementById('projectProgressChart').getContext('2d');
    currentCharts.projectProgress = new Chart(progressCtx, {
        type: 'bar',
        data: {
            labels: projectMetrics.map(p => p.name),
            datasets: [{
                label: 'Progress (%)',
                data: projectMetrics.map(p => p.progress),
                backgroundColor: '#007bff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100
                }
            }
        }
    });

    // Task Distribution Chart
    const distributionCtx = document.getElementById('taskDistributionChart').getContext('2d');
    currentCharts.taskDistribution = new Chart(distributionCtx, {
        type: 'doughnut',
        data: {
            labels: taskDistribution.map(t => t.status),
            datasets: [{
                data: taskDistribution.map(t => t.count),
                backgroundColor: [
                    '#28a745',
                    '#ffc107',
                    '#dc3545',
                    '#6c757d'
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });
}

function updateTeamPerformance(userProductivity) {
    const tbody = document.querySelector('#teamPerformanceTable tbody');
    
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
                            <div class="fw-bold">${user.name} ${user.lastName}</div>
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
}

function exportTeamData(format) {
    const projectId = document.getElementById('projectFilter').value;
    const projectName = document.getElementById('projectFilter').selectedOptions[0].text;
    
    const table = document.getElementById('teamPerformanceTable');
    const rows = Array.from(table.querySelectorAll('tbody tr'));
    
    if (format === 'csv') {
        let csv = 'Member,Tasks Completed,Working Hours,Productivity Score\n';
        
        rows.forEach(row => {
            const cells = row.querySelectorAll('td');
            const name = cells[0].querySelector('.fw-bold').textContent;
            const tasksCompleted = cells[1].textContent.trim();
            const hours = cells[2].textContent.trim();
            const productivity = cells[3].querySelector('span:last-child').textContent;
            
            csv += `${name},${tasksCompleted},${hours},${productivity}\n`;
        });
        
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `team-performance-${projectName}-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
    } else if (format === 'pdf') {
        // Implement PDF export if needed
        showToast('PDF export coming soon', 'info');
    }
} 