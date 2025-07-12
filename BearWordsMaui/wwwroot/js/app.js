// Nav collapse
window.collapseNavbar = () => {
    const navbarCollapse = document.querySelector('.navbar-collapse');
    if (navbarCollapse && navbarCollapse.classList.contains('show')) {
        const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
        bsCollapse.hide();
    }
};


// App theme
window.applyTheme = (theme) => {
    const html = document.documentElement;

    if (theme === 'dark') {
        html.setAttribute('data-bs-theme', 'dark');
    } else if (theme === 'light') {
        html.setAttribute('data-bs-theme', 'light');
    } else { // auto
        // Remove attribute to use system preference
        html.removeAttribute('data-bs-theme');

        // Or detect system preference:
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        html.setAttribute('data-bs-theme', prefersDark ? 'dark' : 'light');
    }
};

let mediaQueryList = null;
let currentTheme = null;

window.initializeThemeListener = (theme) => {
    currentTheme = theme;

    // Only set up listener if theme is 'unspecified'
    if (theme === 'unspecified') {
        setupSystemThemeListener();
    } else {
        removeSystemThemeListener();
    }
};

function setupSystemThemeListener() {
    // Clean up existing listener
    if (mediaQueryList) {
        mediaQueryList.removeEventListener('change', handleSystemThemeChange);
    }

    // Create media query listener for system theme changes
    mediaQueryList = window.matchMedia('(prefers-color-scheme: dark)');
    mediaQueryList.addEventListener('change', handleSystemThemeChange);
}

function removeSystemThemeListener() {
    if (mediaQueryList) {
        mediaQueryList.removeEventListener('change', handleSystemThemeChange);
        mediaQueryList = null;
    }
}

function handleSystemThemeChange(e) {
    // Only apply if current theme is still 'unspecified'
    if (currentTheme === 'unspecified') {
        const html = document.documentElement;
        html.setAttribute('data-bs-theme', e.matches ? 'dark' : 'light');
    }
}


// Initialize tooltips
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});


// Navi helper
function goBack() {
    window.history.back();
}


// Scroll helpers
function saveScrollBeforeBlazorNav() {
    const scrollY = window.scrollY;
    const key = 'scroll-pos-' + window.location.pathname;
    sessionStorage.setItem(key, scrollY);
}

function restoreScrollFromSession() {
    const key = 'scroll-pos-' + window.location.pathname;
    const scrollY = sessionStorage.getItem(key);
    if (scrollY !== null) {
        sessionStorage.removeItem(key);
        setTimeout(() => {
            window.scrollTo(0, parseInt(scrollY));
        }, 2);
    }
}

function clearAllScrollPositions() {
    const prefix = 'scroll-pos-';
    for (let i = sessionStorage.length - 1; i >= 0; i--) {
        const key = sessionStorage.key(i);
        if (key && key.startsWith(prefix)) {
            sessionStorage.removeItem(key);
        }
    }
}