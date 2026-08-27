/**
 * Jadara Clearance System — Student Dashboard Logic
 */

let pollInterval = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = checkPageAccess('Student');
    if (!user) return;

    // Header Setup
    document.getElementById('userName').textContent = user.fullName;
    document.getElementById('logoutBtn').addEventListener('click', logout);

    // Initial Load & Refresh
    await loadStudentClearanceRequest();

    document.getElementById('refreshBtn')?.addEventListener('click', async () => {
        await loadStudentClearanceRequest();
        showToast('Request status refreshed.');
    });

    // Auto Poll every 30 seconds
    pollInterval = setInterval(loadStudentClearanceRequest, 30000);
});

async function loadStudentClearanceRequest() {
    const contentArea = document.getElementById('dashboardContent');
    if (!contentArea) return;

    try {
        const requestData = await apiFetch('/clearance/my-request');
        
        if (!requestData) {
            renderNoRequestState(contentArea);
            return;
        }

        renderRequestProgress(contentArea, requestData);

    } catch (err) {
        if (err.status === 404) {
            renderNoRequestState(contentArea);
        } else {
            contentArea.innerHTML = `
                <div class="alert alert-danger" style="display:block">
                    Failed to load clearance status: ${err.message}
                </div>
            `;
        }
    }
}

function renderNoRequestState(container) {
    container.innerHTML = `
        <div class="card" style="text-align: center; padding: 3rem 1.5rem;">
            <h2 style="font-size: 1.5rem; margin-bottom: 0.5rem; color: var(--color-primary);">No Active Clearance Request</h2>
            <p style="color: var(--color-text-muted); margin-bottom: 1.5rem;">
                You have not initiated a clearance request yet. Click below to begin the clearance process across all departments.
            </p>
            <button id="startRequestBtn" class="btn btn-primary" style="margin: 0 auto;">
                Start Clearance Request
            </button>
        </div>
    `;

    document.getElementById('startRequestBtn').addEventListener('click', handleCreateRequest);
}

async function handleCreateRequest(e) {
    const btn = e.target;
    btn.disabled = true;
    btn.innerHTML = `<span class="spinner"></span> Submitting...`;

    try {
        await apiFetch('/clearance/request', { method: 'POST' });
        showToast('Clearance request initiated successfully!');
        await loadStudentClearanceRequest();
    } catch (err) {
        showToast(err.message || 'Failed to submit clearance request.');
        btn.disabled = false;
        btn.textContent = 'Start Clearance Request';
    }
}

function renderRequestProgress(container, data) {
    const overallBadgeClass = getBadgeClass(data.overallStatus);
    const formattedDate = new Date(data.requestDate).toLocaleString();
    const completedDate = data.completedAt ? new Date(data.completedAt).toLocaleString() : null;

    let certificateHtml = '';
    if (data.overallStatus === 'Completed') {
        certificateHtml = `
            <div class="card" style="background: var(--color-success-bg); border-color: #bbf7d0; margin-top: 1.5rem;">
                <div style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 1rem;">
                    <div>
                        <h3 style="color: var(--color-success); font-size: 1.1rem; margin-bottom: 0.25rem;">
                            ✓ Clearance Certificate Issued
                        </h3>
                        <p style="font-size: 0.85rem; color: var(--color-text-muted);">
                            Certificate Verification Hash: <code style="background:#fff; padding:2px 6px; border-radius:4px;">${data.certificateHash || 'N/A'}</code>
                        </p>
                    </div>
                    <button class="btn btn-success" onclick="alert('Downloading Certificate: Verification Hash ${data.certificateHash}')">
                        Download Certificate
                    </button>
                </div>
            </div>
        `;
    }

    let approvalsHtml = (data.approvals || []).map(app => {
        const badgeClass = getBadgeClass(app.status);
        const updateTime = app.updatedAt ? new Date(app.updatedAt).toLocaleString() : 'Pending review';
        
        let fineDetails = '';
        if (app.fineAmount && app.fineAmount > 0) {
            const paidBadge = app.isPaid 
                ? '<span class="badge badge-success">Paid</span>' 
                : '<span class="badge badge-danger">Unpaid</span>';
            fineDetails = `<div style="font-size: 0.85rem; margin-top: 0.25rem;">Fine: <strong>$${app.fineAmount.toFixed(2)}</strong> ${paidBadge}</div>`;
        }

        let rejectionHtml = '';
        if (app.status === 'Rejected' && app.rejectionReason) {
            rejectionHtml = `<div style="color: var(--color-danger); font-size: 0.85rem; margin-top: 0.25rem;">Reason: "${app.rejectionReason}"</div>`;
        }

        return `
            <div class="step-card">
                <div class="step-info">
                    <div class="step-dept">${escapeHtml(app.departmentName)}</div>
                    <div class="step-meta">Last Updated: ${updateTime}</div>
                    ${rejectionHtml}
                    ${fineDetails}
                </div>
                <div>
                    <span class="badge ${badgeClass}">${app.status}</span>
                </div>
            </div>
        `;
    }).join('');

    container.innerHTML = `
        <div class="card">
            <div class="card-title">
                <div>Clearance Request #${data.id}</div>
                <span class="badge ${overallBadgeClass}">${data.overallStatus}</span>
            </div>
            <div style="font-size: 0.9rem; color: var(--color-text-muted); margin-bottom: 1.5rem;">
                Submitted on: ${formattedDate} ${completedDate ? `| Completed on: ${completedDate}` : ''}
            </div>

            <h3 style="font-size: 1rem; font-weight: 600; margin-bottom: 1rem;">Department Approvals</h3>
            <div class="stepper">
                ${approvalsHtml}
            </div>
        </div>
        ${certificateHtml}
    `;
}

function getBadgeClass(status) {
    switch (status) {
        case 'Approved':
        case 'Completed':
            return 'badge-completed';
        case 'Rejected':
            return 'badge-rejected';
        case 'Pending':
        default:
            return 'badge-pending';
    }
}

function escapeHtml(str) {
    return (str || '').replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
