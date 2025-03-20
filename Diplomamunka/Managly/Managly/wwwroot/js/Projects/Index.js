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
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        const projectContent = document.querySelector('.projects-content');
        const currentUserId = project.currentUserId;

        // Check if project is archived (completed)
        if (project.status === "Completed") {
            // Load archived project view
            await loadArchivedProjectView(project);
            return;
        }

        // Load regular project view - existing code
        projectContent.innerHTML = `
                    <div class="project-details">
                        <div class="d-flex justify-content-between align-items-center mb-4">
                            <div class="d-flex align-items-center gap-3">
                                <h2>${project.name}</h2>
                            </div>
                            ${project.isProjectLead ? `
                                <div class="d-flex gap-2">
                                    <button class="btn btn-primary" onclick="showAddMembersModal(${project.id})">
                                        <i class="bi bi-person-plus"></i> Add Members
                                    </button>
                                    <button class="btn btn-danger" onclick="showDeleteConfirmation(${project.id})">
                                        <i class="bi bi-trash"></i> Delete Project
                                    </button>
                                </div>
                            ` : ''}
                        </div>
                        <div class="project-info mt-4">
                            <div class="row">
                                <div class="col-md-8">
                                    <p class="description">${project.description || 'No description provided'}</p>
                                </div>
                                <div class="col-md-4">
                                    <div class="info-card">
                                        ${project.isProjectLead ? `
                                        <button class="btn btn-link text-primary p-0" onclick="showEditProjectModal(${project.id})">
                                            <i class="bi bi-pencil-square fs-5"></i>
                                        </button>
                                    ` : ''}
                                        <p><strong>Status:</strong> ${project.status}</p>
                                        <p><strong>Priority:</strong> ${project.priority}</p>
                                        <p><strong>Start Date:</strong> ${project.startDate}</p>
                                        <p><strong>Deadline:</strong> ${project.deadline}</p>
                                        <p><strong>Progress:</strong> ${project.totalTasks > 0
                ? Math.round((project.completedTasks / project.totalTasks) * 100)
                : 0}%</p>
                                    </div>
                                </div>
                            </div>
                            <div class="team-members mt-4">
                                <div class="d-flex justify-content-between align-items-center mb-3">
                                    <h4>Team Members</h4>
                                    ${project.isProjectLead ? `
                                        <button class="btn btn-outline-primary btn-sm" onclick="showManageMembersModal(${project.id})">
                                            <i class="bi bi-gear"></i> Manage Members
                                        </button>
                                    ` : ''}
                                </div>
                                <div class="members-list">
                                    ${project.projectMembers.map(member => `
                                        <div class="member-card">
                                            <img src="${member.user.profilePicturePath}" alt="Profile" class="member-avatar">
                                            <div class="member-info">
                                                <p class="member-name">${member.user.name} ${member.user.lastName}</p>
                                                <p class="member-role">${member.role}</p>
                                            </div>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                            <div class="tasks-section mt-4">
                                <div class="d-flex justify-content-between align-items-center mb-3">
                                    <h4>Tasks</h4>
                                    ${project.isProjectLead || project.projectMembers.some(m => m.role === 'Manager' && m.user.id === currentUserId) ? `
                                        <button class="btn btn-primary btn-sm" onclick="showCreateTaskModal(${project.id})">
                                            <i class="bi bi-plus-lg"></i> Create Task
                                        </button>
                                    ` : ''}
                                </div>
                                <div class="tasks-container">
                                    <div class="tasks-filters mb-3">
                                        <div class="btn-group" role="group" aria-label="Task filters">
                                            <button class="btn btn-outline-secondary active" onclick="filterTasks(${project.id}, 'all')">All</button>
                                            <button class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'my')">My Tasks</button>
                                            <button class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'active')">Active</button>
                                            <button class="btn btn-outline-secondary" onclick="filterTasks(${project.id}, 'completed')">Completed</button>
                                        </div>
                                    </div>
                                    <div class="tasks-list">
                                        ${project.tasks ? project.tasks.map(task => createTaskCard(task, currentUserId, project.id)).join('') : '<p>No tasks yet</p>'}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                `;
    } catch (error) {
        console.error('Error loading project:', error);
        showToast('Failed to load project: ' + error.message, 'error');
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
                                <div class="form-check form-switch">
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
                                        <img src="${user.profilePicturePath}" alt="${user.name}" class="user-avatar">
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
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        // Populate form fields
        document.getElementById('editProjectName').value = project.name;
        document.getElementById('editProjectDescription').value = project.description || '';
        document.getElementById('editStartDate').value = new Date(project.startDate).toISOString().split('T')[0];
        document.getElementById('editDeadline').value = new Date(project.deadline).toISOString().split('T')[0];
        document.getElementById('editPriority').value = project.priority;
        document.getElementById('editStatus').value = project.status;

        // Update the Save Changes button to include projectId
        const updateButton = document.getElementById('updateProjectButton');
        updateButton.onclick = () => updateProject(projectId);

        // Show the modal
        const modal = new bootstrap.Modal(document.getElementById('editProjectModal'));
        modal.show();
    } catch (error) {
        console.error('Error showing edit project modal:', error);
        showToast(error.message, 'error');
    }
}

async function updateProject(projectId) {
    try {
        const projectResponse = await fetch(`/api/projectsapi/${projectId}`);
        if (!projectResponse.ok) {
            throw new Error('Failed to load project details');
        }
        const currentProject = await projectResponse.json();

        const newStatus = document.getElementById('editStatus').value;
        const wasCompletionStatusChanged = currentProject.status !== "Completed" && newStatus === "Completed";
        
        // Set completion date if project is being marked as completed
        const completionDate = wasCompletionStatusChanged ? new Date().toISOString() : null;

        const projectData = {
            id: projectId,
            name: document.getElementById('editProjectName').value,
            description: document.getElementById('editProjectDescription').value || '',
            startDate: document.getElementById('editStartDate').value,
            deadline: document.getElementById('editDeadline').value,
            priority: document.getElementById('editPriority').value,
            status: newStatus,
            completedAt: completionDate,
            companyId: currentProject.companyId,
            company: currentProject.company,
            createdById: currentProject.createdById,
            createdBy: currentProject.createdBy,
            projectMembers: currentProject.projectMembers,
            totalTasks: currentProject.totalTasks || 0,
            completedTasks: currentProject.completedTasks || 0
        };

        console.log("Updating project with data:", JSON.stringify(projectData));

        const response = await fetch(`/api/projectsapi/${projectId}/update`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({
                name: projectData.name,
                description: projectData.description,
                startDate: projectData.startDate,
                deadline: projectData.deadline,
                status: projectData.status,
                priority: projectData.priority,
                completedAt: projectData.completedAt
            })
        });

        if (!response.ok) {
            const errorData = await response.json();
            console.error("Server response:", errorData);
            throw new Error(errorData.title || 'Failed to update project');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('editProjectModal'));
        if (modal) {
            modal.hide();
        }

        showToast('Project updated successfully', 'success');

        // Refresh the view
        await loadProject(projectId);
        await loadProjects();

        // Check if status changed to Completed, refresh sidebar if needed
        if (wasCompletionStatusChanged) {
            // Refresh both project lists
            loadProjects();
            loadArchivedProjects();
            
            // Update project count badges
            await updateProjectCountBadges();
            
            // Show a special message
            showToast('Project marked as completed and moved to archives', 'success');
        }
    } catch (error) {
        console.error('Error updating project:', error);
        showToast(error.message, 'error');
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
        // Get project members for the select dropdown
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        // Update assigned users dropdown
        const assignedToSelect = document.getElementById('taskAssignedTo');
        assignedToSelect.innerHTML = project.projectMembers.map(member => `
                    <option value="${member.user.id}">
                        ${member.user.name} ${member.user.lastName} (${member.role})
                    </option>
                `).join('');

        // Set minimum date to today
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('taskDueDate').min = today;
        document.getElementById('taskDueDate').value = today;

        // Update the Create Task button to include the projectId
        const createButton = document.querySelector('#createTaskModal .btn-primary');
        createButton.setAttribute('onclick', `createTask(${projectId})`);

        // Show the modal
        const modal = new bootstrap.Modal(document.getElementById('createTaskModal'));
        modal.show();
    } catch (error) {
        console.error('Error showing create task modal:', error);
        showToast(error.message, 'error');
    }
}

async function createTask(projectId) {
    try {
        const assignedToSelect = document.getElementById('taskAssignedTo');
        const selectedUsers = Array.from(assignedToSelect.selectedOptions).map(option => option.value);

        const taskData = {
            taskTitle: document.getElementById('taskTitle').value,
            description: document.getElementById('taskDescription').value || '',
            dueDate: document.getElementById('taskDueDate').value,
            priority: document.getElementById('taskPriority').value,
            assignedToId: selectedUsers[0] // Take the first selected user as assignedToId
        };

        console.log('Creating task with data:', taskData);

        const response = await fetch(`/api/projectsapi/${projectId}/tasks`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(taskData)
        });

        if (!response.ok) {
            const errorData = await response.json();
            console.error('Server response:', errorData);
            throw new Error(errorData.error || 'Failed to create task');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('createTaskModal'));
        modal.hide();

        // Clear form
        document.getElementById('taskTitle').value = '';
        document.getElementById('taskDescription').value = '';
        document.getElementById('taskDueDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('taskPriority').value = 'Medium';
        assignedToSelect.selectedIndex = -1;

        // Show success message
        showToast('Task created successfully', 'success');

        // Refresh project details
        await loadProject(projectId);
    } catch (error) {
        console.error('Error creating task:', error);
        showToast(error.message, 'error');
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

        const tasksList = document.querySelector('.tasks-list');
        const currentUserId = project.currentUserId;

        // Update button states
        const buttons = document.querySelectorAll('.tasks-filters .btn');
        buttons.forEach(btn => btn.classList.remove('active'));
        const activeButton = document.querySelector(`.tasks-filters .btn:nth-child(${getButtonIndex(filter)})`);
        activeButton.classList.add('active');

        // Filter tasks
        let filteredTasks = project.tasks;

        switch (filter) {
            case 'my':
                filteredTasks = project.tasks.filter(task =>
                    task.assignedUsers.some(u => u.id === currentUserId)
                );
                break;
            case 'active':
                filteredTasks = project.tasks.filter(task =>
                    task.status !== 'Completed'
                );
                break;
            case 'completed':
                filteredTasks = project.tasks.filter(task =>
                    task.status === 'Completed'
                );
                break;
            // 'all' case will use all tasks by default
        }

        // Update tasks list
        tasksList.innerHTML = filteredTasks.length > 0
            ? filteredTasks.map(task => createTaskCard(task, currentUserId, project.id)).join('')
            : '<p>No tasks found</p>';
    } catch (error) {
        console.error('Error filtering tasks:', error);
        showToast(error.message, 'error');
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
    if (!confirm('Are you sure you want to delete this task?')) {
        return;
    }

    try {
        const response = await fetch(`/api/projectsapi/tasks/${taskId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to delete task');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        modal.hide();

        // Show success message
        showToast('Task deleted successfully', 'success');

        // Refresh project details
        await loadProject(projectId);
    } catch (error) {
        console.error('Error deleting task:', error);
        showToast(error.message, 'error');
    }
}

