/**
 * EVA IATS - Login Page JavaScript
 * Handles form validation and authentication
 */

document.addEventListener('DOMContentLoaded', () => {
    initLoginForm();
    initPasswordToggle();
});

/* =========================================
   LOGIN FORM HANDLING
   ========================================= */
function initLoginForm() {
    const form = document.getElementById('loginForm');
    const emailInput = document.getElementById('email');
    const passwordInput = document.getElementById('password');
    const loginBtn = document.getElementById('loginBtn');
    const btnText = loginBtn.querySelector('.btn-text');
    const btnLoader = loginBtn.querySelector('.btn-loader');
    const alertMessage = document.getElementById('alertMessage');

    // Form submission
    form.addEventListener('submit', (e) => {

        // Validate inputs
        const email = emailInput.value.trim();
        const password = passwordInput.value.trim();

        let isValid = true;

        // Email validation
        if (!email) {
            showError('email', 'Email address is required');
            isValid = false;
        } else if (!isValidEmail(email)) {
            showError('email', 'Please enter a valid email address');
            isValid = false;
        }

        // Password validation
        if (!password) {
            showError('password', 'Password is required');
            isValid = false;
        } else if (password.length < 6) {
            showError('password', 'Password must be at least 6 characters');
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
            return;
        }

        // Show loading state and allow form to submit
        loginBtn.disabled = true;
        btnText.style.display = 'none';
        btnLoader.style.display = 'inline-block';
    });

    // Real-time validation
    emailInput.addEventListener('blur', () => {
        const email = emailInput.value.trim();
        if (email && !isValidEmail(email)) {
            showError('email', 'Please enter a valid email address');
        }
    });

    emailInput.addEventListener('input', () => {
        if (emailInput.parentElement.classList.contains('error')) {
            clearError('email');
        }
    });

    passwordInput.addEventListener('input', () => {
        if (passwordInput.parentElement.classList.contains('error')) {
            clearError('password');
        }
    });
}
/* =========================================
   PASSWORD TOGGLE
   ========================================= */
function initPasswordToggle() {
    const toggleBtn = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');

    if (!toggleBtn || !passwordInput) return;

    toggleBtn.addEventListener('click', () => {
        const type = passwordInput.type === 'password' ? 'text' : 'password';
        passwordInput.type = type;

        const icon = toggleBtn.querySelector('i');
        if (type === 'text') {
            icon.classList.remove('fa-eye');
            icon.classList.add('fa-eye-slash');
        } else {
            icon.classList.remove('fa-eye-slash');
            icon.classList.add('fa-eye');
        }
    });
}

/* =========================================
   VALIDATION HELPERS
   ========================================= */
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function showError(fieldName, message) {
    const input = document.getElementById(fieldName);
    const errorElement = document.getElementById(`${fieldName}Error`);
    const formGroup = input.closest('.form-group');

    formGroup.classList.add('error');
    errorElement.textContent = message;
    errorElement.classList.add('show');
}

function clearError(fieldName) {
    const input = document.getElementById(fieldName);
    const errorElement = document.getElementById(`${fieldName}Error`);
    const formGroup = input.closest('.form-group');

    formGroup.classList.remove('error');
    errorElement.textContent = '';
    errorElement.classList.remove('show');
}

function clearErrors() {
    const errorMessages = document.querySelectorAll('.error-message');
    const formGroups = document.querySelectorAll('.form-group');

    errorMessages.forEach(msg => {
        msg.textContent = '';
        msg.classList.remove('show');
    });

    formGroups.forEach(group => {
        group.classList.remove('error');
    });
}

function showAlert(message, type) {
    const alertMessage = document.getElementById('alertMessage');
    alertMessage.textContent = message;
    alertMessage.className = `alert-message ${type}`;
    alertMessage.style.display = 'block';
}

function hideAlert() {
    const alertMessage = document.getElementById('alertMessage');
    alertMessage.style.display = 'none';
    alertMessage.className = 'alert-message';
}

/* =========================================
   REMEMBER ME FUNCTIONALITY
   ========================================= */
document.addEventListener('DOMContentLoaded', () => {
    const rememberMeCheckbox = document.getElementById('rememberMe');
    const emailInput = document.getElementById('email');

    // Load saved email if exists
    const savedEmail = localStorage.getItem('rememberedEmail');
    if (savedEmail) {
        emailInput.value = savedEmail;
        rememberMeCheckbox.checked = true;
    }

    // Save email on form submit if remember me is checked
    const form = document.getElementById('loginForm');
    form.addEventListener('submit', () => {
        if (rememberMeCheckbox.checked) {
            localStorage.setItem('rememberedEmail', emailInput.value.trim());
        } else {
            localStorage.removeItem('rememberedEmail');
        }
    });
});
