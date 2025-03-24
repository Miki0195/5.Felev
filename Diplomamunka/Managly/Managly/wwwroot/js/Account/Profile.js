document.addEventListener("DOMContentLoaded", function () {
    const phoneInput = document.querySelector("#PhoneNumber");
    if (phoneInput) {
        phoneInput.addEventListener("input", function (e) {
            this.value = this.value.replace(/\D/g, '');
        });
    }

    const profileUpload = document.getElementById('profileUpload');
    const profileImg = document.getElementById('profileImg');

    if (profileUpload && profileImg) {
        profileUpload.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                const file = this.files[0];

                if (file.size > 5 * 1024 * 1024) {
                    showToast('Image size should be less than 5MB', 'danger');
                    this.value = '';
                    return;
                }

                if (!file.type.match('image.*')) {
                    showToast('Please select an image file', 'danger');
                    this.value = '';
                    return;
                }

                const reader = new FileReader();
                profileImg.style.opacity = '0.5';

                reader.onload = function (e) {
                    profileImg.src = e.target.result;
                    profileImg.style.opacity = '1';
                };

                reader.onerror = function () {
                    showToast('Error loading image', 'danger');
                    profileImg.style.opacity = '1';
                };

                reader.readAsDataURL(file);
            }
        });
    }

    const toastContainer = document.getElementById('toastContainer');
    if (toastContainer) {
        const successMessage = toastContainer.getAttribute('data-success-message');
        const errorMessage = toastContainer.getAttribute('data-error-message');

        if (successMessage && successMessage !== '') {
            showToast(successMessage, 'success');
        }

        if (errorMessage && errorMessage !== '') {
            showToast(errorMessage, 'danger');
        }
    }
});
