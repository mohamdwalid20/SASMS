// SweetAlert2 Global Helpers

// Confirm Action (Delete, specialized actions)
function confirmAction(e, element, title = null, text = null, icon = null, confirmButtonText = null) {
    e.preventDefault(); // Stop the immediate action

    // Fallback to data attributes if arguments are not provided
    title = title || element.getAttribute('data-confirm-title') || "Are you sure?";
    text = text || element.getAttribute('data-confirm-text') || "You won't be able to revert this!";
    icon = icon || element.getAttribute('data-confirm-icon') || "warning";
    confirmButtonText = confirmButtonText || element.getAttribute('data-confirm-yes') || "Yes, proceed!";
    const cancelButtonText = element.getAttribute('data-confirm-no') || "Cancel";

    Swal.fire({
        title: title,
        text: text,
        icon: icon,
        showCancelButton: true,
        confirmButtonColor: '#4D44B5', // Primary Color
        cancelButtonColor: '#d33',
        confirmButtonText: confirmButtonText,
        cancelButtonText: cancelButtonText
    }).then((result) => {
        if (result.isConfirmed) {
            // Handle different element types
            if (element.tagName === 'FORM') {
                element.submit();
            } else if (element.tagName === 'A') {
                window.location.href = element.href;
            } else if (element.type === 'submit' || element.tagName === 'BUTTON') {
                // If clicked on a submit button, find the parent form
                const form = element.closest('form');
                if (form) {
                    // Create a hidden input to simulate the button click (in case the button name/value is needed)
                    if (element.name && element.value) {
                        const input = document.createElement('input');
                        input.type = 'hidden';
                        input.name = element.name;
                        input.value = element.value;
                        form.appendChild(input);
                    }
                    form.submit();
                }
            }
        }
    });
}

// Simple Alert replacement
function showAlert(title, text, icon = "success") {
    Swal.fire({
        title: title,
        text: text,
        icon: icon,
        confirmButtonColor: '#4D44B5'
    });
}

// Toast Notification (Top Right)
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer)
        toast.addEventListener('mouseleave', Swal.resumeTimer)
    }
});

function showToast(title, icon = 'success') {
    Toast.fire({
        icon: icon,
        title: title
    });
}
