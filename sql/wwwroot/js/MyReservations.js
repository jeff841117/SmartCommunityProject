const myReservationsState = {
    historyPage: 1,
    historyPageSize: 10,
    historyItems: [],
    pendingConfirmAction: null
};

let myReservationSuccessModal;
let myReservationErrorModal;
let myReservationConfirmModal;

$(document).ready(function () {
    myReservationSuccessModal = new bootstrap.Modal(document.getElementById('successModal'));
    myReservationErrorModal = new bootstrap.Modal(document.getElementById('errorModal'));
    myReservationConfirmModal = new bootstrap.Modal(document.getElementById('confirmModal'));

    $('#confirmAction').on('click', function () {
        myReservationConfirmModal.hide();
        if (typeof myReservationsState.pendingConfirmAction === 'function') {
            const action = myReservationsState.pendingConfirmAction;
            myReservationsState.pendingConfirmAction = null;
            action();
        }
    });

    $(document).on('click', '.history-summary', function () {
        $(this).closest('.history-entry').toggleClass('is-open');
    });

    loadMyReservations();
    setInterval(loadMyReservations, 30000);
});

function showGlobalLoading(show) {
    $('#globalLoading').toggle(show);
}

function showSuccess(message) {
    $('#successMessage').text(message);
    myReservationSuccessModal.show();
}

function showError(message) {
    $('#errorMessage').text(message);
    myReservationErrorModal.show();
}

function showConfirm(message, confirmCallback) {
    myReservationsState.pendingConfirmAction = confirmCallback;
    $('#confirmMessage').text(message);
    myReservationConfirmModal.show();
}

function loadMyReservations() {
    $.ajax({
        url: '/Equipment/GetMyReservations',
        type: 'GET',
        success: function (response) {
            if (!response.success || !response.data) {
                showError(response.message || '載入預約資料失敗');
                return;
            }

            displayMyReservations(response.data);
        },
        error: function (xhr, status, error) {
            showError('載入失敗，請重新整理頁面：' + error);
        }
    });
}

function displayMyReservations(data) {
    displayScheduledReservations(data.scheduledReservations || []);
    displayActiveReservations(data.activeReservations || []);
    displayWaitingReservations(data.waitingReservations || []);
    displayHistoryReservations(data.historyReservations || []);
}

function displayScheduledReservations(items) {
    if (!items.length) {
        $('#scheduledReservations').html('<div class="portal-empty">目前沒有未來預約。</div>');
        return;
    }

    const html = items.map(item => `
        <div class="reservation-entry">
            <div class="reservation-entry-header">
                <div>
                    <h3 class="reservation-entry-title">${escapeHtml(item.equipmentName || item.EquipmentName)}</h3>
                    <small class="text-muted">${formatDateOnly(item.reservedStartTime || item.ReservedStartTime)}</small>
                </div>
                <span class="reservation-status ${escapeHtml(item.statusCssClass || item.StatusCssClass || '')}">
                    ${escapeHtml(item.statusText || item.StatusText || '未設定')}
                </span>
            </div>
            <div class="reservation-meta">
                <div class="reservation-meta-item">
                    <span class="reservation-meta-label">預約時間</span>
                    <span class="reservation-meta-value">${formatDateTime(item.reservationTime || item.ReservationTime)}</span>
                </div>
                <div class="reservation-meta-item">
                    <span class="reservation-meta-label">使用日期</span>
                    <span class="reservation-meta-value">${formatDateOnly(item.reservedStartTime || item.ReservedStartTime)}</span>
                </div>
                <div class="reservation-meta-item">
                    <span class="reservation-meta-label">預約開始</span>
                    <span class="reservation-meta-value">${formatTimeOnly(item.reservedStartTime || item.ReservedStartTime)}</span>
                </div>
                <div class="reservation-meta-item">
                    <span class="reservation-meta-label">使用時長</span>
                    <span class="reservation-meta-value">${parseInt(item.durationMinutes || item.DurationMinutes || 0, 10)} 分鐘</span>
                </div>
            </div>
            <div class="reservation-actions">
                <button type="button" class="portal-btn portal-btn-danger" onclick="cancelReservation(${parseInt(item.id || item.Id, 10)})">取消預約</button>
            </div>
        </div>
    `).join('');

    $('#scheduledReservations').html(html);
}

