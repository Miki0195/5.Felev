let selectedUsers = new Set(); // Store selected user IDs
let currentProjectMembers = new Set(); // Store current project members

function createUserSearchContainer(containerId, resultsId, selectedId) {
    return `
                <div class="user-search-container">
                    <input type="text" 
                           class="user-search-input" 
                           placeholder="Search users..."
                           oninput="handleUserSearch(this.value, '${resultsId}')">
                    <div id="${resultsId}" class="user-search-results"></div>
                </div>
                <div id="${selectedId}" class="selected-users-container"></div>
            `;
}

function createUserSearchItem(user, resultsId, selectedId) {
    return `
                <div class="user-search-item" 
                     onclick="selectUser(${JSON.stringify(user).replace(/"/g, '&quot;')}, '${resultsId}', '${selectedId}')">
                    <img src="${user.profilePicturePath}" alt="Avatar" class="user-avatar">
                    <span>${user.name} ${user.lastName}</span>
                </div>
            `;
}

function createSelectedUserItem(user) {
    return `
                <div class="selected-user-item" id="selected-user-${user.id}">
                    <div class="selected-user-info">
                        <img src="${user.profilePicturePath}" alt="Avatar" class="user-avatar">
                        <span>${user.name} ${user.lastName}</span>
                    </div>
                    <button class="remove-user-btn" onclick="removeSelectedUser('${user.id}')">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
            `;
}

let allUsers = []; // Store all users for filtering

async function loadUserSearch(containerId) {
    try {
        const response = await fetch('/api/projectsapi/company-users');
        allUsers = await response.json();

        const container = document.getElementById(containerId);
        container.innerHTML = createUserSearchContainer(
            containerId,
            `${containerId}-results`,
            `${containerId}-selected`
        );
    } catch (error) {
        console.error('Error loading users:', error);
        showToast('Error loading users', 'error');
    }
}

async function loadAvailableUsers(projectId = null) {
    try {
        const response = await fetch('/api/projectsapi/company-users');
        allUsers = await response.json();

        // If projectId is provided, load current project members
        if (projectId) {
            const projectResponse = await fetch(`/api/projectsapi/${projectId}`);
            const project = await projectResponse.json();
            currentProjectMembers = new Set(project.projectMembers.map(member => member.user.id));
        } else {
            currentProjectMembers.clear();
        }
    } catch (error) {
        console.error('Error loading users:', error);
        showToast('Error loading users', 'error');
    }
}

function handleUserSearch(searchTerm, resultsId) {
    const resultsContainer = document.getElementById(resultsId);
    if (!resultsContainer) return;

    // If no search term, clear results and return
    if (!searchTerm) {
        resultsContainer.innerHTML = '';
        resultsContainer.classList.remove('active');
        return;
    }

    const filteredUsers = allUsers.filter(user =>
        !selectedUsers.has(user.id) && // Exclude selected users
        !currentProjectMembers.has(user.id) && // Exclude existing project members
        (user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            user.lastName.toLowerCase().includes(searchTerm.toLowerCase()))
    );

    if (filteredUsers.length > 0) {
        resultsContainer.innerHTML = filteredUsers
            .map(user => `
                        <div class="user-search-item" onclick="selectUser(${JSON.stringify(user).replace(/"/g, '&quot;')})">
                            <img src="${user.profilePicturePath}" alt="Avatar" class="user-avatar">
                            <span>${user.name} ${user.lastName}</span>
                        </div>
                    `).join('');
        resultsContainer.classList.add('active');
    } else {
        resultsContainer.innerHTML = '';
        resultsContainer.classList.remove('active');
    }
}

function selectUser(user) {
    if (!selectedUsers.has(user.id)) {
        selectedUsers.add(user.id);

        // Clear search results and input immediately
        const resultsContainer = document.getElementById('createProjectSearchResults') ||
            document.getElementById('addMembersSearchResults');
        const searchInput = document.querySelector('.user-search-input');

        // Clear search results
        if (resultsContainer) {
            resultsContainer.innerHTML = '';
            resultsContainer.classList.remove('active');
        }

        // Clear search input
        if (searchInput) {
            searchInput.value = '';
        }

        // Refresh the display of selected users
        refreshSelectedUsersDisplay();
    }
}

function removeSelectedUser(userId) {
    selectedUsers.delete(userId);
    refreshSelectedUsersDisplay();
}

function openCreateProjectModal() {
    const today = new Date().toISOString().split('T')[0];
    selectedUsers.clear(); // Clear previously selected users

    const contentArea = document.getElementById('projectsContent');
    contentArea.innerHTML = `
                <div class="create-project-form">
                    <h2>Create New Project</h2>
                    <form id="createProjectForm" class="mt-4">
                        <div class="mb-3">
                            <label class="form-label">Project Name</label>
                            <input type="text" class="form-control" id="projectName" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Description</label>
                            <textarea class="form-control" id="projectDescription" rows="3"></textarea>
                        </div>
                        <div class="row mb-3">
                            <div class="col">
                                <label class="form-label">Start Date</label>
                                <input type="date" 
                                       class="form-control" 
                                       id="startDate" 
                                       required
                                       min="${today}"
                                       value="${today}">
                            </div>
                            <div class="col">
                                <label class="form-label">Deadline</label>
                                <input type="date" 
                                       class="form-control" 
                                       id="deadline" 
                                       required
                                       min="${today}">
                            </div>
                        </div>
                        <div class="row mb-3">
                            <div class="col">
                                <label class="form-label">Priority</label>
                                <select class="form-select" id="priority">
                                    <option value="Low">Low</option>
                                    <option value="Medium" selected>Medium</option>
                                    <option value="High">High</option>
                                </select>
                            </div>
                        </div>
                        <div class="mb-4">
                            <label class="form-label">Team Members</label>
                            <div id="createProjectSearchContainer">
                                <div class="user-search-container">
                                    <input type="text" 
                                           class="user-search-input" 
                                           placeholder="Search users..."
                                           oninput="handleUserSearch(this.value, 'createProjectSearchResults')">
                                    <div id="createProjectSearchResults" class="user-search-results"></div>
                                </div>
                                <div id="createProjectSelectedUsers" class="selected-users-container"></div>
                            </div>
                        </div>
                        <div class="d-flex justify-content-end gap-2">
                            <button type="button" class="btn btn-secondary" onclick="showWelcomeMessage()">Cancel</button>
                            <button type="submit" class="btn btn-primary">Create Project</button>
                        </div>
                    </form>
                </div>
            `;

    // Add event listener to ensure deadline is after start date
    document.getElementById('startDate').addEventListener('change', function () {
        document.getElementById('deadline').min = this.value;
    });

    // Load users after setting up the containers
    loadAvailableUsers();
    document.getElementById('createProjectForm').addEventListener('submit', handleCreateProject);
}

function showWelcomeMessage() {
    const contentArea = document.getElementById('projectsContent');
    contentArea.innerHTML = `
                <div class="welcome-message">
                    <h2>Welcome to Projects</h2>
                    <p>Select a project from the sidebar or create a new one to get started.</p>
                </div>
            `;
}

async function handleCreateProject(e) {
    e.preventDefault();

    try {
        // Format dates to match expected pattern (yyyy-MM-dd)
        const startDate = new Date(document.getElementById('startDate').value);
        const deadline = new Date(document.getElementById('deadline').value);

        const projectData = {
            name: document.getElementById('projectName').value,
            description: document.getElementById('projectDescription').value || '',
            startDate: startDate.toISOString().split('T')[0],  // Format as yyyy-MM-dd
            deadline: deadline.toISOString().split('T')[0],    // Format as yyyy-MM-dd
            priority: document.getElementById('priority').value,
            status: 'Not Started',
            teamMemberIds: Array.from(selectedUsers)
        };

        console.log('Creating project with data:', projectData); // For debugging

        const response = await fetch('/api/projectsapi/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(projectData)
        });

        if (!response.ok) {
            const errorData = await response.json();
            console.error('Server response:', errorData);
            throw new Error(errorData.error || 'Failed to create project');
        }

        showToast('Project created successfully!', 'success');
        await loadProjects();
        showWelcomeMessage();
    } catch (error) {
        console.error('Error creating project:', error);
        showToast(error.message, 'error');
    }
}

