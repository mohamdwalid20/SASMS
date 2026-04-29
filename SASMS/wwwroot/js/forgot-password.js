/**
 * EVA IATS - Forgot Password Page JavaScript
 * Handles password reset request submission to admin
 */

document.addEventListener('DOMContentLoaded', () => {
    initForgotPasswordForm();
});

/* =========================================
   FORGOT PASSWORD FORM HANDLING
   ========================================= */
function initForgotPasswordForm() {
    const form = document.getElementById('forgotPasswordForm');
    const emailInput = document.getElementById('email');
    const reasonInput = document.getElementById('reason');
    const submitBtn = document.getElementById('submitBtn');
    const btnText = submitBtn.querySelector('.btn-text');
    const btnLoader = submitBtn.querySelector('.btn-loader');
    const alertMessage = document.getElementById('alertMessage');
    const successState = document.getElementById('successState');
    const infoBox = document.getElementById('infoBox');

    // Form submission
    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // Clear previous errors
        clearErrors();
        hideAlert();

        // Validate email
        const email = emailInput.value.trim();
        const reason = reasonInput.value.trim();

        let isValid = true;

        // Email validation
        if (!email) {
            showError('email', 'Email address is required');
            isValid = false;
        } else if (!isValidEmail(email)) {
            showError('email', 'Please enter a valid email address');
            isValid = false;
        }

        if (!isValid) return;

        // Show loading state
        submitBtn.disabled = true;
        btnText.innerText = 'Verifying...';
        btnLoader.style.display = 'inline-block';

        // Send reset request to admin
        try {
            await sendResetRequestToAdmin(email, reason);

            // Hide form and show success state
            document.getElementById('formHeader').style.display = 'none';
            form.style.display = 'none';
            infoBox.style.display = 'none';
            successState.style.display = 'block';

        } catch (error) {
            // Show error
            showAlert(error.message, 'error');

            // Reset button
            submitBtn.disabled = false;
            btnText.innerText = 'Send Reset Request';
            btnLoader.style.display = 'none';
        }
    });

    // Real-time email validation
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
   SEND RESET REQUEST TO ADMIN
   ========================================= */
async function sendResetRequestToAdmin(email, reason) {
    const formData = new URLSearchParams();
    formData.append('email', email);

    // CSRF token MUST be in the body for [ValidateAntiForgeryToken] by default
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    const response = await fetch('/Account/ForgotPassword', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        },
        body: formData.toString()
    });

    let data;
    const responseText = await response.text();

    try {
        data = JSON.parse(responseText);
    } catch (e) {
        console.error('[Forgot Password] Server returned non-JSON response:', responseText);
        throw new Error('Server error: Database not ready or invalid request. Please check if your email exists.');
    }

    if (!response.ok || data.success === false) {
        throw new Error(data.message || 'Operation failed. Please verify your email.');
    }

    return { success: true };
}

/* =========================================
   HELPER FUNCTIONS
   ========================================= */
function generateRequestId() {
    return 'REQ-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9).toUpperCase();
}

function storeResetRequest(request) {
    // Get existing requests
    let requests = JSON.parse(localStorage.getItem('passwordResetRequests') || '[]');

    // Add new request
    requests.push(request);

    // Store back
    localStorage.setItem('passwordResetRequests', JSON.stringify(requests));

    // Also store the latest request separately
    localStorage.setItem('latestResetRequest', JSON.stringify(request));
}

/* =========================================
   ADMIN NOTIFICATION SYSTEM (DEMO)
   ========================================= */
// This function demonstrates how admin would be notified
// In production, replace with actual email/notification service
function notifyAdmin(notification) {
    // Example integration points:

    // 1. Email Service (e.g., SendGrid, AWS SES)
    // sendEmail({
    //     to: 'admin@eva-iats.edu',
    //     subject: 'Password Reset Request',
    //     body: `User ${notification.userEmail} has requested a password reset...`
    // });

    // 2. SMS Service (e.g., Twilio)
    // sendSMS({
    //     to: '+1234567890',
    //     message: `New password reset request from ${notification.userEmail}`
    // });

    // 3. Slack/Discord Webhook
    // sendWebhook({
    //     url: 'https://hooks.slack.com/...',
    //     message: `Password reset request: ${notification.userEmail}`
    // });

    // 4. Database Storage
    // saveToDatabase(notification);

    console.log('Admin notification sent:', notification);
}

/* =========================================
   RETRIEVE STORED REQUESTS (FOR ADMIN)
   ========================================= */
// Admin can call this function to view all pending requests
function getPasswordResetRequests() {
    const requests = JSON.parse(localStorage.getItem('passwordResetRequests') || '[]');
    console.table(requests);
    return requests;
}

// Make function available globally for admin console access
window.getPasswordResetRequests = getPasswordResetRequests;

/* =========================================
   AUTO-FILL FOR TESTING (DEV ONLY)
   ========================================= */
// Remove this in production
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    console.log('%c[DEV MODE] Forgot Password Page Loaded', 'color: #F77F00; font-weight: bold;');
    console.log('%cTest email: student@eva-iats.edu', 'color: #6B7784;');
    console.log('%cTo view all reset requests, run: getPasswordResetRequests()', 'color: #6B7784;');
}