function displayActiveReservations(items) {
    if (!items.length) {
        $('#activeReservations').html('<div class="portal-empty">目前沒有使用中的設備。</div>');
        return;
    }

    const html = items.map(item => {
        const remainingTime = parseInt(item.remainingTime || item.RemainingTime || 0, 10);
        return `
            <div class="reservation-entry">
                <div class="reservation-entry-header">
                    <div>
                        <h3 class="reservation-entry-title">${escapeHtml(item.equipmentName || item.EquipmentName)}</h3>
                        <small class="text-muted">${formatDateOnly(item.startTime || item.StartTime)}</small>
                    </div>
                    <span class="reservation-status ${escapeHtml(item.statusCssClass || item.StatusCssClass || '')}">
                        ${escapeHtml(item.statusText || item.StatusText || '使用中')}
                    </span>
                </div>
                <div class="reservation-meta">
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">開始時間</span>
                        <span class="reservation-meta-value">${formatDateTime(item.startTime || item.StartTime)}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">剩餘時間</span>
                        <span class="reservation-meta-value">${remainingTime > 0 ? `${remainingTime} 分鐘` : '已到期'}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">可使用時間</span>
                        <span class="reservation-meta-value">${parseInt(item.availableTime || item.AvailableTime || 0, 10)} 分鐘</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">預計結束</span>
                        <span class="reservation-meta-value">${formatEndTime(item.startTime || item.StartTime, item.availableTime || item.AvailableTime)}</span>
                    </div>
                </div>
                <div class="reservation-actions">
                    <button type="button" class="portal-btn portal-btn-danger" onclick="endUsage(${parseInt(item.id || item.Id, 10)})">結束使用</button>
                </div>
            </div>
        `;
    }).join('');

    $('#activeReservations').html(html);
}

function displayWaitingReservations(items) {
    if (!items.length) {
        $('#waitingReservations').html('<div class="portal-empty">目前沒有排隊中的預約。</div>');
        return;
    }

    const html = items.map(item => {
        const position = parseInt(item.position || item.Position || 0, 10);
        const averageUsageTime = parseInt(item.averageUsageTime || item.AverageUsageTime || 30, 10);
        return `
            <div class="reservation-entry">
                <div class="reservation-entry-header">
                    <div>
                        <h3 class="reservation-entry-title">${escapeHtml(item.equipmentName || item.EquipmentName)}</h3>
                        <small class="text-muted">${escapeHtml(item.queueTypeText || item.QueueTypeText || '一般即時排隊')}</small>
                    </div>
                    <span class="reservation-status status-waiting">排隊中</span>
                </div>
                <div class="reservation-meta">
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">排隊順位</span>
                        <span class="reservation-meta-value">第 ${position} 位</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">預估等待</span>
                        <span class="reservation-meta-value">約 ${Math.max(0, position - 1) * averageUsageTime} 分鐘</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">加入時間</span>
                        <span class="reservation-meta-value">${formatDateTime(item.queueTime || item.QueueTime)}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">前方人數</span>
                        <span class="reservation-meta-value">${Math.max(0, position - 1)} 人</span>
                    </div>
                </div>
                <div class="reservation-actions">
                    <button type="button" class="portal-btn portal-btn-outline" onclick="cancelQueue(${parseInt(item.id || item.Id, 10)})">取消排隊</button>
                </div>
            </div>
        `;
    }).join('');

    $('#waitingReservations').html(html);
}

function displayHistoryReservations(items) {
    const cutoff = new Date();
    cutoff.setMonth(cutoff.getMonth() - 1);

    myReservationsState.historyItems = items.filter(item => {
        const reservationTime = new Date(item.reservationTime || item.ReservationTime);
        return !Number.isNaN(reservationTime.getTime()) && reservationTime >= cutoff;
    });

    myReservationsState.historyPage = 1;
    renderHistoryPage();
}

function renderHistoryPage() {
    const items = myReservationsState.historyItems;
    if (!items.length) {
        $('#historyReservations').html('<div class="portal-empty">最近一個月內沒有歷史記錄。</div>');
        $('#historyPagination').empty();
        return;
    }

    const totalPages = Math.max(1, Math.ceil(items.length / myReservationsState.historyPageSize));
    if (myReservationsState.historyPage > totalPages) {
        myReservationsState.historyPage = totalPages;
    }

    const startIndex = (myReservationsState.historyPage - 1) * myReservationsState.historyPageSize;
    const currentPageItems = items.slice(startIndex, startIndex + myReservationsState.historyPageSize);

    const html = currentPageItems.map(item => `
        <div class="reservation-entry history-entry">
            <div class="history-summary">
                <div>
                    <h3>${escapeHtml(item.equipmentName || item.EquipmentName)}</h3>
                    <small>${formatDateOnly(item.reservationTime || item.ReservationTime)}</small>
                </div>
                <span class="history-toggle">點擊展開詳情</span>
            </div>
            <div class="history-detail">
                <div class="reservation-meta">
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">預約時間</span>
                        <span class="reservation-meta-value">${formatDateTime(item.reservationTime || item.ReservationTime)}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">開始時間</span>
                        <span class="reservation-meta-value">${formatNullableDateTime(item.startTime || item.StartTime, '未開始')}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">結束時間</span>
                        <span class="reservation-meta-value">${formatNullableDateTime(item.endTime || item.EndTime, '未結束')}</span>
                    </div>
                    <div class="reservation-meta-item">
                        <span class="reservation-meta-label">狀態</span>
                        <span class="reservation-status ${escapeHtml(item.statusCssClass || item.StatusCssClass || '')}">
                            ${escapeHtml(item.statusText || item.StatusText || '已完成')}
                        </span>
                    </div>
                </div>
            </div>
        </div>
    `).join('');

    $('#historyReservations').html(html);
    renderHistoryPagination(totalPages);
}

