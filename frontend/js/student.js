/**
 * Jadara Clearance System — Student Dashboard Logic
 * Enterprise Version with Digital Certificate Modal & Arabic Stepper
 */

let pollInterval = null;
let currentRequestData = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = checkPageAccess('Student');
    if (!user) return;

    // Header Setup
    document.getElementById('userName').textContent = user.fullName || 'طالب';
    document.getElementById('logoutBtn')?.addEventListener('click', logout);

    // Initial Load & Refresh
    await loadStudentClearanceRequest();

    document.getElementById('refreshBtn')?.addEventListener('click', async () => {
        await loadStudentClearanceRequest();
        showToast('تم تحديث حالة طلب براءة الذمة بنجاح.');
    });

    // Auto Poll every 25 seconds
    pollInterval = setInterval(loadStudentClearanceRequest, 25000);
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

        currentRequestData = requestData;
        renderRequestProgress(contentArea, requestData);

    } catch (err) {
        if (err.status === 404) {
            renderNoRequestState(contentArea);
        } else {
            contentArea.innerHTML = `
                <div class="card" style="border-right: 4px solid var(--danger-text); padding: 1.5rem;">
                    <div style="font-weight: 700; color: var(--danger-text); margin-bottom: 0.35rem;">تعذر جلب حالة براءة الذمة</div>
                    <div style="color: var(--text-muted); font-size: 0.9rem;">${escapeHtml(err.message)}</div>
                </div>
            `;
        }
    }
}

function renderNoRequestState(container) {
    // Reset KPIs
    document.getElementById('overallStatusText').textContent = 'لا يوجد طلب نشط';
    document.getElementById('completedDeptsCount').textContent = '0 / 4';
    document.getElementById('progressPercentage').textContent = '0%';
    document.getElementById('progressBarFill').style.width = '0%';
    document.getElementById('certificateContainer').innerHTML = '<span class="badge badge-warning">بانتظار البدء</span>';

    container.innerHTML = `
        <div class="card" style="text-align: center; padding: 3.5rem 1.5rem;">
            <div style="width: 60px; height: 60px; border-radius: 50%; background: var(--primary-surface); color: var(--primary); display: flex; align-items: center; justify-content: center; margin: 0 auto 1.25rem; font-size: 1.75rem;">
                📋
            </div>
            <h2 style="font-size: 1.5rem; margin-bottom: 0.5rem; color: var(--text-main); font-weight: 800;">لا يوجد طلب براءة ذمة نشط حالياً</h2>
            <p style="color: var(--text-muted); margin-bottom: 2rem; max-width: 550px; margin-left: auto; margin-right: auto; font-size: 0.95rem;">
                لم تقم ببدء إجراءات براءة الذمة بعد. يمكنك النقر على الزر أدناه لبدء المعاملة فوراً وتوزيعها آلياً على كافة الأقسام المعنية (المكتبة، المالية، القبول والتسجيل، شؤون الطلبة).
            </p>
            <button id="startRequestBtn" class="btn btn-primary" style="padding: 0.85rem 2rem; font-size: 1rem;">
                🚀 بدء تقديم طلب براءة الذمة
            </button>
        </div>
    `;

    document.getElementById('startRequestBtn')?.addEventListener('click', handleCreateRequest);
}

async function handleCreateRequest(e) {
    const btn = e.target;
    btn.disabled = true;
    btn.innerHTML = `<span class="spinner" style="display:inline-block; width:16px; height:16px; border:2px solid #fff; border-top-color:transparent; border-radius:50%; animation:spin 0.8s linear infinite; margin-left:8px;"></span> جاري الإرسال...`;

    try {
        await apiFetch('/clearance/request', { method: 'POST' });
        showToast('تم تقديم طلب براءة الذمة وبدء الإجراءات بنجاح!');
        await loadStudentClearanceRequest();
    } catch (err) {
        showToast(err.message || 'فشل في تقديم الطلب.');
        btn.disabled = false;
        btn.textContent = 'بدء تقديم طلب براءة الذمة';
    }
}