function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : 'success'} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            `;

    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

// Load projects on page load
document.addEventListener('DOMContentLoaded', () => {
    loadProjects();
    
    // Also load archived projects during initial page load
    const archivedList = document.getElementById('archivedProjectsList');
    if (archivedList && archivedList.children.length === 0) {
        loadArchivedProjects();
    }
    
    // Update the project count badges
    updateProjectCountBadges();
});

async function loadProjects() {
    try {
        const response = await fetch('/api/ProjectsApi');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Error loading projects');
        }
        
        const data = await response.json();
        const projectsList = document.getElementById('projectsList');
        projectsList.innerHTML = '';
        
        if (data.length === 0) {
            projectsList.innerHTML = '<div class="project-item text-muted">No active projects</div>';
            return;
        }
        
        // Filter out completed projects
        const activeProjects = data.filter(project => project.status !== "Completed");
        
        if (activeProjects.length === 0) {
            projectsList.innerHTML = '<div class="project-item text-muted">No active projects</div>';
            return;
        }
        
        activeProjects.forEach(project => {
            const projectItem = document.createElement('div');
            projectItem.className = 'project-item';
            projectItem.innerHTML = `
                <div onclick="loadProject(${project.id})" style="cursor: pointer;">
                    <i class="bi bi-file-earmark-text"></i>
                    <span>${project.name}</span>
                </div>
            `;
            projectsList.appendChild(projectItem);
        });
        
        console.log('Active projects loaded successfully:', activeProjects.length);
    } catch (error) {
        console.error('Failed to load projects:', error);
        showToast('Failed to load projects: ' + error.message, 'error');
    }
}

function toggleProjectsList() {
    const projectsList = document.getElementById('projectsList');
    const icon = event.currentTarget.querySelector('.bi-chevron-down, .bi-chevron-up');
    
    projectsList.classList.toggle('active');
    
    // Toggle icon
    if (icon) {
        if (icon.classList.contains('bi-chevron-down')) {
            icon.classList.remove('bi-chevron-down');
            icon.classList.add('bi-chevron-up');
        } else {
            icon.classList.remove('bi-chevron-up');
            icon.classList.add('bi-chevron-down');
        }
    }
}

async function loadProject(projectId) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) throw new Error('Failed to load project');
        
        const project = await response.json();
        
        const projectsContent = document.getElementById('projectsContent');
        
        // Check if the project is completed (archived)
        if (project.status === 'Completed') {
            loadArchivedProjectView(project);
            return;
        }
        
        // Calculate progress percentage
        const progressPercent = project.totalTasks > 0 
            ? Math.round((project.completedTasks / project.totalTasks) * 100) 
            : 0;
        
        // Determine status badge color
        const statusBadgeClass = getStatusBadgeColor(project.status);
        const priorityBadgeClass = getPriorityBadgeColor(project.priority);
        
        // Format dates for display
        const startDateFormatted = formatDate(project.startDate);
        const deadlineFormatted = formatDate(project.deadline);
        
        // Template for project details
        let html = `
            <div class="project-details">
                <div class="d-flex justify-content-between align-items-center mb-4">
                    <h2>${project.name}</h2>
                    <div class="btn-group">
                        ${project.isProjectLead ? `
                            <button class="btn btn-outline-primary" onclick="showEditProjectModal(${project.id})">
                                <i class="bi bi-pencil"></i> Edit
                            </button>
                            <button class="btn btn-outline-danger" onclick="showDeleteConfirmation(${project.id})">
                                <i class="bi bi-trash"></i> Delete
                            </button>
                        ` : ''}
                    </div>
                </div>
                
                <div class="row mb-4">
                    <div class="col-md-8">
                        <!-- New description card with better styling -->
                        <div class="card project-description-card mb-4">
                            <div class="card-body">
                                <h5 class="card-title mb-3">
                                    <i class="bi bi-file-text"></i> Project Details
                                </h5>
                                
                                <div class="project-description-content">
                                    ${project.description ? 
                                        `<p class="description-text">${project.description}</p>` : 
                                        '<p class="text-muted fst-italic">No description provided.</p>'
                                    }
                                </div>
                                
                                <div class="row g-3 mt-3">
                                    <div class="col-md-6 col-lg-3">
                                        <div class="detail-item">
                                            <span class="detail-label"><i class="bi bi-calendar-check"></i> Started:</span>
                                            <span class="detail-value">${startDateFormatted}</span>
                                        </div>
                                    </div>
                                    <div class="col-md-6 col-lg-3">
                                        <div class="detail-item">
                                            <span class="detail-label"><i class="bi bi-calendar-event"></i> Deadline:</span>
                                            <span class="detail-value">${deadlineFormatted}</span>
                                        </div>
                                    </div>
                                    <div class="col-md-6 col-lg-3">
                                        <div class="detail-item">
                                            <span class="detail-label"><i class="bi bi-info-circle"></i> Status:</span>
                                            <span class="badge bg-${statusBadgeClass}">${project.status}</span>
                                        </div>
                                    </div>
                                    <div class="col-md-6 col-lg-3">
                                        <div class="detail-item">
                                            <span class="detail-label"><i class="bi bi-flag"></i> Priority:</span>
                                            <span class="badge bg-${priorityBadgeClass}">${project.priority}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <!-- Project progress card -->
                        <div class="card project-progress-card">
                            <div class="card-body">
                                <h5 class="card-title mb-3">
                                    <i class="bi bi-graph-up"></i> Progress
                                </h5>
                                
                                <div class="progress mb-2" style="height: 20px;">
                                    <div class="progress-bar bg-success" role="progressbar" 
                                         style="width: ${progressPercent}%" 
                                         aria-valuenow="${progressPercent}" 
                                         aria-valuemin="0" 
                                         aria-valuemax="100">
                                        ${progressPercent}%
                                    </div>
                                </div>
                                <p class="text-center text-muted">
                                    <strong>${project.completedTasks}</strong> of <strong>${project.totalTasks}</strong> tasks completed
                                </p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <!-- Activity Feed will be loaded here -->
                        <div id="activityFeedContainer"></div>
                    </div>
                </div>
                
                <div class="team-members mb-4">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <h4><i class="bi bi-people"></i> Team Members</h4>
                        ${project.isProjectLead ? `
                            <div class="btn-group">
                                <button class="btn btn-sm btn-outline-primary" onclick="showAddMembersModal(${project.id})">
                                    <i class="bi bi-person-plus"></i> Add Members
                                </button>
                                <button class="btn btn-sm btn-outline-secondary" onclick="showManageMembersModal(${project.id})">
                                    <i class="bi bi-gear"></i> Manage
                                </button>
                            </div>
                        ` : ''}
                    </div>
                    <div class="members-list">
                        ${project.projectMembers.map(member => `
                            <div class="member-card">
                                <img src="${member.user.profilePicturePath}" 
                                     alt="${member.user.name}" 
                                     class="member-avatar"
                                     onerror="this.onerror=null; this.src='/images/default-avatar.png';">
                                <div class="member-info">
                                    <h6 class="member-name">${member.user.name} ${member.user.lastName}</h6>
                                    <p class="member-role">${member.role}</p>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
                
                <div class="tasks-container">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <h4><i class="bi bi-list-check"></i> Tasks</h4>
                        ${project.isProjectLead ? `
                            <button class="btn btn-sm btn-primary" onclick="showCreateTaskModal(${project.id})">
                                <i class="bi bi-plus-lg"></i> Add Task
                            </button>
                        ` : ''}
                    </div>
                    <div class="tasks-filters mb-3">
                        <div class="btn-group" role="group" aria-label="Task filters">
                            <button type="button" class="btn btn-outline-secondary active" onclick="filterTasks(${project.id}, 'all')">All</button>
                            <button type="button" class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'my')">My Tasks</button>
                            <button type="button" class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'pending')">Pending</button>
                            <button type="button" class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'completed')">Completed</button>
                        </div>
                    </div>
                    <div id="tasksDisplay">
                        ${project.tasks.length > 0 ? 
                            project.tasks.map(task => createTaskCard(task, project.currentUserId, project.id)).join('') : 
                            '<div class="no-tasks-message"><i class="bi bi-info-circle"></i> No tasks found. Create a new task to get started.</div>'
                        }
                    </div>
                </div>
            </div>
        `;
        
        projectsContent.innerHTML = html;
        
        // Load the activity feed
        const activityFeedContainer = document.getElementById('activityFeedContainer');
        activityFeedContainer.innerHTML = '<div id="activityFeed"></div>';
        loadActivityFeed(projectId);
        
        // Initialize filter buttons
        const filterButtons = document.querySelectorAll('.tasks-filters .btn-group .btn');
        filterButtons.forEach(button => {
            button.addEventListener('click', function() {
                filterButtons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');
            });
        });
        
    } catch (error) {
        console.error('Error loading project:', error);
        showToast('Failed to load project details.', 'danger');
    }
}