function renderHistoryPagination(totalPages) {
    if (totalPages <= 1) {
        $('#historyPagination').empty();
        return;
    }

    const $pagination = $('#historyPagination');
    const $nav = $('<nav aria-label="歷史記錄分頁"></nav>');
    const $list = $('<ul class="pagination justify-content-center mb-0"></ul>');

    $list.append(createHistoryPaginationItem('上一頁', myReservationsState.historyPage === 1, function () {
        myReservationsState.historyPage--;
        renderHistoryPage();
    }));

    for (let page = 1; page <= totalPages; page++) {
        const $item = $('<li class="page-item"></li>').toggleClass('active', page === myReservationsState.historyPage);
        const $button = $('<button type="button" class="page-link"></button>').text(page);
        $button.on('click', function () {
            myReservationsState.historyPage = page;
            renderHistoryPage();
        });
        $item.append($button);
        $list.append($item);
    }

    $list.append(createHistoryPaginationItem('下一頁', myReservationsState.historyPage === totalPages, function () {
        myReservationsState.historyPage++;
        renderHistoryPage();
    }));

    $nav.append($list);
    $pagination.empty().append($nav);
}

function createHistoryPaginationItem(text, disabled, onClick) {
    const $item = $('<li class="page-item"></li>').toggleClass('disabled', disabled);
    const $button = $('<button type="button" class="page-link"></button>').text(text);
    if (!disabled) {
        $button.on('click', onClick);
    }
    $item.append($button);
    return $item;
}

function endUsage(reservationId) {
    showConfirm('確定要結束使用這台設備嗎？', function () {
        showGlobalLoading(true);

        $.ajax({
            url: '/Equipment/EndUsage',
            type: 'POST',
            data: { reservationId: reservationId },
            success: function (response) {
                showGlobalLoading(false);
                if (response.success) {
                    showSuccess('已結束使用');
                    loadMyReservations();
                } else {
                    showError('結束使用失敗：' + response.message);
                }
            },
            error: function (xhr, status, error) {
                showGlobalLoading(false);
                showError('結束使用失敗：' + error);
            }
        });
    });
}

function cancelReservation(reservationId) {
    showConfirm('確定要取消這筆未來預約嗎？', function () {
        showGlobalLoading(true);

        $.ajax({
            url: '/Equipment/CancelReservation',
            type: 'POST',
            data: { reservationId: reservationId },
            success: function (response) {
                showGlobalLoading(false);
                if (response.success) {
                    showSuccess('已取消預約');
                    loadMyReservations();
                } else {
                    showError('取消預約失敗：' + response.message);
                }
            },
            error: function (xhr, status, error) {
                showGlobalLoading(false);
                showError('取消預約失敗：' + error);
            }
        });
    });
}

function cancelQueue(queueId) {
    showConfirm('確定要取消排隊嗎？', function () {
        showGlobalLoading(true);

        $.ajax({
            url: '/Equipment/CancelQueue',
            type: 'POST',
            data: { queueId: queueId },
            success: function (response) {
                showGlobalLoading(false);
                if (response.success) {
                    showSuccess('已取消排隊');
                    loadMyReservations();
                } else {
                    showError('取消排隊失敗：' + response.message);
                }
            },
            error: function (xhr, status, error) {
                showGlobalLoading(false);
                showError('取消排隊失敗：' + error);
            }
        });
    });
}

function formatDateTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '時間格式錯誤';
    }

    return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    }).replace(/\//g, '-');
}

function formatDateOnly(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '日期格式錯誤';
    }

    return date.toLocaleDateString('zh-TW').replace(/\//g, '-');
}

function formatTimeOnly(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '時間格式錯誤';
    }

    return date.toLocaleTimeString('zh-TW', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    });
}

function formatNullableDateTime(value, fallbackText) {
    if (!value) {
        return fallbackText;
    }

    return formatDateTime(value);
}

function formatEndTime(startTime, availableTime) {
    const start = new Date(startTime);
    const duration = parseInt(availableTime || 0, 10);
    if (Number.isNaN(start.getTime())) {
        return '時間格式錯誤';
    }

    start.setMinutes(start.getMinutes() + duration);
    return formatDateTime(start);
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