function renderRequestProgress(container, data) {
    const overallBadgeClass = getBadgeClass(data.overallStatus);
    const overallStatusArabic = translateStatus(data.overallStatus);
    const formattedDate = new Date(data.requestDate).toLocaleDateString('ar-JO', { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    const completedDate = data.completedAt ? new Date(data.completedAt).toLocaleDateString('ar-JO', { year: 'numeric', month: 'long', day: 'numeric' }) : null;

    // Calculate Completed vs Total
    const approvals = data.approvals || [];
    const completedCount = approvals.filter(a => a.status === 'Approved').length;
    const totalCount = approvals.length || 4;
    const progressPercent = Math.round((completedCount / totalCount) * 100);

    // Update KPIs
    document.getElementById('overallStatusText').innerHTML = `<span class="badge ${overallBadgeClass}">${overallStatusArabic}</span>`;
    document.getElementById('completedDeptsCount').textContent = `${completedCount} / ${totalCount}`;
    document.getElementById('progressPercentage').textContent = `${progressPercent}%`;
    document.getElementById('progressBarFill').style.width = `${progressPercent}%`;

    let certificateContainerEl = document.getElementById('certificateContainer');
    if (data.overallStatus === 'Completed') {
        certificateContainerEl.innerHTML = `
            <button class="btn btn-success" style="padding: 0.35rem 0.85rem; font-size: 0.85rem;" onclick="openCertificateModal()">
                ✓ عرض وطباعة الشهادة
            </button>
        `;
    } else {
        certificateContainerEl.innerHTML = `<span class="badge badge-warning">قيد المراجعة</span>`;
    }

    let certificateBannerHtml = '';
    if (data.overallStatus === 'Completed') {
        certificateBannerHtml = `
            <div class="certificate-banner" style="margin-top: 1.75rem;">
                <div>
                    <h3 style="color: #b45309; font-size: 1.15rem; font-weight: 800; margin-bottom: 0.35rem;">
                        🎓 تم إصدار وثيقة براءة الذمة الرسمية المعتمدة
                    </h3>
                    <p style="font-size: 0.88rem; color: var(--text-muted); margin-bottom: 0.5rem;">
                        تم إبراء ذمتك بنجاح من كافة الأقسام. يمكنك استعراض وثيقتك وطباعتها بصيغة رسمية مع الختم الرقمي:
                    </p>
                    <div>رمز التحقق المشفر: <span class="hash-code">${data.certificateHash || 'N/A'}</span></div>
                </div>
                <button class="btn btn-primary" onclick="openCertificateModal()" style="white-space: nowrap;">
                    معاينة وطباعة الوثيقة
                </button>
            </div>
        `;
    }

    let approvalsCardsHtml = approvals.map(app => {
        const badgeClass = getBadgeClass(app.status);
        const statusArabic = translateStatus(app.status);
        const deptNameArabic = translateDeptName(app.departmentName);
        const updateTime = app.updatedAt ? new Date(app.updatedAt).toLocaleDateString('ar-JO', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) : 'بانتظار المراجعة';
        
        let fineDetails = '';
        if (app.fineAmount && app.fineAmount > 0) {
            const paidBadge = app.isPaid 
                ? '<span class="badge badge-success">تم السداد</span>' 
                : '<span class="badge badge-danger">مستحق الدفع</span>';
            fineDetails = `<div style="font-size: 0.85rem; margin-top: 0.35rem; color: var(--text-main);">الرسوم أو الغرامة: <strong>${app.fineAmount.toFixed(2)} د.أ</strong> ${paidBadge}</div>`;
        }

        let rejectionHtml = '';
        if (app.status === 'Rejected' && app.rejectionReason) {
            rejectionHtml = `<div style="color: var(--danger-text); font-size: 0.85rem; margin-top: 0.35rem; background: var(--danger-bg); padding: 0.4rem 0.65rem; border-radius: 4px;">ملاحظات القسم: "${escapeHtml(app.rejectionReason)}"</div>`;
        }

        return `
            <div class="step-card">
                <div class="step-header">
                    <span class="step-title">${deptNameArabic}</span>
                    <span class="badge ${badgeClass}">${statusArabic}</span>
                </div>
                <div style="font-size: 0.8rem; color: var(--text-subtle);">آخر تحديث: ${updateTime}</div>
                ${fineDetails}
                ${rejectionHtml}
            </div>
        `;
    }).join('');

    container.innerHTML = `
        <div class="card">
            <div class="card-header">
                <div>
                    <h2 class="card-title">معاملة براءة الذمة #${data.id}</h2>
                    <div class="card-subtitle">تاريخ التقديم: ${formattedDate} ${completedDate ? `| تاريخ الاعتماد النهائي: ${completedDate}` : ''}</div>
                </div>
                <span class="badge ${overallBadgeClass}">${overallStatusArabic}</span>
            </div>

            <h3 style="font-size: 1.05rem; font-weight: 700; color: var(--text-main); margin-bottom: 1rem;">
                اعتمادات الدوائر والأقسام الجامعية:
            </h3>
            <div class="stepper-container">
                ${approvalsCardsHtml}
            </div>
        </div>
        ${certificateBannerHtml}
    `;
}

// Certificate Modal Handlers
window.openCertificateModal = function () {
    if (!currentRequestData) return;
    const modal = document.getElementById('certificateModal');
    if (!modal) return;

    document.getElementById('certStudentName').textContent = currentRequestData.studentFullName || 'طالب جامعة جدارا';
    document.getElementById('certStudentId').textContent = currentRequestData.studentUniversityId || '20241001';
    document.getElementById('certCompletionDate').textContent = currentRequestData.completedAt 
        ? new Date(currentRequestData.completedAt).toLocaleDateString('ar-JO', { year: 'numeric', month: 'long', day: 'numeric' })
        : new Date().toLocaleDateString('ar-JO');
    document.getElementById('certRequestId').textContent = `#${currentRequestData.id}`;
    document.getElementById('certHashVal').textContent = currentRequestData.certificateHash || 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855';

    modal.style.display = 'flex';
};

window.closeCertificateModal = function () {
    const modal = document.getElementById('certificateModal');
    if (modal) modal.style.display = 'none';
};

function getBadgeClass(status) {
    switch (status) {
        case 'Approved':
        case 'Completed':
            return 'badge-success';
        case 'Rejected':
            return 'badge-danger';
        case 'Pending':
        default:
            return 'badge-warning';
    }
}

function translateStatus(status) {
    switch (status) {
        case 'Approved': return 'تم الاعتماد';
        case 'Completed': return 'مكتملة ومعتمدة';
        case 'Rejected': return 'مرفوض';
        case 'Pending': return 'قيد المراجعة';
        default: return status || 'قيد المراجعة';
    }
}

function translateDeptName(dept) {
    switch (dept) {
        case 'Library': return 'مكتبة الجامعة';
        case 'Finance': return 'الدائرة المالية والمحاسبة';
        case 'Registration': return 'دائرة القبول والتسجيل';
        case 'Student Affairs': return 'عمادة شؤون الطلبة';
        default: return dept || 'القسم';
    }
}

function escapeHtml(str) {
    return (str || '').replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