function createTaskCard(task, currentUserId, projectId) {
    const isAssignedToMe = task.assignedUsers.some(u => u.id === currentUserId);
    const isDone = task.status === 'Completed';

    return `
                <div class="task-card ${task.priority.toLowerCase()} ${isDone ? 'completed' : ''}" 
                     onclick="showTaskDetails(${task.id})">
                    <div class="task-header">
                        <h5 class="task-title">${task.taskTitle}</h5>
                        <div class="d-flex align-items-center gap-2">
                            <span class="badge task-priority ${task.priority.toLowerCase()}">${task.priority}</span>
                            ${isAssignedToMe ? `
                                <div class="form-check form-switch" onclick="event.stopPropagation()">
                                    <input class="form-check-input" type="checkbox" 
                                           ${isDone ? 'checked' : ''}
                                           onclick="updateTaskStatus(${task.id}, ${projectId}, '${isDone ? 'Pending' : 'Completed'}', event)"
                                           ${!isAssignedToMe ? 'disabled' : ''}>
                                </div>
                            ` : ''}
                        </div>
                    </div>
                    <p class="task-description">${task.description || 'No description'}</p>
                    <div class="task-meta">
                        <div class="assigned-users">
                            <span class="assigned-label">Assigned to:</span>
                            <div class="assigned-users-list">
                                ${task.assignedUsers.map(user => `
                                    <div class="assigned-user">
                                        <img src="${user.profilePicturePath}" alt="${user.name}" class="user-avatar"
                                             onerror="this.onerror=null; this.src='/images/default-avatar.png';">
                                        <span class="user-name">${user.name} ${user.lastName}</span>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                        <span>Due: ${new Date(task.dueDate).toLocaleDateString()}</span>
                    </div>
                </div>
            `;
}

function showDeleteConfirmation(projectId) {
    // Update the Delete button to include the projectId
    const deleteButton = document.querySelector('#deleteConfirmationModal .btn-danger');
    deleteButton.setAttribute('onclick', `deleteProject(${projectId})`);

    const modal = new bootstrap.Modal(document.getElementById('deleteConfirmationModal'));
    modal.show();
}

async function deleteProject(projectId) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to delete project');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmationModal'));
        modal.hide();

        // Show success message
        showToast('Project deleted successfully', 'success');

        // Refresh projects list and show welcome message
        await loadProjects();
        showWelcomeMessage();
    } catch (error) {
        console.error('Error deleting project:', error);
        showToast(error.message, 'error');
    }
}

async function showEditProjectModal(projectId) {
    try {
        // Make sure the modal exists before proceeding
        const editModal = document.getElementById('editProjectModal');
        if (!editModal) {
            throw new Error('Edit project modal not found in the DOM');
        }

        // Fetch project details
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error(`Failed to fetch project details: ${response.status} ${response.statusText}`);
        }

        const project = await response.json();
        
        // Store project ID in a data attribute on the modal itself if the hidden field doesn't exist
        const projectIdField = document.getElementById('editProjectId');
        if (projectIdField) {
            projectIdField.value = project.id;
        } else {
            // Store the ID on the modal element as a data attribute
            editModal.setAttribute('data-project-id', project.id);
            console.warn('Hidden field editProjectId not found, storing ID on modal data attribute');
        }
        
        // Check if form elements exist and log errors if they don't
        const elements = {
            name: document.getElementById('editProjectName'),
            description: document.getElementById('editProjectDescription'),
            startDate: document.getElementById('editProjectStartDate'),
            deadline: document.getElementById('editProjectDeadline'),
            status: document.getElementById('editProjectStatus'),
            priority: document.getElementById('editProjectPriority')
        };
        
        // Verify all elements exist
        let missingElements = [];
        for (const [key, element] of Object.entries(elements)) {
            if (!element) {
                missingElements.push(`editProject${key.charAt(0).toUpperCase() + key.slice(1)}`);
            }
        }
        
        if (missingElements.length > 0) {
            throw new Error(`Missing form elements: ${missingElements.join(', ')}`);
        }
        
        // Set form field values
        elements.name.value = project.name;
        elements.description.value = project.description || '';
        
        // Parse dates correctly
        const startDate = new Date(project.startDate);
        const deadline = project.deadline ? new Date(project.deadline) : null;
        
        // Format dates in YYYY-MM-DD format for input fields
        elements.startDate.value = startDate.toISOString().split('T')[0];
        elements.deadline.value = deadline ? deadline.toISOString().split('T')[0] : '';
        
        // Set select elements
        elements.status.value = project.status;
        elements.priority.value = project.priority;
        
        // Show the modal
        const bootstrapModal = new bootstrap.Modal(editModal);
        bootstrapModal.show();
    } catch (error) {
        console.error('Error showing edit project modal:', error);
        showToast(`Error showing edit project modal: ${error.message}`, 'danger');
    }
}

// Make sure to add the verifyEditProjectElements function
function verifyEditProjectElements() {
    const elements = [
        'editProjectName',
        'editProjectDescription', 
        'editProjectStartDate', 
        'editProjectDeadline',
        'editProjectStatus',
        'editProjectPriority'
    ];
    
    let allFound = true;
    elements.forEach(id => {
        const element = document.getElementById(id);
        if (!element) {
            console.error(`Element with ID ${id} not found in the DOM`);
            allFound = false;
        }
    });
    
    if (!allFound) {
        showToast('Some form elements could not be found. Please reload the page and try again.', 'warning');
    }
    
    return allFound;
}

async function updateProject() {
    try {
        // Get project ID from hidden field or data attribute
        const editProjectId = document.getElementById('editProjectId') ? 
            document.getElementById('editProjectId').value : 
            document.getElementById('editProjectModal').getAttribute('data-project-id');
            
        if (!editProjectId) {
            console.error('Project ID not found');
            showToast('Error: Project ID not found', 'danger');
            return;
        }

        // Get form elements
        const nameInput = document.getElementById('editProjectName');
        const descriptionInput = document.getElementById('editProjectDescription');
        const startDateInput = document.getElementById('editProjectStartDate');
        const deadlineInput = document.getElementById('editProjectDeadline');
        const priorityInput = document.getElementById('editProjectPriority');
        const statusInput = document.getElementById('editProjectStatus');

        // Validate form elements exist
        if (!nameInput || !startDateInput || !deadlineInput || !priorityInput || !statusInput) {
            let missingElements = [];
            if (!nameInput) missingElements.push('Project Name');
            if (!startDateInput) missingElements.push('Start Date');
            if (!deadlineInput) missingElements.push('Deadline');
            if (!priorityInput) missingElements.push('Priority');
            if (!statusInput) missingElements.push('Status');
            
            console.error(`Missing form elements: ${missingElements.join(', ')}`);
            showToast(`Error: Form is incomplete. Missing: ${missingElements.join(', ')}`, 'danger');
            return;
        }

        // Validate required fields
        if (!nameInput.value.trim() || !startDateInput.value.trim() || !deadlineInput.value.trim()) {
            showToast('Please fill all required fields', 'warning');
            return;
        }

        // Create object with form data WITH AdditionalData included to prevent NULL issues
        const projectData = {
            name: nameInput.value.trim(),
            description: descriptionInput ? descriptionInput.value.trim() : '',
            startDate: startDateInput.value.trim(),
            deadline: deadlineInput.value.trim(),
            priority: priorityInput.value,
            status: statusInput.value,
            activityLogAdditions: [
                { action: "updated project", targetType: "Project", additionalData: "{}" },
                { action: "updated deadline", targetType: "Project", additionalData: "{}" },
                { action: "changed status", targetType: "Project", additionalData: "{}" },
                { action: "changed priority", targetType: "Project", additionalData: "{}" }
            ]
        };

        // Show loading message
        document.getElementById('updateProjectButton').innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
        document.getElementById('updateProjectButton').disabled = true;

        // Get CSRF token if available
        const csrfTokenMeta = document.querySelector('meta[name="csrf-token"]');
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        };
        
        if (csrfTokenMeta) {
            headers['RequestVerificationToken'] = csrfTokenMeta.content;
        }

        console.log('Sending project update:', JSON.stringify(projectData));

        // Use normal update without any query parameters
        const response = await fetch(`/api/projectsapi/${editProjectId}/update`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(projectData)
        });

        console.log('Server response status:', response.status);
        
        // Attempt to get the response text for better error reporting
        let responseText = '';
        try {
            responseText = await response.text();
            console.log('Server response:', responseText);
        } catch (e) {
            console.error('Could not read response text:', e);
        }

        if (response.ok) {
            showToast('Project updated successfully', 'success');
            
            // Close modal
            const modalElement = document.getElementById('editProjectModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            } else {
                // Fallback if modal instance not found
                modalElement.classList.remove('show');
                modalElement.style.display = 'none';
                document.body.classList.remove('modal-open');
                document.querySelector('.modal-backdrop')?.remove();
            }
            
            // Reload project to show changes
            await loadProject(editProjectId);
        } else {
            // Handle different error responses
            let errorMessage = 'Failed to update project';
            
            try {
                if (responseText) {
                    const errorData = JSON.parse(responseText);
                    if (errorData.error) {
                        errorMessage = `Error: ${errorData.error}`;
                    }
                }
            } catch (e) {
                console.error('Error parsing error response:', e);
                errorMessage = `Server error: ${response.status}`;
                
                if (responseText) {
                    errorMessage += ` - ${responseText}`;
                }
            }
            
            console.error('Update project error:', errorMessage);
            showToast(errorMessage, 'danger');
        }
    } catch (error) {
        console.error('Error in updateProject:', error);
        showToast('An unexpected error occurred', 'danger');
    } finally {
        // Reset button
        document.getElementById('updateProjectButton').innerHTML = 'Save Changes';
        document.getElementById('updateProjectButton').disabled = false;
    }
}

function showAddMembersModal(projectId) {
    selectedUsers.clear(); // Clear previously selected users

    // First create the modal
    const modal = new bootstrap.Modal(document.getElementById('addMembersModal'));

    // Update the Add Members button to include the projectId
    const addButton = document.querySelector('#addMembersModal .btn-primary');
    addButton.setAttribute('onclick', `addProjectMembers(${projectId})`);

    modal.show();

    // After modal is shown, initialize the user search
    const searchContainer = document.querySelector('#addMembersModal .modal-body');
    searchContainer.innerHTML = `
                <div class="user-search-container">
                    <input type="text" 
                           class="user-search-input" 
                           placeholder="Search users..."
                           oninput="handleUserSearch(this.value, 'addMembersSearchResults')">
                    <div id="addMembersSearchResults" class="user-search-results"></div>
                </div>
                <div id="addMembersSelectedUsers" class="selected-users-container"></div>
            `;

    // Load users after setting up the containers, passing the project ID
    loadAvailableUsers(projectId);
}

// Add this new function to refresh the selected users display
function refreshSelectedUsersDisplay() {
    const selectedContainer = document.getElementById('addMembersSelectedUsers') ||
        document.getElementById('createProjectSelectedUsers');
    if (!selectedContainer) return;

    // Clear the container
    selectedContainer.innerHTML = '';

    // Add all selected users to the display
    selectedUsers.forEach(userId => {
        const user = allUsers.find(u => u.id === userId);
        if (user) {
            selectedContainer.insertAdjacentHTML('beforeend', createSelectedUserItem(user));
        }
    });
}

async function addProjectMembers(projectId) {
    try {
        if (selectedUsers.size === 0) {
            showToast('Please select at least one member to add', 'error');
            return;
        }

        const response = await fetch(`/api/projectsapi/${projectId}/members`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ memberIds: Array.from(selectedUsers) })
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to add members');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('addMembersModal'));
        modal.hide();

        // Show success message
        showToast('Team members added successfully', 'success');

        // Reload project details
        await loadProject(projectId);
    } catch (error) {
        console.error('Error adding members:', error);
        showToast(error.message, 'error');
    }
}

async function showManageMembersModal(projectId) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        const modalBody = document.querySelector('#manageMembersModal .modal-body');
        modalBody.innerHTML = `
                    <div class="table-responsive">
                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Member</th>
                                    <th>Role</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${project.projectMembers.map(member => `
                                    <tr>
                                        <td>
                                            <div class="d-flex align-items-center gap-2">
                                                <img src="${member.user.profilePicturePath}" alt="Avatar" class="user-avatar">
                                                <span>${member.user.name} ${member.user.lastName}</span>
                                            </div>
                                        </td>
                                        <td>
                                            <select class="form-select" 
                                                    onchange="updateMemberRole(${project.id}, '${member.user.id}', this.value)"
                                                    ${member.role === 'Project Lead' ? 'disabled' : ''}>
                                                <option value="Member" ${member.role === 'Member' ? 'selected' : ''}>Member</option>
                                                <option value="Manager" ${member.role === 'Manager' ? 'selected' : ''}>Manager</option>
                                                <option value="Project Lead" ${member.role === 'Project Lead' ? 'selected' : ''}>Project Lead</option>
                                            </select>
                                        </td>
                                        <td>
                                            ${member.role !== 'Project Lead' ? `
                                                <button class="btn btn-danger btn-sm" 
                                                        onclick="removeMember(${project.id}, '${member.user.id}')">
                                                    Remove
                                                </button>
                                            ` : ''}
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                `;

        const modal = new bootstrap.Modal(document.getElementById('manageMembersModal'));
        modal.show();
    } catch (error) {
        console.error('Error showing manage members modal:', error);
        showToast(error.message, 'error');
    }
}

async function updateMemberRole(projectId, userId, newRole) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}/members/${userId}/role`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ role: newRole })
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to update member role');
        }

        showToast('Member role updated successfully', 'success');

        // First hide the current modal and remove the backdrop
        const currentModal = bootstrap.Modal.getInstance(document.getElementById('manageMembersModal'));
        if (currentModal) {
            currentModal.hide();
            // Remove modal backdrop
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
        }

        // Then refresh the project content
        await loadProject(projectId);

        // Finally, show the modal again
        setTimeout(() => {
            showManageMembersModal(projectId);
        }, 100);
    } catch (error) {
        console.error('Error updating member role:', error);
        showToast(error.message, 'error');
    }
}

async function removeMember(projectId, userId) {
    if (!confirm('Are you sure you want to remove this member from the project?')) {
        return;
    }

    try {
        const response = await fetch(`/api/projectsapi/${projectId}/members/${userId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to remove member');
        }

        showToast('Member removed successfully', 'success');

        // First hide the current modal and remove the backdrop
        const currentModal = bootstrap.Modal.getInstance(document.getElementById('manageMembersModal'));
        if (currentModal) {
            currentModal.hide();
            // Remove modal backdrop
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
        }

        // Then refresh the project content
        await loadProject(projectId);

        // Finally, show the modal again
        setTimeout(() => {
            showManageMembersModal(projectId);
        }, 100);
    } catch (error) {
        console.error('Error removing member:', error);
        showToast(error.message, 'error');
    }
}

