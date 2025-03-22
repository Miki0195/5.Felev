// Everything for the sidebar
function searchProjects(searchTerm) {
    const activeFilter = document.querySelector('.filter-tab.active').getAttribute('data-filter');

    loadProjectsForSidebar(activeFilter, searchTerm);
}

function filterProjectsInSidebar(filter) {
    document.querySelectorAll('.filter-tab').forEach(tab => {
        if (tab.getAttribute('data-filter') === filter) {
            tab.classList.add('active');
        } else {
            tab.classList.remove('active');
        }
    });

    const searchTerm = document.getElementById('project-search-input').value;

    loadProjectsForSidebar(filter, searchTerm);
}

async function loadProjectsForSidebar(filter = 'all', searchTerm = '') {
    const projectsContainer = document.getElementById('projects-list');
    const emptyState = document.getElementById('projects-empty-state');

    if (!projectsContainer) {
        console.error("Projects container not found");
        return;
    }

    try {
        projectsContainer.innerHTML = `
            <div class="loading-state">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p>Loading projects...</p>
            </div>`;

        if (emptyState) emptyState.classList.add('d-none');

        const response = await fetch(`/Projects/sidebarPartial?filter=${filter}&search=${searchTerm}`);
        if (!response.ok) throw new Error(`API returned ${response.status}: ${await response.text()}`);
        const html = await response.text();

        if (html.trim() === '' || html.includes('No projects found')) {
            projectsContainer.innerHTML = ''; 

            if (emptyState) {
                emptyState.classList.remove('d-none');
                if (searchTerm) {
                    emptyState.querySelector('p').innerHTML = `
                        <i class="bi bi-search me-2"></i>
                        No projects found matching "${searchTerm}"
                    `;
                } else {
                    emptyState.querySelector('p').innerHTML = `
                        <i class="bi bi-folder-x me-2"></i>
                        No projects found
                    `;
                }
            }
        } else {
            projectsContainer.innerHTML = html;

            if (emptyState) emptyState.classList.add('d-none');

            document.querySelectorAll('.project-card').forEach(card => {
                card.addEventListener('click', () => loadProject(card.dataset.projectId));
            });
        }
    } catch (error) {
        console.error('Error loading projects:', error);
        projectsContainer.innerHTML = `
            <div class="error-message">
                <p>Error loading projects: ${error.message}</p>
                <button class="btn btn-sm btn-primary" onclick="loadProjectsForSidebar('${filter}', '${searchTerm}')">Retry</button>
            </div>`;
    }
}

let selectedUsers = new Set(); 
let currentProjectMembers = new Set(); 
let allUsers = [];

// Searching for users, this should be used when assigning users to tasks!!!!!!
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
        showToast('Error loading users', 'error');
    }
}

async function loadAvailableUsers(projectId = null) {
    try {
        const response = await fetch('/api/projectsapi/company-users');
        allUsers = await response.json();

        if (projectId) {
            const projectResponse = await fetch(`/api/projectsapi/${projectId}`);
            const project = await projectResponse.json();
            currentProjectMembers = new Set(project.projectMembers.map(member => member.user.id));
        } else {
            currentProjectMembers.clear();
        }
    } catch (error) {
        showToast('Error loading users', 'error');
    }
}

function handleUserSearch(searchTerm, resultsId) {
    const resultsContainer = document.getElementById(resultsId);
    if (!resultsContainer) return;

    if (!searchTerm) {
        resultsContainer.innerHTML = '';
        resultsContainer.classList.remove('active');
        return;
    }

    const filteredUsers = allUsers.filter(user =>
        !selectedUsers.has(user.id) && 
        !currentProjectMembers.has(user.id) && 
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

        const possibleContainers = [
            'createProjectSearchResults',
            'addMembersSearchResults',
            'createProjectSearchContainer-results',
            'addMembersSearchContainer-results'
        ];

        let resultsContainer = null;
        for (const containerId of possibleContainers) {
            const container = document.getElementById(containerId);
            if (container && container.classList.contains('active')) {
                resultsContainer = container;
                break;
            }
        }

        if (resultsContainer) {
            resultsContainer.innerHTML = '';
            resultsContainer.classList.remove('active');
        }

        
        const searchInput = document.querySelector('.user-search-input');
        if (searchInput) {
            searchInput.value = '';
        }

        refreshSelectedUsersDisplay();
    }
}

