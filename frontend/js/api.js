/**
 * Jadara Clearance System — API Client Utility
 */

class ApiError extends Error {
    constructor(status, message, errors = null) {
        super(message);
        this.status = status;
        this.errors = errors;
    }
}

/**
 * Perform authenticated or unauthenticated API fetch requests.
 * @param {string} path - Endpoint path starting with '/'
 * @param {Object} options - Options: method, body, auth (boolean)
 */
async function apiFetch(path, { method = 'GET', body = null, auth = true } = {}) {
    const headers = {
        'Content-Type': 'application/json'
    };

    if (auth) {
        const storedAuth = localStorage.getItem('jadara_auth');
        if (storedAuth) {
            try {
                const parsed = JSON.parse(storedAuth);
                if (parsed && parsed.token) {
                    headers['Authorization'] = `Bearer ${parsed.token}`;
                }
            } catch (e) {
                console.error("Failed to parse stored auth token", e);
            }
        }
    }

    const fetchOptions = {
        method,
        headers
    };

    if (body) {
        fetchOptions.body = JSON.stringify(body);
    }

    try {
        const cleanPath = path.startsWith('/') ? path : '/' + path;
        const targetUrl = `${API_BASE_URL}${cleanPath}`;
        console.log(`[API Request] ${method} ${targetUrl}`);
        const response = await fetch(targetUrl, fetchOptions);
        
        let jsonResponse = null;
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            jsonResponse = await response.json();
        }

        if (!response.ok) {
            // Unauthenticated - token expired or invalid
            if (response.status === 401 && auth) {
                localStorage.removeItem('jadara_auth');
                if (!window.location.pathname.endsWith('index.html') && window.location.pathname !== '/') {
                    window.location.href = 'index.html';
                }
            }

            const errorMessage = (jsonResponse && jsonResponse.message) 
                ? jsonResponse.message 
                : `HTTP Error ${response.status}: ${response.statusText}`;

            const errorDetails = (jsonResponse && jsonResponse.errors) ? jsonResponse.errors : null;
            throw new ApiError(response.status, errorMessage, errorDetails);
        }

        // Return extracted data if wrapped in standard ApiResponse envelope
        if (jsonResponse && typeof jsonResponse.success !== 'undefined') {
            return jsonResponse.data !== undefined ? jsonResponse.data : jsonResponse;
        }

        return jsonResponse;
    } catch (err) {
        if (err instanceof ApiError) {
            throw err;
        }
        throw new ApiError(0, err.message || 'Network error or server unavailable.');
    }
}

/**
 * Global helper to show UI toast notifications
 */
function showToast(message, duration = 3000) {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, duration);
}
