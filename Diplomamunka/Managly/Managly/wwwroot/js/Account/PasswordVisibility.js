function togglePassword(passwordFieldId, iconId) {
    var passwordField = document.getElementById(passwordFieldId);
    var icon = document.getElementById(iconId);

    if (passwordField.type === "password" || passwordField.type === "confirm_password") {
        passwordField.type = "text";
    icon.classList.remove("fa-eye");
    icon.classList.add("fa-eye-slash");
    } else {
        passwordField.type = "password" || passwordField.type === "confirm_password";
    icon.classList.remove("fa-eye-slash");
    icon.classList.add("fa-eye");
    }
}