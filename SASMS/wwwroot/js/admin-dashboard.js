$(document).ready(function () {
    // Sidebar Toggle
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
    });

    // Count up animation for stats
    $('.stat-value').each(function () {
        var $this = $(this);
        var countTo = $this.text(); // Get the number

        // Only animate if it's a number
        if (!isNaN(countTo) && countTo.trim() !== '') {
            jQuery({ Counter: 0 }).animate({ Counter: countTo }, {
                duration: 1000,
                easing: 'swing',
                step: function () {
                    $this.text(Math.ceil(this.Counter));
                }
            });
        }
    });
});
