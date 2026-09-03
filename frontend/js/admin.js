/**
 * Jadara Clearance System — Administrator Audit Logs Logic
 */

let currentPage = 1;
const pageSize = 15;

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const user = (typeof checkPageAccess === 'function') ? checkPageAccess('Admin') : null;
        if (!user) return;

        // Header Setup (defensive)
        const userNameEl = document.getElementById('userName');
        if (userNameEl) userNameEl.textContent = user.fullName || user.name || 'Administrator';
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn && typeof logout === 'function') logoutBtn.addEventListener('click', logout);

        // Initial Load
        await loadAuditLogs();
    } catch (err) {
        console.error('Admin initialization error:', err);
        showToast && showToast('Initialization failed. Check console.');
    }

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
    const recentActivityList = document.getElementById('recentActivityList');

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
        const raw = await apiFetch(`/audit/logs?${queryParams.toString()}`);
        // normalize response: array or {data|items}
        const logs = Array.isArray(raw) ? raw : (raw && (raw.data || raw.items || raw.logs) ? (raw.data || raw.items || raw.logs) : []);

        if (pageIndicator) pageIndicator.textContent = `Page ${currentPage}`;
        if (prevBtn) prevBtn.disabled = currentPage === 1;

        const count = Array.isArray(logs) ? logs.length : 0;
        if (!logs || count === 0) {
            if (tableBody) tableBody.innerHTML = '';
            if (emptyState) emptyState.style.display = 'block';
            if (nextBtn) nextBtn.disabled = true;
            if (recentActivityList) recentActivityList.innerHTML = '';
            return;
        }

        if (emptyState) emptyState.style.display = 'none';
        if (nextBtn) nextBtn.disabled = count < pageSize;

        if (tableBody) {
            tableBody.innerHTML = logs.map(log => `
                <tr>
                    <td><small>${new Date(log.timestamp || log.createdAt || Date.now()).toLocaleString()}</small></td>
                    <td><span class="badge ${getActionBadgeClass(log.actionType || log.action || '')}">${escapeHtml(log.actionType || log.action || 'action')}</span></td>
                    <td>${escapeHtml(log.description || log.details || '')}</td>
                    <td>${escapeHtml(log.actionByUserName || log.actorName || 'System')} (ID: ${log.actionByUserId || log.actorId || '—'})</td>
                    <td>${log.requestId ? `#${log.requestId}` : 'N/A'}</td>
                </tr>
            `).join('');
        }

        // populate recent activities (first 6)
        if (recentActivityList) populateRecentActivities(logs.slice(0, 6));

    } catch (err) {
        console.error('loadAuditLogs error:', err);
        showToast && showToast(err && err.message ? err.message : 'Failed to load audit logs.');
    }
}

function populateRecentActivities(items) {
    const list = document.getElementById('recentActivityList');
    if (!list) return;
    list.innerHTML = items.map(it => {
        const when = new Date(it.timestamp || it.createdAt || Date.now()).toLocaleString();
        const actor = escapeHtml(it.actionByUserName || it.actorName || 'System');
        const action = escapeHtml(it.actionType || it.action || 'activity');
        const desc = escapeHtml((it.description || it.details || '').slice(0, 120));
        return `<li style="padding:0.5rem; border-bottom:1px solid var(--color-border);"><div style="font-weight:700;">${action}</div><div style="font-size:0.85rem; color:var(--color-text-muted);">${desc}</div><div style="font-size:0.75rem; color:var(--color-text-muted); margin-top:0.25rem;">${actor} • ${when}</div></li>`;
    }).join('');
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