async function showCreateTaskModal(projectId) {
    try {
        const modal = document.getElementById('createTaskModal');
        if (!modal) {
            console.error('Create task modal not found in the DOM');
            showToast('Error: Modal not found. Please reload the page.', 'error');
            return;
        }

        // Check if form elements exist
        const formElements = {
            'taskTitle': document.getElementById('taskTitle'),
            'taskDescription': document.getElementById('taskDescription'),
            'taskDueDate': document.getElementById('taskDueDate'),
            'taskPriority': document.getElementById('taskPriority'),
            'taskAssignedTo': document.getElementById('taskAssignedTo')
        };

        const missingElements = [];
        Object.entries(formElements).forEach(([id, element]) => {
            if (!element) {
                console.error(`Form element #${id} not found`);
                missingElements.push(id);
            }
        });
        
        if (missingElements.length > 0) {
            console.error('Missing form elements:', missingElements.join(', '));
            showToast('Error: Some form elements are missing. Please reload the page.', 'error');
            return;
        }

        // Get project members for the select dropdown
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        // Update assigned users dropdown
        const assignedToSelect = document.getElementById('taskAssignedTo');
        if (!assignedToSelect) {
            throw new Error('Cannot find the assigned users dropdown');
        }
        
        // Clear previous options
        assignedToSelect.innerHTML = '';
        
        // Add new options
        project.projectMembers.forEach(member => {
            const option = document.createElement('option');
            option.value = member.user.id;
            option.textContent = `${member.user.name} ${member.user.lastName} (${member.role})`;
            assignedToSelect.appendChild(option);
        });

        // Set minimum date to today
        const today = new Date().toISOString().split('T')[0];
        const dueDateInput = document.getElementById('taskDueDate');
        if (dueDateInput) {
            dueDateInput.min = today;
            dueDateInput.value = today;
        }

        // Reset other form values
        formElements.taskTitle.value = '';
        formElements.taskDescription.value = '';
        formElements.taskPriority.value = 'Medium';

        // Update Create Task button to include projectId
        const createTaskBtn = modal.querySelector('.modal-footer .btn-primary');
        if (createTaskBtn) {
            createTaskBtn.setAttribute('onclick', `createTask(${projectId})`);
        } else {
            console.error('Create task button not found');
        }

        // Show the modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
        console.log('Create task modal displayed successfully');
    } catch (error) {
        console.error('Error showing create task modal:', error);
        showToast('Failed to load task creation form. Please try again.', 'error');
    }
}

