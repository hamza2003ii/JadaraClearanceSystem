/**
 * Jadara Clearance System — Administrator Audit Logs Logic
 * Enterprise Version with CSV Export & Filter Analytics
 */

let currentPage = 1;
const pageSize = 15;
let currentLoadedLogs = [];

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const user = (typeof checkPageAccess === 'function') ? checkPageAccess('Admin') : null;
        if (!user) return;

        // Header Setup
        const userNameEl = document.getElementById('userName');
        if (userNameEl) userNameEl.textContent = user.fullName || user.name || 'مسؤول النظام';
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn && typeof logout === 'function') logoutBtn.addEventListener('click', logout);

        // Initial Load
        await loadAuditLogs();
    } catch (err) {
        console.error('Admin initialization error:', err);
        if (typeof showToast === 'function') showToast('فشل في تهيئة اللوحة.');
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
        if (typeof showToast === 'function') showToast('تمت إعادة ضبط الفلاتر.');
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

    // Export Button Handler
    document.getElementById('exportLogsBtn')?.addEventListener('click', exportLogsToCSV);
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
        const raw = await apiFetch(`/audit/logs?${queryParams.toString()}`);
        const logs = Array.isArray(raw) ? raw : (raw && (raw.data || raw.items || raw.logs) ? (raw.data || raw.items || raw.logs) : []);
        currentLoadedLogs = logs;

        if (pageIndicator) pageIndicator.textContent = `صفحة ${currentPage}`;
        if (prevBtn) prevBtn.disabled = currentPage === 1;

        const count = Array.isArray(logs) ? logs.length : 0;
        
        // Update live counter KPI if available
        const kpiActive = document.getElementById('kpiActiveRequests');
        if (kpiActive) kpiActive.textContent = `${count} عملية`;

        if (!logs || count === 0) {
            if (tableBody) tableBody.innerHTML = '';
            if (emptyState) emptyState.style.display = 'block';
            if (nextBtn) nextBtn.disabled = true;
            return;
        }

        if (emptyState) emptyState.style.display = 'none';
        if (nextBtn) nextBtn.disabled = count < pageSize;

        if (tableBody) {
            tableBody.innerHTML = logs.map(log => {
                const timestampFormatted = new Date(log.timestamp || log.createdAt || Date.now()).toLocaleDateString('ar-JO', {
                    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit'
                });
                const actionBadgeClass = getActionBadgeClass(log.actionType || log.action || '');
                const actionArabic = translateActionType(log.actionType || log.action || '');

                return `
                    <tr>
                        <td style="direction: ltr; text-align: right; font-size: 0.85rem;"><small>${timestampFormatted}</small></td>
                        <td><span class="badge ${actionBadgeClass}">${escapeHtml(actionArabic)}</span></td>
                        <td style="max-width: 350px;">${escapeHtml(log.description || log.details || '')}</td>
                        <td><strong>${escapeHtml(log.actionByUserName || log.actorName || 'النظام')}</strong> <small style="color: var(--text-subtle);">(ID: ${log.actionByUserId || '—'})</small></td>
                        <td><code>${log.requestId ? `#${log.requestId}` : '—'}</code></td>
                    </tr>
                `;
            }).join('');
        }

    } catch (err) {
        console.error('loadAuditLogs error:', err);
        if (typeof showToast === 'function') showToast(err && err.message ? err.message : 'فشل في تحميل سجلات التدقيق.');
    }
}

/**
 * Export current audit records to a downloadable CSV file
 */
function exportLogsToCSV() {
    if (!currentLoadedLogs || currentLoadedLogs.length === 0) {
        if (typeof showToast === 'function') showToast('لا توجد سجلات حالية للتصدير.');
        return;
    }

    const headers = ['التوقيت', 'نوع العملية', 'البيان', 'المنفذ', 'معرف المنفذ', 'رقم المعاملة'];
    const rows = currentLoadedLogs.map(log => [
        `"${new Date(log.timestamp || Date.now()).toISOString()}"`,
        `"${(log.actionType || '').replace(/"/g, '""')}"`,
        `"${(log.description || '').replace(/"/g, '""')}"`,
        `"${(log.actionByUserName || '').replace(/"/g, '""')}"`,
        `"${log.actionByUserId || ''}"`,
        `"${log.requestId || ''}"`
    ]);

    // Prepend UTF-8 BOM so Excel opens Arabic correctly
    const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `jadara_audit_logs_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    if (typeof showToast === 'function') showToast('تم تصدير سجلات التدقيق بنجاح بصيغة CSV!');
}

function getActionBadgeClass(actionType) {
    const act = (actionType || '').toLowerCase();
    if (act.includes('approved') || act.includes('completed') || act.includes('success')) {
        return 'badge-success';
    }
    if (act.includes('rejected') || act.includes('fail') || act.includes('delete')) {
        return 'badge-danger';
    }
    return 'badge-warning';
}

function translateActionType(actionType) {
    switch (actionType) {
        case 'RequestCreated': return 'إنشاء طلب براءة ذمة';
        case 'ApprovalUpdated': return 'تعديل حالة الاعتماد';
        case 'Approved': return 'اعتماد براءة ذمة';
        case 'Rejected': return 'رفض طلب';
        case 'RequestCompleted': return 'اكتمال براءة الذمة';
        default: return actionType || 'إجراء';
    }
}

function escapeHtml(str) {
    return (str || '').replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
