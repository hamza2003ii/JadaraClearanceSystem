/**
 * Jadara Clearance System — Administrator Audit Logs Logic
 */

let currentPage = 1;
const pageSize = 15;

document.addEventListener('DOMContentLoaded', async () => {
    const user = checkPageAccess('Admin');
    if (!user) return;

    // Header Setup
    document.getElementById('userName').textContent = user.fullName;
    document.getElementById('logoutBtn').addEventListener('click', logout);

    // Initial Load
    await loadAuditLogs();

    // Filter Form Handlers
    document.getElementById('filterForm')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        currentPage = 1;
        await loadAuditLogs();
    });

    document.getElementById('resetFiltersBtn')?.addEventListener('click', async () => {
        document.getElementById('requestIdFilter').value = '';
        document.getElementById('userIdFilter').value = '';
        document.getElementById('fromDateFilter').value = '';
        document.getElementById('toDateFilter').value = '';
        currentPage = 1;
        await loadAuditLogs();
    });

    // Pagination Controls
    document.getElementById('prevPageBtn')?.addEventListener('click', async () => {
        if (currentPage > 1) {
            currentPage--;
            await loadAuditLogs();
        }
    });

    document.getElementById('nextPageBtn')?.addEventListener('click', async () => {
        currentPage++;
        await loadAuditLogs();
    });
});

async function loadAuditLogs() {
    const tableBody = document.getElementById('auditTableBody');
    const emptyState = document.getElementById('emptyState');
    const pageIndicator = document.getElementById('pageIndicator');
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');

    if (!tableBody) return;

    const requestId = document.getElementById('requestIdFilter')?.value;
    const userId = document.getElementById('userIdFilter')?.value;
    const fromDate = document.getElementById('fromDateFilter')?.value;
    const toDate = document.getElementById('toDateFilter')?.value;

    const queryParams = new URLSearchParams({
        page: currentPage,
        pageSize
    });

    if (requestId) queryParams.append('requestId', requestId);
    if (userId) queryParams.append('userId', userId);
    if (fromDate) queryParams.append('fromDate', fromDate);
    if (toDate) queryParams.append('toDate', toDate);

    try {
        const logs = await apiFetch(`/audit/logs?${queryParams.toString()}`);
        
        pageIndicator.textContent = `Page ${currentPage}`;
        prevBtn.disabled = currentPage === 1;

        if (!logs || logs.length === 0) {
            tableBody.innerHTML = '';
            emptyState.style.display = 'block';
            nextBtn.disabled = true;
            return;
        }

        emptyState.style.display = 'none';
        nextBtn.disabled = logs.length < pageSize;

        tableBody.innerHTML = logs.map(log => `
            <tr>
                <td><small>${new Date(log.timestamp).toLocaleString()}</small></td>
                <td><span class="badge ${getActionBadgeClass(log.actionType)}">${escapeHtml(log.actionType)}</span></td>
                <td>${escapeHtml(log.description)}</td>
                <td>${escapeHtml(log.actionByUserName)} (ID: ${log.actionByUserId})</td>
                <td>${log.requestId ? `#${log.requestId}` : 'N/A'}</td>
            </tr>
        `).join('');

    } catch (err) {
        showToast(err.message || 'Failed to load audit logs.');
    }
}

function getActionBadgeClass(actionType) {
    if (actionType.toLowerCase().includes('approved') || actionType.toLowerCase().includes('completed')) {
        return 'badge-success';
    }
    if (actionType.toLowerCase().includes('rejected')) {
        return 'badge-danger';
    }
    return 'badge-warning';
}

function escapeHtml(str) {
    return (str || '').replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