async function createTask(projectId) {
    try {
        console.log('Creating task for project:', projectId);
        
        // Get form elements with correct IDs
        const titleInput = document.getElementById('taskTitle');
        const descriptionInput = document.getElementById('taskDescription');
        const dueDateInput = document.getElementById('taskDueDate');
        const priorityInput = document.getElementById('taskPriority');
        const assignedToInput = document.getElementById('taskAssignedTo');

        // Log which elements are missing for debugging
        const missingElements = [];
        if (!titleInput) missingElements.push('taskTitle');
        if (!descriptionInput) missingElements.push('taskDescription');
        if (!dueDateInput) missingElements.push('taskDueDate');
        if (!priorityInput) missingElements.push('taskPriority');
        if (!assignedToInput) missingElements.push('taskAssignedTo');

        if (missingElements.length > 0) {
            console.error('Missing form elements:', missingElements.join(', '));
            showToast('Error: Some form elements are missing. Please try again.', 'error');
            return;
        }

        // Get form values
        const taskTitle = titleInput.value.trim();
        const description = descriptionInput.value.trim();
        const dueDate = dueDateInput.value;
        const priority = priorityInput.value;
        
        // Get selected users from the multi-select
        const assignedToId = assignedToInput.value;

        if (!taskTitle || !dueDate || !assignedToId) {
            showToast('Please fill out all required fields and assign at least one user', 'error');
            return;
        }

        // Create data object matching your TaskCreateDto structure
        // Include additionalData to prevent null in ActivityLog
        const taskData = {
            taskTitle: taskTitle,
            description: description || "",
            dueDate: dueDate,
            priority: priority,
            assignedToId: assignedToId,  // Make sure this matches your controller parameter name
            // Add additional data for the ActivityLog to prevent null errors
            activityLogAdditions: [
                { action: "created task", targetType: "Task", additionalData: "{}" },
                { action: "assigned task", targetType: "Task", additionalData: "{}" }
            ]
        };

        console.log('Creating task with data:', taskData);

        // Set button to loading state
        const createButton = document.querySelector('#createTaskModal .btn-primary');
        if (createButton) {
            createButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Creating...';
            createButton.disabled = true;
        }

        const response = await fetch(`/api/projectsapi/${projectId}/tasks`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(taskData)
        });

        // Log full response for debugging
        console.log('Server response status:', response.status);
        const responseText = await response.text();
        console.log('Response from server:', responseText);

        if (!response.ok) {
            console.error('Error response:', responseText);
            throw new Error('Failed to create task: ' + (response.status === 400 ? 'Invalid data' : 'Server error'));
        }

        // Close the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('createTaskModal'));
        if (modal) {
            modal.hide();
        } else {
            // Fallback if modal instance not found
            const modalElement = document.getElementById('createTaskModal');
            if (modalElement) {
                modalElement.classList.remove('show');
                modalElement.style.display = 'none';
                document.body.classList.remove('modal-open');
                document.querySelector('.modal-backdrop')?.remove();
            }
        }
        
        showToast('Task created successfully', 'success');
        
        // Reload the project to show the new task
        await loadProject(projectId);
    } catch (error) {
        console.error('Error creating task:', error);
        showToast('Error creating task: ' + error.message, 'error');
    } finally {
        // Reset button state
        const createButton = document.querySelector('#createTaskModal .btn-primary');
        if (createButton) {
            createButton.innerHTML = 'Create Task';
            createButton.disabled = false;
        }
    }
}

// Add this function to handle task filtering
async function filterTasks(projectId, filter) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        // Correct element ID - tasksDisplay instead of non-existent tasksList
        const tasksDisplay = document.getElementById('tasksDisplay');
        if (!tasksDisplay) {
            throw new Error('Tasks display container not found');
        }
        
        const currentUserId = project.currentUserId;

        // Update button states
        const buttons = document.querySelectorAll('.tasks-filters .btn-group .btn');
        buttons.forEach(btn => btn.classList.remove('active'));
        const activeButton = document.querySelector(`.tasks-filters .btn:nth-child(${getButtonIndex(filter)})`);
        if (activeButton) {
            activeButton.classList.add('active');
        }

        // Filter tasks
        let filteredTasks = project.tasks || [];

        switch (filter) {
            case 'my':
                filteredTasks = filteredTasks.filter(task =>
                    task.assignedUsers.some(u => u.id === currentUserId)
                );
                break;
            case 'pending':
                filteredTasks = filteredTasks.filter(task =>
                    task.status !== 'Completed'
                );
                break;
            case 'completed':
                filteredTasks = filteredTasks.filter(task =>
                    task.status === 'Completed'
                );
                break;
            // 'all' case will use all tasks by default
        }

        // Update tasks display
        tasksDisplay.innerHTML = filteredTasks.length > 0
            ? filteredTasks.map(task => createTaskCard(task, currentUserId, project.id)).join('')
            : '<div class="no-tasks-message"><i class="bi bi-info-circle"></i> No tasks found for this filter.</div>';
    } catch (error) {
        console.error('Error filtering tasks:', error);
        showToast('Failed to filter tasks: ' + error.message, 'danger');
    }
}

// Helper function to get the correct button index
function getButtonIndex(filter) {
    switch (filter) {
        case 'all': return 1;
        case 'my': return 2;
        case 'active': return 3;
        case 'completed': return 4;
        default: return 1;
    }
}