function removeSelectedUser(userId) {
    selectedUsers.delete(userId);
    refreshSelectedUsersDisplay();
}

function refreshSelectedUsersDisplay() {
    const possibleContainers = [
        'createProjectSearchContainer-selected',
        'addMembersSearchContainer-selected'
    ];

    let selectedContainer = null;
    for (const containerId of possibleContainers) {
        const container = document.getElementById(containerId);
        if (container) {
            selectedContainer = container;
            break;
        }
    }

    if (!selectedContainer) return;

    if (selectedUsers.size === 0) {
        selectedContainer.innerHTML = '<div class="no-users-selected">No team members selected</div>';
        return;
    }

    const selectedUsersHTML = Array.from(selectedUsers)
        .map(userId => {
            const user = allUsers.find(u => u.id === userId);
            if (!user) return '';

            return createSelectedUserItem(user);
        })
        .join('');

    selectedContainer.innerHTML = selectedUsersHTML;
}

// For creating projects
async function openCreateProjectModal() {
    const contentArea = document.getElementById('projectsContent');
    if (!contentArea) return;

    try {
    contentArea.innerHTML = `
            <div class="loading-indicator text-center p-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                        </div>
                <p class="mt-2">Loading form...</p>
                </div>
            `;

        selectedUsers.clear();

        const response = await fetch('/Projects/createModal');
        if (!response.ok) throw new Error('Failed to load create project form');

        const html = await response.text();

        contentArea.innerHTML = html;

        const startDateInput = document.getElementById('startDate');
        const deadlineInput = document.getElementById('deadline');

        if (startDateInput && deadlineInput) {
            const today = new Date().toISOString().split('T')[0];
            startDateInput.min = today;
            deadlineInput.min = today;

            startDateInput.addEventListener('change', function () {
                deadlineInput.min = this.value;
                if (deadlineInput.value && deadlineInput.value < this.value) {
                    deadlineInput.value = this.value;
                }
            });
        }

        const searchContainer = document.getElementById('createProjectSearchContainer');
        if (searchContainer) {
            await loadUserSearch('createProjectSearchContainer');
            await loadAvailableUsers(); 
        }

        const form = document.getElementById('createProjectForm');
        if (form) {
            form.addEventListener('submit', function (e) {
    e.preventDefault();
                handleCreateProject();
            });
        }
    } catch (error) {
        showToast('Error loading create project form', 'error');
    }
}