async function updateTask(taskId, projectId) {
    try {
        const assignedToSelect = document.getElementById('editTaskAssignedTo');
        const selectedUsers = Array.from(assignedToSelect.selectedOptions).map(option => option.value);

        const taskData = {
            taskTitle: document.getElementById('editTaskTitle').value,
            description: document.getElementById('editTaskDescription').value || '',
            dueDate: document.getElementById('editTaskDueDate').value,
            priority: document.getElementById('editTaskPriority').value,
            status: document.getElementById('editTaskStatus').value,
            assignedUserIds: selectedUsers
        };

        const response = await fetch(`/api/projectsapi/tasks/${taskId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(taskData)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to update task');
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        modal.hide();

        // Show success message
        showToast('Task updated successfully', 'success');

        // Refresh project details
        await loadProject(projectId);
    } catch (error) {
        console.error('Error updating task:', error);
        showToast(error.message, 'error');
    }
}

async function updateTaskStatus(taskId, projectId, newStatus, event) {
    // Stop the click event from bubbling up to the task card
    event.stopPropagation();

    try {
        const response = await fetch(`/api/projectsapi/tasks/${taskId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status: newStatus })
        });

        if (!response.ok) {
            throw new Error('Failed to update task status');
        }

        showToast('Task status updated successfully', 'success');
        await loadProject(projectId); // Refresh project view
    } catch (error) {
        console.error('Error updating task status:', error);
        showToast(error.message, 'error');
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
    if (!dateString) return 'Not set';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    } catch (error) {
        console.error('Error formatting date:', error);
        return 'Invalid date';
    }
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