async function showTaskDetails(taskId) {
    try {
        const response = await fetch(`/api/projectsapi/tasks/${taskId}`);
        if (!response.ok) {
            throw new Error('Failed to load task details');
        }

        const task = await response.json();

        // Get project members for the select dropdown
        const projectResponse = await fetch(`/api/projectsapi/${task.projectId}`);
        if (!projectResponse.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await projectResponse.json();

        // Update assigned users dropdown
        const assignedToSelect = document.getElementById('editTaskAssignedTo');
        assignedToSelect.innerHTML = project.projectMembers.map(member => `
                    <option value="${member.user.id}">
                        ${member.user.name} ${member.user.lastName} (${member.role})
                    </option>
                `).join('');

        // Populate form fields
        document.getElementById('editTaskTitle').value = task.taskTitle;
        document.getElementById('editTaskDescription').value = task.description || '';
        document.getElementById('editTaskDueDate').value = new Date(task.dueDate).toISOString().split('T')[0];
        document.getElementById('editTaskPriority').value = task.priority;
        document.getElementById('editTaskStatus').value = task.status;

        // Set multiple selected users
        const assignedUserIds = task.assignedUsers.map(u => u.id);
        Array.from(assignedToSelect.options).forEach(option => {
            option.selected = assignedUserIds.includes(option.value);
        });

        // Update modal footer
        const modalFooter = document.getElementById('taskModalFooter');
        modalFooter.innerHTML = `
                    <button type="button" class="btn btn-danger me-auto" onclick="deleteTask(${task.id}, ${task.projectId})">Delete Task</button>
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="updateTask(${task.id}, ${task.projectId})">Save Changes</button>
                `;

        // Show the modal
        const modal = new bootstrap.Modal(document.getElementById('taskDetailsModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading task details:', error);
        showToast(error.message, 'error');
    }
}

async function deleteTask(taskId, projectId) {
    try {
        // Prepare headers
        const headers = {
            'Content-Type': 'application/json'
        };
        
        // Add CSRF token to headers if it exists
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]');
        if (csrfToken) {
            headers['X-CSRF-TOKEN'] = csrfToken.value;
        }
        
        const response = await fetch(`/api/projectsapi/tasks/${taskId}`, {
            method: 'DELETE',
            headers: headers
        });
        
        if (!response.ok) {
            throw new Error('Failed to delete task');
        }
        
        // Close any open modals
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        if (modal) modal.hide();
        
        const confirmModal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmationModal'));
        if (confirmModal) confirmModal.hide();
        
        showToast('Task deleted successfully', 'success');
        
        // Reload the project to update the task list
        await loadProject(projectId);
    } catch (error) {
        console.error('Error deleting task:', error);
        showToast('Error deleting task: ' + error.message, 'error');
    }
}

async function updateTask(taskId, projectId) {
    try {
        // Get form values
        const taskTitle = document.getElementById('editTaskTitle').value.trim();
        const description = document.getElementById('editTaskDescription').value.trim();
        const dueDate = document.getElementById('editTaskDueDate').value;
        const priority = document.getElementById('editTaskPriority').value;
        const status = document.getElementById('editTaskStatus').value;
        
        // Get selected users from the multi-select
        const assignedToSelect = document.getElementById('editTaskAssignedTo');
        const assignedUserIds = Array.from(assignedToSelect.selectedOptions).map(option => option.value);
        
        if (!taskTitle || !dueDate || assignedUserIds.length === 0) {
            showToast('Please fill out all required fields and assign at least one user', 'error');
            return;
        }
        
        // Create data object matching your TaskUpdateDto
        // Include activityLogAdditions with additional data to prevent AdditionalData NULL errors
        const taskData = {
            taskTitle: taskTitle,
            description: description,
            dueDate: dueDate,
            priority: priority,
            status: status,
            assignedUserIds: assignedUserIds,
            activityLogAdditions: [
                { action: "updated task", targetType: "Task", additionalData: "{}" },
                { action: "assigned task", targetType: "Task", additionalData: "{}" },
                { action: "updated task status", targetType: "Task", additionalData: "{}" }
            ]
        };

        console.log('Updating task with data:', taskData);
        
        // Set button to loading state
        const updateButton = document.querySelector('#taskDetailsModal .btn-primary');
        if (updateButton) {
            updateButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
            updateButton.disabled = true;
        }
        
        const headers = {
            'Content-Type': 'application/json'
        };
        
        const response = await fetch(`/api/projectsapi/tasks/${taskId}`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(taskData)
        });
        
        // Try to get response text for better error handling
        let responseText = '';
        try {
            responseText = await response.text();
            console.log('Server response:', responseText);
        } catch (e) {
            console.error('Could not read response text:', e);
        }
        
        if (!response.ok) {
            throw new Error('Failed to update task: ' + responseText);
        }
        
        // Close modal and refresh tasks
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        if (modal) {
            modal.hide();
        } else {
            // Fallback if modal instance not found
            document.getElementById('taskDetailsModal').classList.remove('show');
            document.body.classList.remove('modal-open');
            document.querySelector('.modal-backdrop')?.remove();
        }
        
        showToast('Task updated successfully', 'success');
        
        // Reload the project to show the updated task
        await loadProject(projectId);
    } catch (error) {
        console.error('Error updating task:', error);
        showToast('Error updating task: ' + error.message, 'error');
    } finally {
        // Reset button state
        const updateButton = document.querySelector('#taskDetailsModal .btn-primary');
        if (updateButton) {
            updateButton.innerHTML = 'Save Changes';
            updateButton.disabled = false;
        }
    }
}

async function updateTaskStatus(taskId, projectId, newStatus, event) {
    // Prevent the click from propagating to parent elements
    if (event) {
        event.stopPropagation();
    }
    
    try {
        // Create a properly formatted additionalData JSON string
        // This ensures it will never be null in the database
        const additionalData = JSON.stringify({
            taskId: taskId,
            newStatus: newStatus,
            timestamp: new Date().toISOString()
        });
        
        // Prepare status update data
        const statusData = {
            status: newStatus,
            additionalData: additionalData
        };
        
        // Prepare headers for the fetch request
        const headers = {
            'Content-Type': 'application/json'
        };

        console.log('Updating task status with data:', statusData);

        const response = await fetch(`/api/projectsapi/tasks/${taskId}/status`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(statusData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Error updating task status:', errorText);
            
            // Reset checkbox state if updating fails
            if (event && event.target && event.target.type === 'checkbox') {
                event.target.checked = newStatus === 'Completed';
            }
            
            throw new Error('Failed to update task status');
        }

        // Reload the project to show updated task status
        await loadProject(projectId);
        showToast(`Task marked as ${newStatus}`, 'success');
    } catch (error) {
        console.error('Error updating task status:', error);
        showToast('Error updating task status: ' + error.message, 'error');
        
        // Reset checkbox state if updating fails
        if (event && event.target && event.target.type === 'checkbox') {
            event.target.checked = newStatus === 'Completed';
        }
    }
}

function toggleArchivedProjectsList() {
    const archivedProjectsList = document.getElementById('archivedProjectsList');
    const icon = event.currentTarget.querySelector('.bi-chevron-down, .bi-chevron-up');
    
    archivedProjectsList.classList.toggle('active');
    
    // Toggle icon
    if (icon) {
        if (icon.classList.contains('bi-chevron-down')) {
            icon.classList.remove('bi-chevron-down');
            icon.classList.add('bi-chevron-up');
            
            // Only load projects if the list is empty
            if (archivedProjectsList.children.length === 0) {
                loadArchivedProjects();
            }
        } else {
            icon.classList.remove('bi-chevron-up');
            icon.classList.add('bi-chevron-down');
        }
    }
}

async function loadArchivedProjects() {
    try {
        console.log('Loading archived projects...');
        const response = await fetch('/api/ProjectsApi/archived');
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Error loading archived projects');
        }
        
        const projects = await response.json();
        const archivedProjectsList = document.getElementById('archivedProjectsList');
        archivedProjectsList.innerHTML = '';
        
        if (projects.length === 0) {
            archivedProjectsList.innerHTML = '<div class="project-item text-muted">No archived projects</div>';
            return;
        }
        
        projects.forEach(project => {
            const projectItem = document.createElement('div');
            projectItem.className = 'project-item';
            projectItem.innerHTML = `
                <div onclick="loadProject(${project.id})" style="cursor: pointer;">
                    <i class="bi bi-file-earmark-text"></i>
                    <span>${project.name}</span>
                </div>
            `;
            archivedProjectsList.appendChild(projectItem);
        });
        
        console.log('Archived projects loaded successfully:', projects.length);
    } catch (error) {
        console.error('Failed to load archived projects:', error);
        showToast('Failed to load archived projects: ' + error.message, 'error');
    }
}

