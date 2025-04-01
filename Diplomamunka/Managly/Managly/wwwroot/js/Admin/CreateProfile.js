document.addEventListener("DOMContentLoaded", function () {
    AOS.init();
    loadNotifications();

    const form = document.querySelector('.needs-validation');
    const submitButton = form.querySelector('button[type="submit"]');
    const inputs = form.querySelectorAll('input, select');
    let isSubmitting = false;

    form.addEventListener('submit', event => {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        if (isSubmitting) {
            event.preventDefault();
            return;
        }

        isSubmitting = true;
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';

        form.classList.add('was-validated');
    });

    inputs.forEach(input => {
        input.addEventListener('input', function () {
            this.classList.remove('is-invalid');
            this.classList.remove('is-valid');

            const validationSpan = this.parentElement.querySelector('[data-valmsg-for]');
            if (validationSpan) {
                validationSpan.textContent = '';
            }

            if (form.checkValidity()) {
                form.classList.remove('was-validated');
            }
        });
    });
});