async function handleCreateProject() {
    const form = document.getElementById('createProjectForm');
    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    const submitBtn = form.querySelector('button[type="submit"]');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Creating...';

    try {
        const projectData = {
            name: document.getElementById('projectName').value,
            description: document.getElementById('projectDescription').value || '',
            startDate: document.getElementById('startDate').value,
            deadline: document.getElementById('deadline').value,
            priority: document.getElementById('priority').value,
            status: document.getElementById('status').value,
            teamMemberIds: Array.from(selectedUsers)
        };

        const response = await fetch('/api/projectsapi/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(projectData)
        });

        let project;
        try {
            project = await response.json();
        } catch (parseError) {
            showToast('There was an error creating the project!', 'warning');
        }

        showToast('Project created successfully!', 'success');

        loadProjectsForSidebar();

        if (project && project.id) {
            loadProject(project.id);
        } else {
        showWelcomeMessage();
        }
    } catch (error) {
        showToast(error.message || 'Error creating project', 'error');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    }
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
// Load projects on page load
document.addEventListener('DOMContentLoaded', () => {
    loadProjectsForSidebar();

    const filterTabsContainer = document.querySelector('.filter-tabs');
    if (filterTabsContainer) {
        filterTabsContainer.addEventListener('click', (event) => {
            const filterTab = event.target.closest('.filter-tab');
            if (!filterTab) return;

            document.querySelectorAll('.filter-tab').forEach(tab => {
                tab.classList.remove('active');
            });
            filterTab.classList.add('active');

            const filter = filterTab.dataset.filter;
            filterProjectsInSidebar(filter);
        });
    }

    const searchInput = document.getElementById('project-search-input');
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            searchProjects(this.value);
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                console.log(`Search input Enter pressed: "${this.value}"`);
                searchProjects(this.value);
            }
        });
    } else {
        console.error("Search input element not found. Looking for #project-search-input");
    }

    const createButton = document.getElementById('btn-create-project');
    if (createButton) {
        createButton.addEventListener('click', openCreateProjectModal, { once: false });
    }
});
//LOADING THE SELECTED PROJECTS

async function loadProject(projectId) {
    const projectContent = document.querySelector('.projects-content');
    projectContent.innerHTML = `
        <div class="d-flex justify-content-center align-items-center" style="height: 100%">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `;
    
    try {
        const projectCards = document.querySelectorAll('.project-card');
        projectCards.forEach(card => card.classList.remove('active', 'selected'));
        const selectedCard = document.querySelector(`.project-card[data-project-id="${projectId}"]`);
        if (selectedCard) {
            selectedCard.classList.add('active', 'selected');
        }
        
        const checkResponse = await fetch(`/api/ProjectsApi/${projectId}`);
        
        if (!checkResponse.ok) throw new Error(`Failed to check project: ${checkResponse.status}`);
        
        const projectData = await checkResponse.json();
        
        let endpoint = `/Projects/${projectId}`;
        if (projectData.status === "Completed") {
            endpoint = `/Projects/archived/${projectId}`;
        }
        
        const projectHtml = await fetch(endpoint, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'  
            }
        });
        
        if (!projectHtml.ok) throw new Error(`Failed to load project: ${projectHtml.status}${errorText ? " - " + errorText : ""}`);
        
        const html = await projectHtml.text();
        projectContent.innerHTML = html;
        
        history.pushState({projectId}, "", `/Projects#${projectId}`);
        
        const filterButtons = document.querySelectorAll('.tasks-filters .btn');
        filterButtons.forEach(button => {
            button.addEventListener('click', function() {
                filterButtons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');
            });
        });
        
        setupProjectEventListeners(projectId);
    } catch (error) {
        showToast('There was an error loading the project.', 'danger')
    }
}

async function filterTasks(projectId, filter) {
    try {
        const tasksDisplay = document.getElementById('tasksDisplay');

        const response = await fetch(`/Projects/api/tasks/${projectId}?filter=${filter}`);
        if (!response.ok) throw new Error('Failed to filter tasks');

        const html = await response.text();
        tasksDisplay.innerHTML = html;

    } catch (error) {
        showToast('Failed to load tasks: ' + error.message, 'danger');
    }
}

