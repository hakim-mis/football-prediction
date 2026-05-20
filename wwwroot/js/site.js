document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            if (window.bootstrap) {
                const instance = bootstrap.Alert.getOrCreateInstance(alert);
                instance.close();
            }
        }, 7000);
    });
});
