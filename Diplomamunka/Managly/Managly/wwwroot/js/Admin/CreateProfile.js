document.addEventListener("DOMContentLoaded", function () {
    AOS.init();
    loadNotifications();

    const form = document.querySelector('.needs-validation');
    const inputs = form.querySelectorAll('input, select');

    form.addEventListener('submit', event => {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
        }
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