/**
 * User Management Module
 * Handles all functionality for user management in the admin panel
 */
(function() {
    'use strict';
    
    // ===== CONSTANTS & STATE MANAGEMENT =====
    
    // Track current sort state
    let currentSort = {
        column: '',
        direction: 'asc'
    };
    
    // Track role change state
    let roleChangeState = {
        activeForm: null,
        originalValue: null
    };
    
    // Cache for DOM elements
    const elements = {};
    
    // ===== INITIALIZATION =====
    
    /**
     * Initialize the module when DOM is ready
     */
    function init() {
        cacheElements();
        setupEventListeners();
        initAvatarFallbacks();
    }
    
    /**
     * Cache all DOM elements for better performance
     */
    function cacheElements() {
        const userRoleContainer = document.querySelector('.user-management-container');
        const hiddenRoleInput = document.getElementById('currentUserRole');
        const currentUserRole = (hiddenRoleInput ? hiddenRoleInput.value : 'Employee');

        elements.currentUserRole = currentUserRole;
        elements.isAdmin = currentUserRole === 'Admin';
        elements.isManager = currentUserRole === 'Admin' || currentUserRole === 'Manager';

        elements.searchInput = document.getElementById('userSearch');
        elements.roleFilter = document.getElementById('roleFilter');
        elements.clearFiltersBtn = document.getElementById('clearFilters');
        elements.userRows = document.querySelectorAll('#userTableBody tr');
        elements.userTableBody = document.querySelector('#userTableBody');
        elements.sortHeaders = document.querySelectorAll('th div[data-sort]');
        elements.roleSelects = document.querySelectorAll('.role-select');

        elements.deleteButton = document.querySelectorAll('#delete');
        elements.deleteButtons = document.querySelectorAll('.delete-confirm');
        elements.saveVacationButtons = document.querySelectorAll('.save-vacation-days');
        elements.vacationDaysInputs = document.querySelectorAll('input[id^="totalVacationDays-"]');
        elements.confirmRoleChangeBtn = document.getElementById('confirmRoleChange');
        elements.confirmRoleChangeModal = document.getElementById('confirmRoleChangeModal');
        elements.roleUserName = document.getElementById('roleUserName');
        elements.newRole = document.getElementById('newRole');
        elements.avatars = document.querySelectorAll('.avatar-img');
        elements.antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
    }
    
    /**
     * Set up all event listeners
     */
    function setupEventListeners() {
        elements.searchInput.addEventListener('input', filterRows);
        elements.roleFilter.addEventListener('change', filterRows);
        elements.clearFiltersBtn.addEventListener('click', clearFilters);
        if (elements.isAdmin) {
            elements.roleSelects.forEach(select => setupRoleSelect(select));
            elements.confirmRoleChangeBtn.addEventListener('click', confirmRoleChange);
            elements.confirmRoleChangeModal.addEventListener('hidden.bs.modal', handleRoleChangeModalDismiss);
        } else {
            elements.roleSelects.forEach(select => {
                select.disabled = true;
                select.title = "Only administrators can change user roles";
                select.addEventListener('click', () => showToast("You don't have permission to change user roles.", "warning"));
            });
        }

        if (elements.isAdmin) {
            elements.deleteButtons.forEach(button => button.addEventListener('click', deleteUser));
        } else {
            elements.deleteButton.forEach(button => {
                button.disabled = true;
                button.addEventListener('click', (e) => {
                    e.preventDefault();
                    showToast("Only administrators can delete users.", "warning");
                });
            });
            elements.deleteButtons.forEach(button => {
                button.disabled = true; 
                button.addEventListener('click', (e) => {
                    e.preventDefault();
                    showToast("Only administrators can delete users.", "warning");
                });
            });
        }

        elements.saveVacationButtons.forEach(button => button.addEventListener('click', saveVacationDays));
        elements.vacationDaysInputs.forEach(input => input.addEventListener('change', validateVacationDays));

        elements.sortHeaders.forEach(header => header.addEventListener('click', handleSortClick));
    }
    
    /**
     * Initialize fallbacks for avatar images that fail to load
     */
    function initAvatarFallbacks() {
        elements.avatars.forEach(img => {
            img.addEventListener('error', handleAvatarError);
        });
    }
    
    /**
     * Get the anti-forgery token
     * @returns {string} - CSRF token value
     */
    function getAntiForgeryToken() {
        return elements.antiForgeryToken ? elements.antiForgeryToken.value : '';
    }
    
    /**
     * Handle API response properly based on HTTP status and response content
     * @param {Response} response - The fetch API response
     * @returns {Promise} - Promise that resolves to the parsed response data
     */
    async function handleApiResponse(response) {
        // Get the response body as JSON
        const data = await response.json();
        
        // Check if the request was successful based on HTTP status code
        if (!response.ok) {
            // Add the HTTP status to the error data for better error handling
            throw { 
                ...data, 
                status: response.status, 
                statusText: response.statusText 
            };
        }
        
        // Return the data with status information
        return { ...data, status: response.status };
    }
    
    // ===== FILTERING & SEARCHING =====
    
    /**
     * Filter table rows based on search input and role filter
     */
    function filterRows() {
        const searchTerm = elements.searchInput.value.toLowerCase().trim();
        const roleValue = elements.roleFilter.value.toLowerCase();
        
        elements.userRows.forEach(row => {
            const name = row.getAttribute('data-user-name');
            const email = row.getAttribute('data-user-email');
            const role = row.getAttribute('data-user-role');
            
            const matchesSearch = searchTerm === '' || 
                name.includes(searchTerm) || 
                email.includes(searchTerm);
                
            const matchesRole = roleValue === '' || 
                role.toLowerCase() === roleValue;
                
            row.style.display = (matchesSearch && matchesRole) ? '' : 'none';
        });
    }
    
    /**
     * Clear all filters and show all rows
     */
    function clearFilters() {
        elements.searchInput.value = '';
        elements.roleFilter.value = '';
        elements.userRows.forEach(row => row.style.display = '');
        showToast('Filters cleared', 'success');
    }
    
    // ===== ROLE MANAGEMENT =====
    
    /**
     * Setup event handlers for role select dropdowns
     * @param {HTMLElement} select - The role select element
     */
    function setupRoleSelect(select) {
        select.dataset.originalValue = select.value;
        
        select.addEventListener('change', function(e) {
            e.preventDefault();
            const userName = this.getAttribute('data-user-name');
            const newRole = this.value;
            
            roleChangeState.activeForm = this.closest('form');
            roleChangeState.originalValue = this.dataset.originalValue;
            
            elements.roleUserName.textContent = userName;
            elements.newRole.textContent = newRole;
            
            const modal = new bootstrap.Modal(elements.confirmRoleChangeModal);
            modal.show();
        });
    }
    
    /**
     * Handle role change confirmation
     */
    function confirmRoleChange() {
        if (!roleChangeState.activeForm) return;

        const formData = new FormData(roleChangeState.activeForm);
        const userId = formData.get('userId');
        const newRole = formData.get('role');

        fetch(roleChangeState.activeForm.action, {
            method: 'POST',
            body: formData
        })
            .then(handleApiResponse)
            .then(data => {
                const modal = bootstrap.Modal.getInstance(elements.confirmRoleChangeModal);
                modal.hide();

                if (data.success) {
                    updateRoleUI(userId, newRole, roleChangeState.activeForm);
                    showToast(data.message, 'success');

                    roleChangeState.originalValue = newRole; 
                } else {
                    showToast(data.message, 'error');
                    revertRoleSelect(roleChangeState.activeForm);
                }
            })
            .catch(error => {
                showToast(error.message || 'An error occurred while updating the role.', 'error');
                revertRoleSelect(roleChangeState.activeForm);

                const modal = bootstrap.Modal.getInstance(elements.confirmRoleChangeModal);
                modal.hide();
            });
    }
    
    /**
     * Update UI after successful role change
     * @param {string} userId - The user ID
     * @param {string} newRole - The new role
     * @param {HTMLElement} form - The form element
     */
    function updateRoleUI(userId, newRole, form) {
        const row = form.closest('tr');
        if (row) {
            // Update the data attribute
            row.setAttribute('data-user-role', newRole);
            
            // Update the select element in the table
            const roleSelect = row.querySelector('.role-select');
            if (roleSelect) {
                roleSelect.value = newRole;
                roleSelect.dataset.originalValue = newRole;
            }
            
            // Update the role in the user details modal if it exists
            const userModal = document.querySelector(`#userModal-${userId}`);
            if (userModal) {
                const roleText = userModal.querySelector('.profile-basic-info p.text-muted');
                if (roleText) {
                    roleText.textContent = newRole;
                }
            }
        }
    }
    
    /**
     * Revert role select to original value
     * @param {HTMLElement} form - The form element
     */
    function revertRoleSelect(form) {
        if (form && roleChangeState.originalValue) {
            form.querySelector('select').value = roleChangeState.originalValue;
        }
    }
    
    /**
     * Handle role change modal dismissal
     */
    function handleRoleChangeModalDismiss() {
        revertRoleSelect(roleChangeState.activeForm);
        roleChangeState.activeForm = null;
        roleChangeState.originalValue = null;
    }
    
    // ===== USER MANAGEMENT =====
    
    /**
     * Delete a user
     */
    function deleteUser() {
        const userId = this.getAttribute('data-user-id');
        const token = getAntiForgeryToken();
        
        fetch('/Admin/DeleteUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: new URLSearchParams({ userId: userId })
        })
        .then(handleApiResponse)
        .then(data => {
            if (data.success) {
                const row = this.closest('tr');
                if (row) {
                    row.remove();
                }
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            showToast(error.message || 'An error occurred while deleting the user.', 'error');
        });
    }
    
    /**
     * Open user edit modal
     * @param {string} userId - The user ID
     */
    function openEditModal(userId) {
        fetch(`/Admin/GetUserDetails?userId=${userId}`)
            .then(handleApiResponse)
            .then(data => {
                document.getElementById('editUserId').value = data.userId;
                document.getElementById('editName').value = data.name;
                document.getElementById('editLastName').value = data.lastName;
                document.getElementById('editEmail').value = data.email;
                document.getElementById('editRole').value = data.roles;
                document.getElementById('editPhoneNumber').value = data.phoneNumber;
                document.getElementById('editCountry').value = data.country;
                document.getElementById('editCity').value = data.city;
                document.getElementById('editAddress').value = data.address;
                document.getElementById('editDateOfBirth').value = data.dateOfBirth ? data.dateOfBirth.substring(0, 10) : '';
                document.getElementById('editGender').value = data.gender;
                
                // Set vacation days information
                document.getElementById('editTotalVacationDays').value = data.totalVacationDays;
                document.getElementById('editUsedVacationDays').value = data.usedVacationDays;
                document.getElementById('editRemainingVacationDays').value = data.remainingVacationDays;
                
                const modal = new bootstrap.Modal(document.getElementById('editUserModal'));
                modal.show();
            })
            .catch(error => {
                showToast(error.message || 'Failed to load user details', 'error');
            });
    }
    
    /**
     * Handle avatar image loading errors
     * @param {Event} e - The error event
     */
    function handleAvatarError(e) {
        const img = e.target;
        const parent = img.parentElement;
        const userName = parent.closest('.user-info')?.querySelector('span')?.textContent || 'User';
        
        // Replace broken image with first letter avatar
        parent.innerHTML = userName.charAt(0).toUpperCase();
        parent.style.backgroundColor = getColorForName(userName);
    }
    
    /**
     * Generate a consistent color based on a name
     * @param {string} name - The name to generate a color for
     * @returns {string} - Hex color code
     */
    function getColorForName(name) {
        let hash = 0;
        for (let i = 0; i < name.length; i++) {
            hash = name.charCodeAt(i) + ((hash << 5) - hash);
        }
        
        const colors = [
            '#3498db', '#e74c3c', '#2ecc71', '#f39c12', 
            '#9b59b6', '#1abc9c', '#34495e', '#16a085'
        ];
        
        return colors[Math.abs(hash) % colors.length];
    }
    
    // ===== VACATION DAYS MANAGEMENT =====
    
    /**
     * Save vacation days for a user
     */
    function saveVacationDays() {
        const userId = this.getAttribute('data-user-id');
        const totalDaysInput = document.getElementById(`totalVacationDays-${userId}`);
        const totalDays = parseInt(totalDaysInput.value);
        const token = getAntiForgeryToken();
        
        if (isNaN(totalDays) || totalDays < 20) {
            showToast('Total vacation days must be at least 20', 'error');
            totalDaysInput.value = 20;
            return;
        }
        
        fetch('/Admin/UpdateVacationDays', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: new URLSearchParams({ 
                userId: userId,
                totalVacationDays: totalDays
            })
        })
        .then(handleApiResponse)
        .then(data => {
            if (data.success) {
                updateVacationDaysUI(this, data);
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            showToast(error.message || 'An error occurred while updating vacation days.', 'error');
        });
    }
    
    /**
     * Update vacation days UI
     * @param {HTMLElement} button - The button that was clicked
     * @param {Object} data - The response data
     */
    function updateVacationDaysUI(button, data) {
        const modalBody = button.closest('.card-body');
        const inputs = modalBody.querySelectorAll('input');
        
        inputs[0].value = data.totalDays;
        inputs[1].value = data.usedDays;
        inputs[2].value = data.remainingDays;
        
        const progressBar = modalBody.querySelector('.progress-bar');
        const usedPercentage = data.totalDays > 0 ? 
            (data.usedDays * 100) / data.totalDays : 0;
            
        progressBar.style.width = `${usedPercentage}%`;
        progressBar.setAttribute('aria-valuenow', usedPercentage);
        progressBar.textContent = `${Math.round(usedPercentage)}%`;
    }
    
    /**
     * Validate vacation days input
     */
    function validateVacationDays() {
        const value = parseInt(this.value);
        if (isNaN(value) || value < 20) {
            this.value = 20;
            showToast('Total vacation days must be at least 20', 'warning');
        }
    }
    
    /**
     * Update vacation days in the edit modal
     * @param {string} userId - The user ID
     * @param {number} totalDays - The total vacation days
     */
    function updateVacationDays(userId, totalDays) {
        const token = getAntiForgeryToken();
        
        fetch('/Admin/UpdateVacationDays', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: new URLSearchParams({ 
                userId: userId,
                totalVacationDays: totalDays
            })
        })
        .then(handleApiResponse)
        .then(data => {
            if (data.success) {
                const totalInput = document.getElementById('editTotalVacationDays');
                const usedInput = document.getElementById('editUsedVacationDays');
                const remainingInput = document.getElementById('editRemainingVacationDays');
                
                if (totalInput) totalInput.value = data.totalDays;
                if (usedInput) usedInput.value = data.usedDays;
                if (remainingInput) remainingInput.value = data.remainingDays;
                
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            showToast(error.message || 'Failed to update vacation days', 'error');
        });
    }
    
    // ===== SORTING =====
    
    /**
     * Handle clicking on a sortable header
     */
    function handleSortClick() {
        const sortColumn = this.getAttribute('data-sort');
        const icon = this.querySelector('.sort-icon');
        
        // Reset other icons
        document.querySelectorAll('.sort-icon').forEach(i => {
            if (i !== icon) {
                i.className = 'fas fa-sort sort-icon';
            }
        });
        
        // Update sort direction
        if (currentSort.column === sortColumn) {
            currentSort.direction = currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            currentSort.column = sortColumn;
            currentSort.direction = 'asc';
        }
        
        // Update icon
        icon.className = `fas fa-sort-${currentSort.direction === 'asc' ? 'up' : 'down'} sort-icon`;
        
        sortRows(sortColumn);
    }
    
    /**
     * Sort table rows
     * @param {string} sortColumn - The column to sort by
     */
    function sortRows(sortColumn) {
        const rows = Array.from(elements.userTableBody.querySelectorAll('tr'));
        
        rows.sort((a, b) => {
            let aValue, bValue;
            
            if (sortColumn === 'name') {
                aValue = a.querySelector('.user-info span').textContent.trim().toLowerCase();
                bValue = b.querySelector('.user-info span').textContent.trim().toLowerCase();
            } else if (sortColumn === 'email') {
                aValue = a.querySelector('td:nth-child(2)').textContent.trim().toLowerCase();
                bValue = b.querySelector('td:nth-child(2)').textContent.trim().toLowerCase();
            }
            
            if (currentSort.direction === 'asc') {
                return aValue > bValue ? 1 : -1;
            } else {
                return aValue < bValue ? 1 : -1;
            }
        });
        
        // Reorder rows in the DOM
        rows.forEach(row => elements.userTableBody.appendChild(row));
        
        // Reinitialize avatar fallbacks for any moved images
        initAvatarFallbacks();
    }
    
    // ===== UTILITIES =====
    
    // Initialize UI enhancement event handlers for the edit modal
    function setupEditModalHandlers() {
        const totalDaysInput = document.getElementById('editTotalVacationDays');
        if (totalDaysInput) {
            totalDaysInput.addEventListener('change', function() {
                const totalDays = parseInt(this.value);
                const usedDays = parseInt(document.getElementById('editUsedVacationDays').value);
                
                if (isNaN(totalDays) || totalDays < 20) {
                    this.value = 20;
                    showToast('Minimum vacation days allowed is 20', 'warning');
                    return;
                }
                
                const remainingDays = Math.max(0, totalDays - usedDays);
                document.getElementById('editRemainingVacationDays').value = remainingDays;
            });
        }
        
        const saveButton = document.getElementById('saveUserChanges');
        if (saveButton) {
            saveButton.addEventListener('click', function() {
                const userId = document.getElementById('editUserId').value;
                const totalVacationDays = parseInt(document.getElementById('editTotalVacationDays').value);
                
                updateVacationDays(userId, totalVacationDays);
            });
        }
    }
    
    // ===== PUBLIC INTERFACE =====
    
    // Expose functions to global scope
    window.userManagement = {
        openEditModal: openEditModal,
        updateVacationDays: updateVacationDays
    };
    
    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        init();
        setupEditModalHandlers();
    });
    
})();