async function updateTaskStatus(taskId, projectId, newStatus, event) {
    event.stopPropagation();

    try {
        const response = await fetch(`/api/projectsapi/tasks/${taskId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status: newStatus })
        });

        if (!response.ok) throw new Error('Failed to update task status');

        const activeFilterButton = document.querySelector('.tasks-filters .btn-group .btn.active');
        const filter = activeFilterButton ? activeFilterButton.textContent.trim().toLowerCase() : 'all';

        filterTasks(projectId, filter);

    } catch (error) {
        showToast('Failed to update task status: ' + error.message, 'danger');
        event.target.checked = (newStatus === 'Completed');
    }
}

async function refreshActivityFeed(projectId, showLoadingIndicator = true) {
    try {
        const activityFeedContainer = document.getElementById('activityFeedContainer');
        if (!activityFeedContainer) {
            return;
        }

        const response = await fetch(`/Projects/${projectId}/ActivityFeed`);
        if (!response.ok) throw new Error(`Failed to refresh activity feed: ${response.status}`);

        const html = await response.text();
        activityFeedContainer.innerHTML = html;


        const feedContent = document.querySelector('.activity-feed-content');
        if (feedContent) {
            feedContent.addEventListener('scroll', function() {
                if (this.scrollTop > 10) {
                    this.classList.add('scrolled-down');
                } else {
                    this.classList.remove('scrolled-down');
                }
            });
        }

    } catch (error) {
        showToast("Error loadin activity feed: " + error, 'danger');
    }
}

function showDeleteConfirmation(projectId) {
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

        if (!response.ok) throw new Error('Failed to delete project');

        const modal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmationModal'));
        modal.hide();

        showToast('Project deleted successfully', 'success');

        loadProjectsForSidebar();
        showWelcomeMessage();
    } catch (error) {
        showToast('Failed to delete project!', 'error');
    }
}

async function showEditProjectModal(projectId) {
    try {
        const editModal = document.getElementById('editProjectModal');

        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) throw new Error(`Failed to fetch project details: ${response.status} ${response.statusText}`);
        
        const project = await response.json();
        
        const projectIdField = document.getElementById('editProjectId');
        if (projectIdField) {
            projectIdField.value = project.id;
        } else {
            editModal.setAttribute('data-project-id', project.id);
        }
        
        const startDate = new Date(project.startDate);
        const deadline = project.deadline ? new Date(project.deadline) : null;

        document.getElementById('editProjectName').value = project.name;
        document.getElementById('editProjectDescription').value = project.description || '';
        document.getElementById('editProjectStartDate').value = startDate.toISOString().split('T')[0];
        document.getElementById('editProjectDeadline').value = deadline ? deadline.toISOString().split('T')[0] : '';
        document.getElementById('editProjectStatus').value = project.status;
        document.getElementById('editProjectPriority').value = project.priority;
        
        const bootstrapModal = new bootstrap.Modal(editModal);
        bootstrapModal.show();
    } catch (error) {
        showToast('Error editing the project details!', 'danger');
    }
}

async function updateProject() {
    try {
        const editProjectId = document.getElementById('editProjectId') ? 
            document.getElementById('editProjectId').value : 
            document.getElementById('editProjectModal').getAttribute('data-project-id');
            
        const nameInput = document.getElementById('editProjectName');
        const descriptionInput = document.getElementById('editProjectDescription');
        const startDateInput = document.getElementById('editProjectStartDate');
        const deadlineInput = document.getElementById('editProjectDeadline');
        const priorityInput = document.getElementById('editProjectPriority');
        const statusInput = document.getElementById('editProjectStatus');

        if (!nameInput.value.trim() || !startDateInput.value.trim() || !deadlineInput.value.trim()) {
            showToast('Please fill all required fields', 'warning');
            return;
        }

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

        document.getElementById('updateProjectButton').innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
        document.getElementById('updateProjectButton').disabled = true;

        const csrfTokenMeta = document.querySelector('meta[name="csrf-token"]');
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        };
        
        if (csrfTokenMeta) {
            headers['RequestVerificationToken'] = csrfTokenMeta.content;
        }

        const response = await fetch(`/api/projectsapi/${editProjectId}/update`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(projectData)
        });

        if (response.ok) {
            showToast('Project updated successfully', 'success');
            
            const modalElement = document.getElementById('editProjectModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            } 
         
            await loadProject(editProjectId);
        } else {
            showToast('There was an error updating the project!', 'error');
        }

        await loadProjectsForSidebar();
    } catch (error) {
        showToast('An unexpected error occurred', 'error');
    } finally {
        document.getElementById('updateProjectButton').innerHTML = 'Save Changes';
        document.getElementById('updateProjectButton').disabled = false;
    }
}

function showAddMembersModal(projectId) {
    selectedUsers.clear();

    // First create the modal
    const modal = new bootstrap.Modal(document.getElementById('addMembersModal'));

    // Update the Add Members button to include the projectId
    const addButton = document.querySelector('#addMembersModal .btn-primary');
    addButton.setAttribute('onclick', `addProjectMembers(${projectId})`);

    // Prepare the modal body
    const modalBody = document.querySelector('#addMembersModal .modal-body');

    // Clear the current content
    modalBody.innerHTML = '';

    // Create a container div with a unique ID for the user search
    const containerId = 'addMembersSearchContainer';
    const containerDiv = document.createElement('div');
    containerDiv.id = containerId;
    modalBody.appendChild(containerDiv);

    // Show the modal
    modal.show();

    // Initialize the user search after modal is shown
    // This uses your existing functions to create the search container
    loadUserSearch(containerId).then(() => {
        // After user search is loaded, load available users for this project
        loadAvailableUsers(projectId).then(() => {
            // Initialize selected users display
            refreshSelectedUsersDisplay();
        });
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

/**
 * Show the restore project modal
 * @param {number} projectId - The ID of the project to restore
 */
function showRestoreProjectModal(projectId) {
    // Set the project ID for restoration
    document.getElementById('restoreProjectId').value = projectId;
    
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
        // Get form values
        const projectId = document.getElementById('restoreProjectId').value;
        const newDeadline = document.getElementById('restoreProjectDeadline').value;
        const newStatus = document.getElementById('restoreProjectStatus').value;
        const newPriority = document.getElementById('restoreProjectPriority').value;

        if (!projectId || !newDeadline) {
            showToast('Please fill in all required fields', 'error');
            return;
        }

        // Show loading state
        const restoreBtn = document.getElementById('restoreProjectBtn');
        if (restoreBtn) {
            restoreBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Restoring...';
            restoreBtn.disabled = true;
        }

        // First, fetch the existing project data
        console.log('Fetching existing project data for ID:', projectId);
        const projectResponse = await fetch(`/api/ProjectsApi/${projectId}`);

        if (!projectResponse.ok) {
            throw new Error(`Failed to fetch project data: ${projectResponse.status}`);
        }

        const projectData = await projectResponse.json();
        console.log('Existing project data:', projectData);

        // Format the date properly as an ISO string for JSON serialization
        const deadlineDate = new Date(newDeadline);

        // Create the update data object using existing data plus our changes
        const updateData = {
            // Required fields from existing project
            name: projectData.name,
            description: projectData.description || '',
            startDate: projectData.startDate,

            // New values for restoration
            deadline: deadlineDate.toISOString(),
            status: newStatus,
            priority: newPriority,

            // Add activity log data to avoid nulls
            activityLogAdditions: [
                { action: "restored project", targetType: "Project", additionalData: "{}" }
            ]
        };

        console.log('Restore data being sent:', JSON.stringify(updateData));

        // Use the API endpoint for project update
        const response = await fetch(`/api/projectsapi/${projectId}/update`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(updateData)
        });

        console.log('Restore response status:', response.status);

        // Try to read the response text for better error handling
        let responseText = '';
        try {
            responseText = await response.text();
            console.log('Server response:', responseText);
        } catch (e) {
            console.error('Could not read response text:', e);
        }

        if (!response.ok) {
            try {
                // Try to parse JSON error response
                if (responseText && responseText.trim().startsWith('{')) {
                    const errorData = JSON.parse(responseText);
                    if (errorData.errors) {
                        const errorMessages = Object.entries(errorData.errors)
                            .map(([field, msgs]) => `${field}: ${msgs.join(', ')}`)
                            .join('; ');
                        throw new Error(`Validation failed: ${errorMessages}`);
                    } else {
                        throw new Error(errorData.message || errorData.error || 'Failed to restore project');
                    }
                } else {
                    throw new Error(`Server error: ${response.status}`);
                }
            } catch (parseError) {
                if (parseError.message.includes('Validation failed')) {
                    throw parseError;
                } else {
                    throw new Error(`Failed to restore project: ${response.status}`);
                }
            }
        }

        // Hide the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('restoreProjectModal'));
        if (modal) {
            modal.hide();
        }

        showToast(`Project restored successfully`, 'success');

        // Refresh the sidebar
        await loadProjectsForSidebar();

        // Reload the project with the regular view
        await loadProject(projectId);
    } catch (error) {
        console.error('Error restoring project:', error);
        showToast(error.message, 'error');
    } finally {
        // Reset button state
        const restoreBtn = document.getElementById('restoreProjectBtn');
        if (restoreBtn) {
            restoreBtn.innerHTML = 'Restore Project';
            restoreBtn.disabled = false;
        }
    }
}

// Set up event listener for the restore button
document.addEventListener('DOMContentLoaded', function() {
    // Add event listener to the restore button in the modal
    document.getElementById('restoreProjectBtn').addEventListener('click', restoreProject);
});

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


// Error handler for source map errors (like signalr.min.js.map)
window.addEventListener('error', function(event) {
    // Only suppress source map errors
    if (event.filename && event.filename.endsWith('.map')) {
        // Prevent the error from appearing in console
        event.preventDefault();
        return true;
    }
    return false;
}, true);

/**
 * Setup event listeners for project details page
 * @param {number} projectId - The ID of the project
 */
function setupProjectEventListeners(projectId) {
    // Task filter buttons
    const filterButtons = document.querySelectorAll('.tasks-filters .btn-group .btn');
    filterButtons.forEach(button => {
        button.addEventListener('click', function() {
            filterButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');
            
            const filter = this.getAttribute('data-filter') || this.textContent.trim().toLowerCase();
            filterTasks(projectId, filter);
        });
    });
    
    // Activity feed refresh button
    const refreshBtn = document.getElementById('refreshActivityBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => refreshActivityFeed(projectId, true));
    }
    
    // Setup activity feed scroll event
    const feedContent = document.querySelector('.activity-feed-content');
    if (feedContent) {
        feedContent.addEventListener('scroll', function() {
            if (this.scrollTop > 10) {
                this.classList.add('scrolled-down');
            } else {
                this.classList.remove('scrolled-down');
            }
        });
    }
    
    // Other event listeners...
    
    // View all activities button
    const viewAllBtn = document.getElementById('viewAllActivitiesBtn');
    if (viewAllBtn) {
        viewAllBtn.addEventListener('click', () => showAllActivities(projectId));
    }
}

/**
 * Shows a modal with all activities for a project
 * @param {number} projectId - The ID of the project
 */
async function showAllActivities(projectId) {
    // Create the modal if it doesn't exist
    if (!document.getElementById('allActivitiesModal')) {
        const modalHtml = `
            <div class="modal fade" id="allActivitiesModal" tabindex="-1" aria-labelledby="allActivitiesModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="allActivitiesModalLabel">
                                <i class="bi bi-activity"></i> Project Activity History
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body" id="allActivitiesModalBody">
                            <div class="text-center py-5">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Loading...</span>
                                </div>
                                <p class="mt-2">Loading complete activity history...</p>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('allActivitiesModal'));
    modal.show();

    try {
        // Fetch all activities (use a larger limit)
        const response = await fetch(`/api/projectsapi/${projectId}/activities?limit=100`);
        
        if (!response.ok) {
            throw new Error(`Failed to load activities: ${response.status}`);
        }
        
        const data = await response.json();
        const modalBody = document.getElementById('allActivitiesModalBody');
        
        if (!data.success || !data.activities || data.activities.length === 0) {
            modalBody.innerHTML = `
                <div class="text-center py-4">
                    <div class="empty-icon mx-auto mb-3">
                        <i class="bi bi-clock-history"></i>
                    </div>
                    <p>No activity history found for this project.</p>
                </div>
            `;
            return;
        }
        
        // Create a timeline view for all activities
        let timelineHtml = '<div class="activity-timeline full-timeline">';
        
        data.activities.forEach(activity => {
            timelineHtml += `
                <div class="activity-item">
                    <div class="activity-timeline-connector">
                        <div class="activity-timeline-dot ${getActivityTypeClass(activity.type, activity.targetType)}"></div>
                    </div>
                    <div class="activity-content">
                        <div class="activity-user">
                            <img src="${activity.userAvatar || '/images/default/default-profile.png'}" 
                                 alt="${activity.userName}" 
                                 class="activity-avatar"
                                 onerror="this.onerror=null; this.src='/images/default/default-profile.png'">
                            <div class="activity-user-info">
                                <span class="activity-username">${activity.userName}</span>
                                <span class="activity-time" title="${formatDate(activity.timestamp)}">${formatTimeAgo(new Date(activity.timestamp))}</span>
                            </div>
                        </div>
                        <div class="activity-details">
                            <div class="activity-message">
                                ${activity.description}
                                ${activity.targetName ? `<span class="activity-target">${activity.targetName}</span>` : ''}
                            </div>
                            <div class="activity-icon-badge ${getActivityTypeClass(activity.type, activity.targetType)}">
                                <i class="bi ${getActivityIcon(activity.type, activity.targetType)}"></i>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
        
        timelineHtml += '</div>';
        modalBody.innerHTML = timelineHtml;
        
    } catch (error) {
        console.error('Error loading all activities:', error);
        document.getElementById('allActivitiesModalBody').innerHTML = `
            <div class="text-center py-4 text-danger">
                <i class="bi bi-exclamation-triangle fs-1"></i>
                <p class="mt-3">Failed to load activity history.</p>
                <button class="btn btn-outline-primary mt-2" onclick="showAllActivities(${projectId})">
                    <i class="bi bi-arrow-clockwise"></i> Try Again
                </button>
            </div>
        `;
    }
}

// Helper functions for the activity modal
function getActivityTypeClass(type, targetType) {
    if (type) {
        switch(type.toLowerCase()) {
            case 'task_created': return 'activity-task-created';
            case 'task_assigned': return 'activity-task-assigned';
            case 'task_completed': return 'activity-task-completed';
            case 'task_updated': return 'activity-task-updated';
            case 'member_joined': return 'activity-member-joined';
            case 'project_created': return 'activity-project-created';
            case 'project_updated': return 'activity-project-updated';
            default: return 'activity-default';
        }
    } else if (targetType) {
        switch(targetType.toLowerCase()) {
            case 'task': return 'activity-task-updated';
            case 'project': return 'activity-project-updated';
            case 'member': return 'activity-member-joined';
            default: return 'activity-default';
        }
    }
    return 'activity-default';
}

function getActivityIcon(type, targetType) {
    if (type) {
        switch(type.toLowerCase()) {
            case 'task_created': return 'bi-plus-circle';
            case 'task_assigned': return 'bi-person-check';
            case 'task_completed': return 'bi-check-circle';
            case 'task_updated': return 'bi-pencil';
            case 'member_joined': return 'bi-person-plus';
            case 'project_created': return 'bi-folder-plus';
            case 'project_updated': return 'bi-pencil-square';
            default: return 'bi-info-circle';
        }
    } else if (targetType) {
        switch(targetType.toLowerCase()) {
            case 'task': return 'bi-list-check';
            case 'project': return 'bi-folder-symlink';
            case 'member': return 'bi-people';
            default: return 'bi-info-circle';
        }
    }
    return 'bi-info-circle';
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString();
}