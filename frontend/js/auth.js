/**
 * Jadara Clearance System — Auth Form Logic (Login & Register)
 */

document.addEventListener('DOMContentLoaded', () => {
    // -------------------------------------------------------------
    // Login Form Handling (index.html)
    // -------------------------------------------------------------
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        checkPageAccess(); // Redirect if already logged in

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const emailInput = document.getElementById('email');
            const passwordInput = document.getElementById('password');
            const submitBtn = document.getElementById('loginSubmitBtn');
            const errorAlert = document.getElementById('loginAlert');

            // Reset errors
            errorAlert.style.display = 'none';
            emailInput.classList.remove('is-invalid');
            passwordInput.classList.remove('is-invalid');

            let isValid = true;
            if (!emailInput.value.trim() || !validateEmail(emailInput.value.trim())) {
                emailInput.classList.add('is-invalid');
                isValid = false;
            }
            if (!passwordInput.value.trim()) {
                passwordInput.classList.add('is-invalid');
                isValid = false;
            }

            if (!isValid) return;

            // Loading state
            setButtonLoading(submitBtn, true, 'Signing in...');

            try {
                const response = await apiFetch('/auth/login', {
                    method: 'POST',
                    body: {
                        email: emailInput.value.trim(),
                        password: passwordInput.value
                    },
                    auth: false
                });

                // Store session
                const authData = {
                    token: response.token,
                    role: response.role,
                    userId: response.userId,
                    fullName: response.fullName,
                    departmentId: response.departmentId,
                    expiresAt: response.expiresAt
                };
                localStorage.setItem('jadara_auth', JSON.stringify(authData));

                showToast('Login successful! Redirecting...');
                setTimeout(() => {
                    redirectToRoleDashboard(response.role);
                }, 400);

            } catch (err) {
                showErrorAlert(errorAlert, err.message || 'Login failed. Please check your credentials.');
            } finally {
                setButtonLoading(submitBtn, false, 'Sign In');
            }
        });
    }

    // -------------------------------------------------------------
    // Register Form Handling (register.html)
    // -------------------------------------------------------------
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        checkPageAccess(); // Redirect if already logged in

        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const fullNameInput = document.getElementById('fullName');
            const emailInput = document.getElementById('email');
            const universityIdInput = document.getElementById('universityId');
            const passwordInput = document.getElementById('password');
            const confirmPasswordInput = document.getElementById('confirmPassword');
            const submitBtn = document.getElementById('registerSubmitBtn');
            const errorAlert = document.getElementById('registerAlert');

            // Reset validation UI
            errorAlert.style.display = 'none';
            [fullNameInput, emailInput, universityIdInput, passwordInput, confirmPasswordInput].forEach(el => {
                el.classList.remove('is-invalid');
            });

            let isValid = true;
            if (!fullNameInput.value.trim()) {
                fullNameInput.classList.add('is-invalid');
                isValid = false;
            }
            if (!emailInput.value.trim() || !validateEmail(emailInput.value.trim())) {
                emailInput.classList.add('is-invalid');
                isValid = false;
            }
            if (!universityIdInput.value.trim()) {
                universityIdInput.classList.add('is-invalid');
                isValid = false;
            }
            if (!passwordInput.value || passwordInput.value.length < 6) {
                passwordInput.classList.add('is-invalid');
                isValid = false;
            }
            if (passwordInput.value !== confirmPasswordInput.value) {
                confirmPasswordInput.classList.add('is-invalid');
                isValid = false;
            }

            if (!isValid) return;

            setButtonLoading(submitBtn, true, 'Registering...');

            try {
                const response = await apiFetch('/auth/register', {
                    method: 'POST',
                    body: {
                        fullName: fullNameInput.value.trim(),
                        email: emailInput.value.trim(),
                        password: passwordInput.value,
                        universityId: universityIdInput.value.trim()
                    },
                    auth: false
                });

                // Auto login on successful registration
                const authData = {
                    token: response.token,
                    role: response.role,
                    userId: response.userId,
                    fullName: response.fullName,
                    departmentId: response.departmentId,
                    expiresAt: response.expiresAt
                };
                localStorage.setItem('jadara_auth', JSON.stringify(authData));

                showToast('Registration successful! Redirecting to dashboard...');
                setTimeout(() => {
                    window.location.href = 'student-dashboard.html';
                }, 500);

            } catch (err) {
                showErrorAlert(errorAlert, err.message || 'Registration failed. Please try again.');
            } finally {
                setButtonLoading(submitBtn, false, 'Register Account');
            }
        });
    }
});

// Helper Functions
function validateEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function setButtonLoading(btn, isLoading, defaultText) {
    if (!btn) return;
    if (isLoading) {
        btn.disabled = true;
        btn.innerHTML = `<span class="spinner"></span> ${defaultText}`;
    } else {
        btn.disabled = false;
        btn.textContent = defaultText;
    }
}

function showErrorAlert(alertElem, message) {
    if (!alertElem) return;
    alertElem.textContent = message;
    alertElem.style.display = 'block';
}
