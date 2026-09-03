/**
 * Jadara Clearance System — Theme Switcher & Accessibility Helper
 * Manages Dark Mode / Light Mode with localStorage persistence.
 */

(function () {
    const THEME_KEY = 'jadara_theme';

    // 1. Detect and apply theme immediately before DOM renders to prevent FOUC
    function getPreferredTheme() {
        const savedTheme = localStorage.getItem(THEME_KEY);
        if (savedTheme) {
            return savedTheme;
        }
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        updateThemeToggleIcons(theme);
    }

    // Apply initial theme
    const currentTheme = getPreferredTheme();
    applyTheme(currentTheme);

    // Update toggle buttons across pages
    function updateThemeToggleIcons(theme) {
        const toggleButtons = document.querySelectorAll('.theme-toggle-btn');
        toggleButtons.forEach(btn => {
            if (theme === 'dark') {
                btn.innerHTML = `
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="5"></circle>
                        <line x1="12" y1="1" x2="12" y2="3"></line>
                        <line x1="12" y1="21" x2="12" y2="23"></line>
                        <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
                        <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
                        <line x1="1" y1="12" x2="3" y2="12"></line>
                        <line x1="21" y1="12" x2="23" y2="12"></line>
                        <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
                        <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
                    </svg>
                    <span>الوضع الفاتح</span>
                `;
                btn.title = "التبديل إلى الوضع الفاتح (Light Mode)";
            } else {
                btn.innerHTML = `
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
                    </svg>
                    <span>الوضع الداكن</span>
                `;
                btn.title = "التبديل إلى الوضع الداكن (Dark Mode)";
            }
        });
    }

    // Global toggle function
    window.toggleTheme = function () {
        const activeTheme = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(activeTheme);
    };

    // Attach listener when DOM loads
    document.addEventListener('DOMContentLoaded', () => {
        updateThemeToggleIcons(document.documentElement.getAttribute('data-theme') || 'light');
        
        // Listen to system preference changes if user hasn't chosen a manual theme
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            if (!localStorage.getItem(THEME_KEY)) {
                applyTheme(e.matches ? 'dark' : 'light');
            }
        });
    });
})();