/**
 * Loads the archived project view with watermark and restricted functionality
 * @param {Object} project - The project data
 */
async function loadArchivedProjectView(project) {
    try {
        // Use a more appropriate approach to handle the archived view
        const projectContent = document.querySelector('.projects-content');
        
        // Get creator name - properly handle the case when createdBy might be missing
        let creatorName = 'Unknown';
        if (project.createdBy && project.createdBy.name) {
            creatorName = `${project.createdBy.name} ${project.createdBy.lastName || ''}`;
        } else if (project.projectMembers && project.projectMembers.length > 0) {
            // Try to find the project lead from members if creator is not available
            const projectLead = project.projectMembers.find(m => m.role === 'Project Lead');
            if (projectLead && projectLead.user) {
                creatorName = `${projectLead.user.name} ${projectLead.user.lastName || ''}`;
            }
        }
        
        // Use the current date as completion date since we don't have the actual date when it was completed
        // In a real app, you would store this in the database when the project is marked as completed
        const completionDate = project.updatedAt ? formatDate(project.updatedAt) : formatDate(new Date());
        
        projectContent.innerHTML = `
            <div class="project-details archived-project">
                <div class="archived-overlay">
                    <span>ARCHIVED</span>
                </div>
                
                <div class="d-flex justify-content-between align-items-center mb-4">
                    <div class="d-flex align-items-center gap-3">
                        <h2>${project.name}</h2>
                    </div>
                    <button class="btn btn-success" onclick="showRestoreProjectModal()">
                        <i class="bi bi-arrow-counterclockwise"></i> Restore Project
                    </button>
                </div>
                
                <div class="project-info mt-4">
                    <div class="row">
                        <div class="col-md-8">
                            <p class="description">${project.description || 'No description provided'}</p>
                        </div>
                        <div class="col-md-4">
                            <div class="info-card">
                                <p><strong>Status:</strong> Completed</p>
                                <p><strong>Priority:</strong> ${project.priority}</p>
                                <p><strong>Start Date:</strong> ${formatDate(project.startDate)}</p>
                                <p><strong>Original Deadline:</strong> ${formatDate(project.deadline)}</p>
                                <p><strong>Completion Date:</strong> ${completionDate}</p>
                                <p><strong>Created By:</strong> ${creatorName}</p>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="team-members mt-4">
                    <h5>Team Members</h5>
                    <div class="members-list" id="archivedProjectMembers">
                        <!-- Member cards will be inserted here -->
                    </div>
                </div>
                
                <div class="tasks-container mt-4">
                    <h5>Tasks</h5>
                    <div id="archivedProjectTasks">
                        <!-- Tasks will be inserted here -->
                    </div>
                </div>
            </div>
        `;
        
        // Set the project ID for restoration
        document.getElementById('restoreProjectId').value = project.id;
        
        // Populate team members
        const membersContainer = document.getElementById('archivedProjectMembers');
        membersContainer.innerHTML = '';
        
        if (project.projectMembers && project.projectMembers.length > 0) {
            project.projectMembers.forEach(member => {
                const memberCard = document.createElement('div');
                memberCard.className = 'member-card';
                
                // Handle potential missing data
                const userProfilePic = member.user && member.user.profilePicturePath ? 
                    member.user.profilePicturePath : '/images/default/default-profile.png';
                const userName = member.user ? 
                    `${member.user.name || ''} ${member.user.lastName || ''}` : 'Unknown User';
                
                memberCard.innerHTML = `
                    <img src="${userProfilePic}" alt="${userName}" class="member-avatar" onerror="this.src='/images/default/default-profile.png'">
                    <div class="member-info">
                        <p class="member-name">${userName}</p>
                        <p class="member-role">${member.role || 'Member'}</p>
                    </div>
                `;
                membersContainer.appendChild(memberCard);
            });
        } else {
            membersContainer.innerHTML = '<p class="text-muted">No team members</p>';
        }
        
        // Populate tasks
        const tasksContainer = document.getElementById('archivedProjectTasks');
        tasksContainer.innerHTML = '';
        
        if (project.tasks && project.tasks.length > 0) {
            project.tasks.forEach(task => {
                const taskCard = document.createElement('div');
                taskCard.className = `task-card ${task.priority ? task.priority.toLowerCase() : 'medium'} ${task.status === 'Completed' ? 'completed' : ''}`;
                
                // Generate assigned users HTML
                let assignedUsersHtml = '';
                if (task.assignments && task.assignments.length > 0) {
                    assignedUsersHtml = `
                        <div class="assigned-users">
                            ${task.assignments.map(assignment => {
                                const userName = assignment.user ? assignment.user.name : 'Unknown';
                                const userPic = assignment.user && assignment.user.profilePicturePath ? 
                                    assignment.user.profilePicturePath : '/images/default/default-profile.png';
                                
                                return `
                                <div class="assigned-user">
                                    <img src="${userPic}" 
                                         alt="${userName}" 
                                         class="user-avatar"
                                         onerror="this.src='/images/default/default-profile.png'">
                                    <span class="user-name">${userName}</span>
                                </div>
                            `}).join('')}
                        </div>
                    `;
                } else {
                    assignedUsersHtml = '<span class="text-muted">Unassigned</span>';
                }
                
                // Handle task titles - they could be in different properties depending on the API
                const taskTitle = task.title || task.taskTitle || 'Untitled Task';
                
                taskCard.innerHTML = `
                    <div class="task-header">
                        <h5 class="task-title">${taskTitle}</h5>
                        <span class="task-priority badge ${task.priority ? task.priority.toLowerCase() : 'medium'}">${task.priority || 'Medium'}</span>
                    </div>
                    <p class="task-description">${task.description || 'No description'}</p>
                    <div class="task-meta">
                        <div>
                            <small>Due: ${formatDate(task.dueDate)}</small>
                        </div>
                        <div class="assigned-to">
                            <small>Assigned to:</small>
                            ${assignedUsersHtml}
                        </div>
                    </div>
                `;
                
                tasksContainer.appendChild(taskCard);
            });
        } else {
            tasksContainer.innerHTML = '<p class="text-muted">No tasks</p>';
        }
    } catch (error) {
        console.error('Error loading archived project view:', error);
        showToast('Failed to load archived project: ' + error.message, 'error');
    }
}

/**
 * Format date to a readable string
 * @param {string} dateString - ISO date string
 * @returns {string} Formatted date
 */
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString();
}

/**
 * Show the restore project modal
 */
function showRestoreProjectModal() {
    // Set default deadline to 2 weeks from now
    const twoWeeksFromNow = new Date();
    twoWeeksFromNow.setDate(twoWeeksFromNow.getDate() + 14);
    
    const formattedDate = twoWeeksFromNow.toISOString().split('T')[0];
    document.getElementById('restoreProjectDeadline').value = formattedDate;
    
    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('restoreProjectModal'));
    modal.show();
}

/**
 * Restore an archived project to active status
 */
