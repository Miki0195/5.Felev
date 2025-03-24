function setupEventListeners() {
    const projectIdFromUrl = window.location.hash ? window.location.hash.substring(1) : null;
    const isProjectDetailsPage = !!document.querySelector('.projects-content .project-details');
    const isMembersModalOpen = !!document.querySelector('#manageMembersModal.show');

    loadProjectsForSidebar();

    // ========== SIDEBAR NAVIGATION EVENTS ==========
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
                searchProjects(this.value);
            }
        });
    }

    const createButton = document.getElementById('btn-create-project');
    if (createButton) {
        createButton.addEventListener('click', openCreateProjectModal);
    }

    // ========== PROJECT DETAILS PAGE EVENTS ==========
    if (isProjectDetailsPage && projectIdFromUrl) {
        const projectId = parseInt(projectIdFromUrl);

        const filterButtons = document.querySelectorAll('.tasks-filters .btn-group .btn');
        filterButtons.forEach(button => {
            button.addEventListener('click', function () {
                filterButtons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');

                const filter = this.getAttribute('data-filter') || this.textContent.trim().toLowerCase();
                filterTasks(projectId, filter);
            });
        });

        const feedContent = document.querySelector('.activity-feed-content');
        if (feedContent) {
            feedContent.addEventListener('scroll', function () {
                if (this.scrollTop > 10) {
                    this.classList.add('scrolled-down');
                } else {
                    this.classList.remove('scrolled-down');
                }
            });
        }

        const viewAllBtn = document.getElementById('viewAllActivitiesBtn');
        if (viewAllBtn) {
            viewAllBtn.addEventListener('click', () => showAllActivities(projectId));
        }

    }

    // ========== MEMBERS MANAGEMENT MODAL EVENTS ==========
    if (isMembersModalOpen || document.getElementById('manageMembersModal')) {
        document.querySelectorAll('.role-select').forEach(select => {
            const oldSelect = select.cloneNode(true);
            select.parentNode.replaceChild(oldSelect, select);
            select = oldSelect;

            select.addEventListener('change', async function () {
                const userId = this.dataset.userId;
                const projectId = this.dataset.projectId;
                const newRole = this.value;

                try {
                    await updateMemberRole(projectId, userId, newRole);
                } catch (error) {
                    this.value = this.getAttribute('data-original-value') || 'Member';
                    showToast('Failed to update member role', 'error');
                }
            });

            select.setAttribute('data-original-value', select.value);
        });

        document.querySelectorAll('.remove-member-btn').forEach(button => {
            const oldButton = button.cloneNode(true);
            button.parentNode.replaceChild(oldButton, button);
            button = oldButton;

            button.addEventListener('click', async function () {
                const userId = this.dataset.userId;
                const projectId = this.dataset.projectId;

                if (confirm('Are you sure you want to remove this team member from the project?')) {
                    try {
                        await removeMember(projectId, userId);
                        this.closest('tr').remove();
                    } catch (error) {
                        showToast('Failed to remove team member', 'error');
                    }
                }
            });
        });
    }
}

document.addEventListener('DOMContentLoaded', setupEventListeners);

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

        setupEventListeners();
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

async function showRestoreProjectModal(projectId) {
    try {
        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error(`Failed to fetch project details: ${response.status}`);
        }

        const project = await response.json();

        document.getElementById('restoreProjectId').value = projectId;

        const today = new Date();
        const oneMonthLater = new Date(today);
        oneMonthLater.setMonth(today.getMonth() + 1);

        const formattedDate = oneMonthLater.toISOString().split('T')[0];
        document.getElementById('restoreProjectDeadline').value = formattedDate;
        document.getElementById('restoreProjectDeadline').min = today.toISOString().split('T')[0];

        document.getElementById('restoreProjectStatus').value = "In Progress";
        document.getElementById('restoreProjectPriority').value = project.priority || "Medium";

        const restoreBtn = document.getElementById('restoreProjectBtn');
        if (restoreBtn) {
            const newBtn = restoreBtn.cloneNode(true);
            restoreBtn.parentNode.replaceChild(newBtn, restoreBtn);

            newBtn.addEventListener('click', () => restoreProject(projectId));
        }

        const modal = new bootstrap.Modal(document.getElementById('restoreProjectModal'));
        modal.show();
    } catch (error) {
        showToast('Failed to prepare project restoration. Please try again.', 'error');
    }
}

