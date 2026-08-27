/**
 * Jadara Clearance System — Route Guard & Auth Utility
 */

function getAuthUser() {
    const raw = localStorage.getItem('jadara_auth');
    if (!raw) return null;
    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

function checkPageAccess(requiredRole = null) {
    const user = getAuthUser();
    const currentPath = window.location.pathname.toLowerCase();

    const isPublicPage = currentPath.endsWith('index.html') || 
                         currentPath.endsWith('register.html') || 
                         currentPath === '/' || 
                         currentPath.endsWith('/');

    if (!user || !user.token) {
        // Not authenticated: redirect to login if on protected page
        if (!isPublicPage) {
            window.location.href = 'index.html';
        }
        return null;
    }

    // Authenticated user accessing login/register -> redirect to role dashboard
    if (isPublicPage) {
        redirectToRoleDashboard(user.role);
        return user;
    }

    // Role specific enforcement
    if (requiredRole && user.role !== requiredRole) {
        redirectToRoleDashboard(user.role);
        return null;
    }

    return user;
}

function redirectToRoleDashboard(role) {
    switch (role) {
        case 'Student':
            if (!window.location.pathname.endsWith('student-dashboard.html')) {
                window.location.href = 'student-dashboard.html';
            }
            break;
        case 'DepartmentOfficer':
            if (!window.location.pathname.endsWith('officer-dashboard.html')) {
                window.location.href = 'officer-dashboard.html';
            }
            break;
        case 'Admin':
            if (!window.location.pathname.endsWith('admin-audit.html')) {
                window.location.href = 'admin-audit.html';
            }
            break;
        default:
            window.location.href = 'index.html';
            break;
    }
}

function logout() {
    localStorage.removeItem('jadara_auth');
    window.location.href = 'index.html';
}