async function restoreProject() {
    try {
        const projectId = document.getElementById('restoreProjectId').value;
        const newDeadline = document.getElementById('restoreProjectDeadline').value;
        const newStatus = document.getElementById('restoreProjectStatus').value;
        const newPriority = document.getElementById('restoreProjectPriority').value;
        
        if (!projectId || !newDeadline) {
            showToast('Please fill in all required fields', 'error');
            return;
        }
        
        const projectResponse = await fetch(`/api/projectsapi/${projectId}`);
        if (!projectResponse.ok) {
            throw new Error('Failed to load project details');
        }
        const currentProject = await projectResponse.json();
        
        // Update project with new data
        const updateData = {
            name: currentProject.name,
            description: currentProject.description,
            startDate: currentProject.startDate,
            deadline: newDeadline,
            status: newStatus,
            priority: newPriority
        };
        
        const response = await fetch(`/api/projectsapi/${projectId}/update`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(updateData)
        });
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.title || 'Failed to restore project');
        }
        
        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('restoreProjectModal'));
        if (modal) {
            modal.hide();
        }
        
        showToast(`Project restored with new deadline: ${formatDate(newDeadline)}`, 'success');
        
        // Refresh both project lists and reload project view
        await loadProjects();
        await loadArchivedProjects();
        
        // Update project count badges
        await updateProjectCountBadges();
        
        await loadProject(projectId);
    } catch (error) {
        console.error('Error restoring project:', error);
        showToast(error.message, 'error');
    }
}

/**
 * Updates the project count badges in the sidebar
 */
async function updateProjectCountBadges() {
    try {
        // Get the current counts from the server
        const response = await fetch('/api/ProjectsApi/counts');
        if (!response.ok) {
            throw new Error('Failed to get project counts');
        }
        
        const counts = await response.json();
        
        // Update the badges in the sidebar
        const activeBadge = document.querySelector('#projectsList').closest('.projects-dropdown').querySelector('.badge');
        const archivedBadge = document.querySelector('#archivedProjectsList').closest('.projects-dropdown').querySelector('.badge');
        
        if (activeBadge) {
            if (counts.active > 0) {
                activeBadge.textContent = counts.active;
                activeBadge.classList.remove('d-none');
            } else {
                activeBadge.classList.add('d-none');
            }
        }
        
        if (archivedBadge) {
            if (counts.archived > 0) {
                archivedBadge.textContent = counts.archived;
                archivedBadge.classList.remove('d-none');
            } else {
                archivedBadge.classList.add('d-none');
            }
        }
    } catch (error) {
        console.error('Error updating project count badges:', error);
        // Don't show a toast for this error as it's not critical
    }
}

// New functions for Activity Feed

/**
 * Loads the activity feed for a project
 * @param {number} projectId - The ID of the project
 */
async function loadActivityFeed(projectId) {
    try {
        const activityFeed = document.getElementById('activityFeed');
        activityFeed.innerHTML = `
            <div class="activity-feed">
                <div class="activity-feed-header">
                    <h5><i class="bi bi-activity"></i> Recent Activity</h5>
                </div>
                <div class="activity-feed-content" id="activityFeedContent">
                    <div class="text-center my-3">
                        <div class="spinner-border spinner-border-sm text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        const response = await fetch(`/api/projectsapi/${projectId}/activities?limit=15`);
        if (!response.ok) throw new Error('Failed to load activity feed');
        
        const data = await response.json();
        if (!data.success) throw new Error(data.message || 'Failed to load activity feed');
        
        const activities = data.activities;
        const activityFeedContent = document.getElementById('activityFeedContent');
        
        if (activities.length === 0) {
            activityFeedContent.innerHTML = `
                <div class="text-center text-muted py-3">
                    <i class="bi bi-info-circle"></i> No activity found for this project.
                </div>
            `;
            return;
        }
        
        activityFeedContent.innerHTML = `
            <div class="activity-list">
                ${activities.map(activity => createActivityItem(activity)).join('')}
            </div>
        `;
    } catch (error) {
        console.error('Error loading activity feed:', error);
        const activityFeedContent = document.getElementById('activityFeedContent');
        if (activityFeedContent) {
            activityFeedContent.innerHTML = `
                <div class="text-center text-danger py-3">
                    <i class="bi bi-exclamation-triangle"></i> Failed to load activity feed.
                </div>
            `;
        }
    }
}

/**
 * Creates an HTML item for an activity
 * @param {Object} activity - The activity data
 * @returns {string} HTML string for the activity item
 */
function createActivityItem(activity) {
    const timeAgo = formatTimeAgo(new Date(activity.timestamp));
    
    let icon = '';
    let content = '';
    
    // Always use the activity description when it exists
    if (activity.description) {
        switch (activity.type) {
            case 'task_created':
                icon = '<i class="bi bi-plus-circle activity-icon task-created"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'task_assigned':
                icon = '<i class="bi bi-person-check activity-icon task-assigned"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'task_completed':
                icon = '<i class="bi bi-check-circle activity-icon task-completed"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'task_updated':
                icon = '<i class="bi bi-pencil activity-icon task-updated"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'member_joined':
                icon = '<i class="bi bi-person-plus activity-icon member-joined"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'project_created':
                icon = '<i class="bi bi-folder-plus activity-icon project-created"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            case 'project_updated':
                icon = '<i class="bi bi-pencil-square activity-icon project-updated"></i>';
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
                break;
            default:
                // For any activity with a description but unknown type, still show the description
                if (activity.targetType === 'Project') {
                    icon = '<i class="bi bi-pencil-square activity-icon project-updated"></i>';
                } else if (activity.targetType === 'Task') {
                    icon = '<i class="bi bi-pencil activity-icon task-updated"></i>';
                } else if (activity.targetType === 'Member') {
                    icon = '<i class="bi bi-person-plus activity-icon member-joined"></i>';
                } else {
                    icon = '<i class="bi bi-info-circle activity-icon"></i>';
                }
                content = `<strong>${activity.userName}</strong> ${activity.description}`;
        }
    } else {
        // Fallback for activities without a description
        icon = '<i class="bi bi-info-circle activity-icon"></i>';
        content = `<strong>${activity.userName}</strong> performed an activity`;
    }
    
    return `
        <div class="activity-item">
            <div class="activity-avatar">
                <img src="${activity.userAvatar}" alt="${activity.userName}" 
                     onerror="this.onerror=null; this.src='/images/default-avatar.png';">
            </div>
            <div class="activity-content">
                <div class="activity-icon-container">
                    ${icon}
                </div>
                <div class="activity-text">
                    <p>${content}</p>
                    <small class="text-muted">${timeAgo}</small>
                </div>
            </div>
        </div>
    `;
}

/**
 * Formats a date to a "time ago" string (e.g., "2 hours ago")
 * @param {Date} date - The date to format
 * @returns {string} Formatted "time ago" string
 */
function formatTimeAgo(date) {
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);
    
    if (diffInSeconds < 60) {
        return 'just now';
    }
    
    const diffInMinutes = Math.floor(diffInSeconds / 60);
    if (diffInMinutes < 60) {
        return `${diffInMinutes} ${diffInMinutes === 1 ? 'minute' : 'minutes'} ago`;
    }
    
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) {
        return `${diffInHours} ${diffInHours === 1 ? 'hour' : 'hours'} ago`;
    }
    
    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 30) {
        return `${diffInDays} ${diffInDays === 1 ? 'day' : 'days'} ago`;
    }
    
    const diffInMonths = Math.floor(diffInDays / 30);
    if (diffInMonths < 12) {
        return `${diffInMonths} ${diffInMonths === 1 ? 'month' : 'months'} ago`;
    }
    
    const diffInYears = Math.floor(diffInMonths / 12);
    return `${diffInYears} ${diffInYears === 1 ? 'year' : 'years'} ago`;
}

/**
 * Gets the appropriate badge color for a project status
 * @param {string} status - The project status
 * @returns {string} The badge color class name
 */
function getStatusBadgeColor(status) {
    switch (status) {
        case 'Active': return 'success';
        case 'On Hold': return 'warning';
        case 'Completed': return 'secondary';
        case 'Cancelled': return 'danger';
        default: return 'primary';
    }
}

/**
 * Gets the appropriate badge color for a priority level
 * @param {string} priority - The priority level
 * @returns {string} The badge color class name
 */
function getPriorityBadgeColor(priority) {
    switch (priority) {
        case 'Low': return 'success';
        case 'Medium': return 'info';
        case 'High': return 'warning';
        case 'Urgent': return 'danger';
        default: return 'secondary';
    }
}