async function restoreProject(projectId) {
    try {
        const deadlineInput = document.getElementById('restoreProjectDeadline');
        const statusInput = document.getElementById('restoreProjectStatus');
        const priorityInput = document.getElementById('restoreProjectPriority');
        
        if (!deadlineInput.value) {
            showToast('Please select a deadline', 'warning');
            return;
        }
        
        const restoreBtn = document.getElementById('restoreProjectBtn');
        restoreBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Restoring...';
        restoreBtn.disabled = true;
        
        const deadline = new Date(deadlineInput.value);
        
        const restoreData = {
            deadline: deadline.toISOString(), 
            status: statusInput.value,
            priority: priorityInput.value
        };

        const csrfTokenMeta = document.querySelector('meta[name="csrf-token"]');
        const headers = {
            'Content-Type': 'application/json'
        };
        
        if (csrfTokenMeta) {
            headers['RequestVerificationToken'] = csrfTokenMeta.content;
        }
        
        const response = await fetch(`/Projects/restore/${projectId}`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(restoreData)
        });
        
        if (!response.ok) {
            throw new Error(`Failed to restore project: ${response.status}`);
        }
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('restoreProjectModal'));
        if (modal) {
            modal.hide();
        }
        
        showToast('Project restored successfully', 'success');
        
        await loadProjectsForSidebar();
        await loadProject(projectId);
    } catch (error) {
        showToast(`Failed to restore project: ${error.message}`, 'error');
    } finally {
        const restoreBtn = document.getElementById('restoreProjectBtn');
        if (restoreBtn) {
            restoreBtn.innerHTML = 'Restore Project';
            restoreBtn.disabled = false;
        }
    }
}
// EVERYTHING FOR MANAGING MEMBERS
function showAddMembersModal(projectId) {
    selectedUsers.clear();

    const modal = new bootstrap.Modal(document.getElementById('addMembersModal'));

    const addButton = document.querySelector('#addMembersModal .btn-primary');
    addButton.setAttribute('onclick', `addProjectMembers(${projectId})`);

    const modalBody = document.querySelector('#addMembersModal .modal-body');

    modalBody.innerHTML = '';

    const containerId = 'addMembersSearchContainer';
    const containerDiv = document.createElement('div');
    containerDiv.id = containerId;
    modalBody.appendChild(containerDiv);

    modal.show();

    loadUserSearch(containerId).then(() => {
        loadAvailableUsers(projectId).then(() => {
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

        const modal = bootstrap.Modal.getInstance(document.getElementById('addMembersModal'));
        modal.hide();

        showToast('Team members added successfully', 'success');

        await loadProject(projectId);
    } catch (error) {
        showToast('There was an error adding members to the project!', 'error');
    }
}

async function showManageMembersModal(projectId) {
    try {
        const modalBody = document.querySelector('#manageMembersModal .modal-body');
        modalBody.innerHTML = `<div class="text-center py-4"><div class="spinner-border" role="status"></div><p class="mt-2">Loading team members...</p></div>`;

        const modal = new bootstrap.Modal(document.getElementById('manageMembersModal'));
        modal.show();

        const response = await fetch(`/Projects/ManageMembers/${projectId}`, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            let errorMessage = `Error ${response.status}: ${response.statusText}`;
            throw new Error(errorMessage);
        }
        
        const html = await response.text();
        modalBody.innerHTML = html;

        setupEventListeners();
    } catch (error) {
        showToast(`Failed to load team members!`, 'error');
        
    }
}

async function updateMemberRole(projectId, userId, newRole) {
    try {
        const button = document.querySelector(`.role-select[data-user-id="${userId}"]`);
        const originalValue = button?.getAttribute('data-original-value');
        
        const response = await fetch(`/api/projectsapi/${projectId}/members/${userId}/role`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ role: newRole })
        });

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }

        showToast('Member role updated successfully', 'success');

        if (button) {
            button.setAttribute('data-original-value', newRole);
        }

        refreshActivityFeed(projectId, false);
        await loadProject(projectId);
    } catch (error) {
        showToast('There was an error updating the role!');
    } finally {
        const button = document.querySelector(`.role-select[data-user-id="${userId}"]`);
    }
}

