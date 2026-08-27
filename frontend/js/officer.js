/**
 * Jadara Clearance System — Department Officer Dashboard Logic
 */

let pendingApprovals = [];
let currentSelectedApproval = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = checkPageAccess('DepartmentOfficer');
    if (!user) return;

    // Header Setup
    document.getElementById('userName').textContent = user.fullName;
    document.getElementById('userRole').textContent = `Department Officer`;
    document.getElementById('logoutBtn').addEventListener('click', logout);

    // Modal elements
    setupModalEvents();

    // Initial Load
    await loadPendingApprovals();

    document.getElementById('refreshBtn')?.addEventListener('click', async () => {
        await loadPendingApprovals();
        showToast('Pending approvals refreshed.');
    });
});

async function loadPendingApprovals() {
    const tableBody = document.getElementById('pendingTableBody');
    const emptyState = document.getElementById('emptyState');
    if (!tableBody) return;

    try {
        pendingApprovals = await apiFetch('/clearance/department-pending');
        
        if (!pendingApprovals || pendingApprovals.length === 0) {
            tableBody.innerHTML = '';
            if (emptyState) emptyState.style.display = 'block';
            return;
        }

        if (emptyState) emptyState.style.display = 'none';

        tableBody.innerHTML = pendingApprovals.map(item => `
            <tr id="approval-row-${item.approvalId}">
                <td>
                    <div style="font-weight:600;">${escapeHtml(item.studentFullName)}</div>
                </td>
                <td><code>${escapeHtml(item.studentUniversityId)}</code></td>
                <td>Approval #${item.approvalId} (Req #${item.requestId})</td>
                <td>
                    <span class="badge badge-pending">Pending</span>
                </td>
                <td>
                    <button class="btn btn-primary btn-sm" onclick="openReviewModal(${item.approvalId})">
                        Review Request
                    </button>
                </td>
            </tr>
        `).join('');

    } catch (err) {
        showToast(err.message || 'Failed to load pending approvals.');
    }
}

function openReviewModal(approvalId) {
    currentSelectedApproval = pendingApprovals.find(a => a.approvalId === approvalId);
    if (!currentSelectedApproval) return;

    document.getElementById('modalStudentName').textContent = currentSelectedApproval.studentFullName;
    document.getElementById('modalUniversityId').textContent = currentSelectedApproval.studentUniversityId;
    
    // Reset Form Fields
    document.getElementById('statusSelect').value = 'Approved';
    document.getElementById('rejectionReasonGroup').style.display = 'none';
    document.getElementById('rejectionReason').value = '';
    document.getElementById('fineAmount').value = '';
    document.getElementById('modalAlert').style.display = 'none';

    document.getElementById('reviewModal').classList.add('active');
}

function setupModalEvents() {
    const modal = document.getElementById('reviewModal');
    const closeBtn = document.getElementById('modalCloseBtn');
    const cancelBtn = document.getElementById('modalCancelBtn');
    const statusSelect = document.getElementById('statusSelect');
    const form = document.getElementById('reviewForm');

    const closeModal = () => modal.classList.remove('active');

    closeBtn?.addEventListener('click', closeModal);
    cancelBtn?.addEventListener('click', closeModal);

    statusSelect?.addEventListener('change', (e) => {
        const rejectionGroup = document.getElementById('rejectionReasonGroup');
        if (e.target.value === 'Rejected') {
            rejectionGroup.style.display = 'block';
        } else {
            rejectionGroup.style.display = 'none';
        }
    });

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!currentSelectedApproval) return;

        const status = statusSelect.value;
        const rejectionReason = document.getElementById('rejectionReason').value.trim();
        const fineAmountVal = document.getElementById('fineAmount').value;
        const submitBtn = document.getElementById('modalSubmitBtn');
        const modalAlert = document.getElementById('modalAlert');

        if (status === 'Rejected' && !rejectionReason) {
            modalAlert.textContent = 'Please specify a rejection reason.';
            modalAlert.style.display = 'block';
            return;
        }

        modalAlert.style.display = 'none';
        submitBtn.disabled = true;
        submitBtn.innerHTML = `<span class="spinner"></span> Updating...`;

        const body = {
            status,
            rejectionReason: status === 'Rejected' ? rejectionReason : null,
            fineAmount: fineAmountVal !== '' ? parseFloat(fineAmountVal) : null
        };

        try {
            await apiFetch(`/clearance/approval/${currentSelectedApproval.approvalId}`, {
                method: 'PUT',
                body
            });

            showToast(`Approval #${currentSelectedApproval.approvalId} marked as ${status}.`);
            modal.classList.remove('active');

            // Remove from UI table
            pendingApprovals = pendingApprovals.filter(a => a.approvalId !== currentSelectedApproval.approvalId);
            document.getElementById(`approval-row-${currentSelectedApproval.approvalId}`)?.remove();

            if (pendingApprovals.length === 0) {
                document.getElementById('emptyState').style.display = 'block';
            }

        } catch (err) {
            modalAlert.textContent = err.message || 'Failed to update approval status.';
            modalAlert.style.display = 'block';
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Submit Decision';
        }
    });
}

function escapeHtml(str) {
    return (str || '').replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
