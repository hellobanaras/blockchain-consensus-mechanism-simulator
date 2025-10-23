// Minimal Blazor debugging script
(function() {
    'use strict';

    console.log('Blazor Debug Script Loaded');

    // Add error boundary for JavaScript errors
    window.addEventListener('error', function(e) {
        console.error('JavaScript error detected:', e.message, e.filename, e.lineno);
    });

    // Add unhandled promise rejection handler
    window.addEventListener('unhandledrejection', function(e) {
        console.error('Unhandled promise rejection:', e.reason);
    });

    console.log('Blazor debug script setup complete');
})();