async function removeMember(projectId, userId) {
    try {
        const button = document.querySelector(`.remove-member-btn[data-user-id="${userId}"]`);
        
        const response = await fetch(`/api/projectsapi/${projectId}/members/${userId}`, {
            method: 'DELETE',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }
        
        showToast('Team member removed successfully', 'success');

        refreshActivityFeed(projectId, false);
        await loadProject(projectId);
    } catch (error) {
        showToast('There was an error removing the member from the project!', 'error');
    }
}

async function refreshActivityFeed(projectId, showLoadingIndicator = true) {
    const activityFeedContainer = document.getElementById('activityFeedContainer');
        
    if (showLoadingIndicator) {
        activityFeedContainer.innerHTML = `
            <div class="text-center py-3">
                <div class="spinner-border spinner-border-sm text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span class="ms-2">Refreshing activity feed...</span>
            </div>`;
    }

    let success = false;

    try {
        const response = await fetch(`/Projects/${projectId}/ActivityFeed`);

        if (response.ok) {
            const html = await response.text();
            activityFeedContainer.innerHTML = html;
            success = true;

            const feedContent = document.querySelector('.activity-feed-content');
            if (feedContent) {
                feedContent.addEventListener('scroll', function () {
                    if (this.scrollTop > 10) {
                        this.classList.add('scrolled-down');
                    } else {
                        this.classList.remove('scrolled-down');
                    }
                });
            }
        } else {
            console.warn(`Activity feed primary refresh failed: ${response.status} ${response.statusText}`);
        }
    } catch (error) {
        console.warn("Primary activity feed refresh attempt failed:", error);
    }
}

async function showAllActivities(projectId) {
    const modal = new bootstrap.Modal(document.getElementById('allActivitiesModal'));
    modal.show();

    const modalBody = document.getElementById('allActivitiesModalBody');
    modalBody.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2">Loading complete activity history...</p>
        </div>`;

    try {
        const response = await fetch(`/Projects/${projectId}/AllActivities`, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to load activities: ${response.status}`);
        }

        const html = await response.text();
        modalBody.innerHTML = html;
    } catch (error) {
        console.error('Error loading activities:', error);
        modalBody.innerHTML = `
            <div class="text-center py-4 text-danger">
                <i class="bi bi-exclamation-triangle fs-1"></i>
                <p class="mt-3">Failed to load activity history: ${error.message}</p>
                <button class="btn btn-outline-primary mt-2" onclick="showAllActivities(${projectId})">
                    <i class="bi bi-arrow-clockwise"></i> Try Again
                </button>
            </div>
        `;
    }
}
// EVERYTHING FOR TASKS
async function showCreateTaskModal(projectId) {
    try {
        const modal = document.getElementById('createTaskModal');
        if (!modal) {
            showToast('Error: Modal not found. Please reload the page.', 'error');
            return;
        }

        const formElements = {
            'taskTitle': document.getElementById('taskTitle'),
            'taskDescription': document.getElementById('taskDescription'),
            'taskDueDate': document.getElementById('taskDueDate'),
            'taskPriority': document.getElementById('taskPriority'),
            'taskAssignedTo': document.getElementById('taskAssignedTo')
        };

        const response = await fetch(`/api/projectsapi/${projectId}`);
        if (!response.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await response.json();

        const assignedToSelect = document.getElementById('taskAssignedTo');
        if (!assignedToSelect) {
            throw new Error('Cannot find the assigned users dropdown');
        }
        
        assignedToSelect.innerHTML = '';
        
        project.projectMembers.forEach(member => {
            const option = document.createElement('option');
            option.value = member.user.id;
            option.textContent = `${member.user.name} ${member.user.lastName} (${member.role})`;
            assignedToSelect.appendChild(option);
        });

        const today = new Date().toISOString().split('T')[0];
        const dueDateInput = document.getElementById('taskDueDate');
        if (dueDateInput) {
            dueDateInput.min = today;
            dueDateInput.value = today;
        }

        formElements.taskTitle.value = '';
        formElements.taskDescription.value = '';
        formElements.taskPriority.value = 'Medium';

        const createTaskBtn = modal.querySelector('.modal-footer .btn-primary');
        if (createTaskBtn) {
            createTaskBtn.setAttribute('onclick', `createTask(${projectId})`);
        }

        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
    } catch (error) {
        showToast('Failed to load task creation form. Please try again.', 'error');
    }
}

async function createTask(projectId) {
    try {
        const titleInput = document.getElementById('taskTitle');
        const descriptionInput = document.getElementById('taskDescription');
        const dueDateInput = document.getElementById('taskDueDate');
        const priorityInput = document.getElementById('taskPriority');
        const assignedToInput = document.getElementById('taskAssignedTo');

        const taskTitle = titleInput.value.trim();
        const description = descriptionInput.value.trim();
        const dueDate = dueDateInput.value;
        const priority = priorityInput.value;
        const assignedToId = assignedToInput.value;

        if (!taskTitle || !dueDate || !assignedToId) {
            showToast('Please fill out all required fields and assign at least one user', 'error');
            return;
        }

        const taskData = {
            taskTitle: taskTitle,
            description: description || "",
            dueDate: dueDate,
            priority: priority,
            assignedToId: assignedToId, 
            activityLogAdditions: [
                { action: "created task", targetType: "Task", additionalData: "{}" },
                { action: "assigned task", targetType: "Task", additionalData: "{}" }
            ]
        };
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

        if (!response.ok) {
            throw new Error('Failed to create task: ' + (response.status === 400 ? 'Invalid data' : 'Server error'));
        }

        const modal = bootstrap.Modal.getInstance(document.getElementById('createTaskModal'));
        if (modal) {
            modal.hide();
        } else {
            const modalElement = document.getElementById('createTaskModal');
            if (modalElement) {
                modalElement.classList.remove('show');
                modalElement.style.display = 'none';
                document.body.classList.remove('modal-open');
                document.querySelector('.modal-backdrop')?.remove();
            }
        }
        
        showToast('Task created successfully', 'success');
        
        await loadProject(projectId);
        await loadProjectsForSidebar(projectId);
    } catch (error) {
        showToast('Error creating task: ' + error.message, 'error');
    } finally {
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
        if (!response.ok) throw new Error('Failed to load task details');

        const task = await response.json();

        const projectResponse = await fetch(`/api/projectsapi/${task.projectId}`);
        if (!projectResponse.ok) {
            throw new Error('Failed to load project details');
        }
        const project = await projectResponse.json();

        const assignedToSelect = document.getElementById('editTaskAssignedTo');
        assignedToSelect.innerHTML = project.projectMembers.map(member => `
                    <option value="${member.user.id}">
                        ${member.user.name} ${member.user.lastName} (${member.role})
                    </option>
                `).join('');

        document.getElementById('editTaskTitle').value = task.taskTitle;
        document.getElementById('editTaskDescription').value = task.description || '';
        document.getElementById('editTaskDueDate').value = new Date(task.dueDate).toISOString().split('T')[0];
        document.getElementById('editTaskPriority').value = task.priority;
        document.getElementById('editTaskStatus').value = task.status;

        const assignedUserIds = task.assignedUsers.map(u => u.id);
        Array.from(assignedToSelect.options).forEach(option => {
            option.selected = assignedUserIds.includes(option.value);
        });

        const modalFooter = document.getElementById('taskModalFooter');
        modalFooter.innerHTML = `
                    <button type="button" class="btn btn-danger me-auto" onclick="deleteTask(${task.id}, ${task.projectId})">Delete Task</button>
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="updateTask(${task.id}, ${task.projectId})">Save Changes</button>
                `;

        const modal = new bootstrap.Modal(document.getElementById('taskDetailsModal'));
        modal.show();
    } catch (error) {
        showToast(error.message, 'error');
    }
}

async function deleteTask(taskId, projectId) {
    try {
        const headers = {
            'Content-Type': 'application/json'
        };
        
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
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        if (modal) modal.hide();
        
        const confirmModal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmationModal'));
        if (confirmModal) confirmModal.hide();
        
        showToast('Task deleted successfully', 'success');
        
        await loadProject(projectId);
        await loadProjectsForSidebar(projectId);
    } catch (error) {
        showToast('Error deleting task: ' + error.message, 'error');
    }
}

async function updateTask(taskId, projectId) {
    try {
        const taskTitle = document.getElementById('editTaskTitle').value.trim();
        const description = document.getElementById('editTaskDescription').value.trim();
        const dueDate = document.getElementById('editTaskDueDate').value;
        const priority = document.getElementById('editTaskPriority').value;
        const status = document.getElementById('editTaskStatus').value;
        
        const assignedToSelect = document.getElementById('editTaskAssignedTo');
        const assignedUserIds = Array.from(assignedToSelect.selectedOptions).map(option => option.value);
        
        if (!taskTitle || !dueDate || assignedUserIds.length === 0) {
            showToast('Please fill out all required fields and assign at least one user', 'error');
            return;
        }
        
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
        
        if (!response.ok) {
            throw new Error('Failed to update task: ' + responseText);
        }
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('taskDetailsModal'));
        if (modal) {
            modal.hide();
        } else {
            document.getElementById('taskDetailsModal').classList.remove('show');
            document.body.classList.remove('modal-open');
            document.querySelector('.modal-backdrop')?.remove();
        }
        
        showToast('Task updated successfully', 'success');
        
        await loadProject(projectId);
        await loadProjectsForSidebar(projectId);
    } catch (error) {
        showToast('Error updating task: ' + error.message, 'error');
    } finally {
        const updateButton = document.querySelector('#taskDetailsModal .btn-primary');
        if (updateButton) {
            updateButton.innerHTML = 'Save Changes';
            updateButton.disabled = false;
        }
    }
}

async function updateTaskStatus(taskId, projectId, newStatus, event) {
    if (event) {
        event.stopPropagation();
    }
    
    try {
        const additionalData = JSON.stringify({
            taskId: taskId,
            newStatus: newStatus,
            timestamp: new Date().toISOString()
        });
        
        const statusData = {
            status: newStatus,
            additionalData: additionalData
        };
        
        const headers = {
            'Content-Type': 'application/json'
        };

        const response = await fetch(`/api/projectsapi/tasks/${taskId}/status`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(statusData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            
            if (event && event.target && event.target.type === 'checkbox') {
                event.target.checked = newStatus === 'Completed';
            }
            
            throw new Error('Failed to update task status');
        }

        await loadProject(projectId);
        await loadProjectsForSidebar(projectId);
        showToast(`Task marked as ${newStatus}`, 'success');
    } catch (error) {
        console.error('Error updating task status:', error);
        showToast('Error updating task status: ' + error.message, 'error');
        
        if (event && event.target && event.target.type === 'checkbox') {
            event.target.checked = newStatus === 'Completed';
        }
    }
}