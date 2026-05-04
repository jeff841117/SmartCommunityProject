const reservationPageState = {
    currentPage: 1,
    pageSize: 6,
    pendingConfirmAction: null
};

let successModalInstance;
let errorModalInstance;
let confirmModalInstance;

$(document).ready(function () {
    const $config = $('#reservationPageConfig');
    reservationPageState.pageSize = parseInt($config.data('page-size') || 6, 10);

    successModalInstance = new bootstrap.Modal(document.getElementById('successModal'));
    errorModalInstance = new bootstrap.Modal(document.getElementById('errorModal'));
    confirmModalInstance = new bootstrap.Modal(document.getElementById('confirmModal'));

    updateUserWelcome();
    initializeFutureReservationInputs();
    bindReservationPageEvents();
    refreshAllEquipmentStatus();
    applyEquipmentFiltersAndPagination();

    setInterval(refreshAllEquipmentStatus, 30000);
});

function bindReservationPageEvents() {
    $('#availabilityFilter, #categoryFilter').on('change', function () {
        reservationPageState.currentPage = 1;
        applyEquipmentFiltersAndPagination();
    });

    $('#confirmAction').on('click', function () {
        confirmModalInstance.hide();
        if (typeof reservationPageState.pendingConfirmAction === 'function') {
            const action = reservationPageState.pendingConfirmAction;
            reservationPageState.pendingConfirmAction = null;
            action();
        }
    });
}

function initializeFutureReservationInputs() {
    const $config = $('#reservationPageConfig');
    const advanceDays = parseInt($config.data('advance-days') || 7, 10);
    const today = getLocalDateOnly(new Date());
    const minDate = formatLocalDate(today);
    const maxDate = formatLocalDate(addDays(today, advanceDays));

    $('.future-date-input').each(function () {
        $(this).attr('min', minDate);
        $(this).attr('max', maxDate);
        const currentValue = $(this).val();
        if (!currentValue || currentValue < minDate || currentValue > maxDate) {
            $(this).val(minDate);
        }
    });
}

function updateUserWelcome() {
    $.ajax({
        url: '/Equipment/GetCurrentUser',
        type: 'GET',
        success: function (response) {
            if (response.userName) {
                $('#userWelcome').html(`歡迎，<strong>${response.userName}</strong>`);
            } else {
                $('#userWelcome').html('請先<a href="/Account/Login">登入</a>');
            }
        },
        error: function () {
            $('#userWelcome').html('載入使用者資訊失敗');
        }
    });
}

function refreshAllEquipmentStatus() {
    $('.equipment-item').each(function () {
        const equipmentId = $(this).data('equipment-id');
        refreshEquipmentStatus(equipmentId);
    });
}

function refreshEquipmentStatus(equipmentId) {
    $.ajax({
        url: '/Equipment/GetQueueInfo',
        type: 'GET',
        data: { equipmentId: equipmentId },
        success: function (response) {
            if (!response.success || !response.data) {
                setEquipmentAvailabilityState(equipmentId, 'error');
                $('#status-' + equipmentId).text('載入失敗').removeClass().addClass('status-offline');
                $('#reserve-btn-' + equipmentId).addClass('disabled-btn').prop('disabled', true);
                applyEquipmentFiltersAndPagination();
                return;
            }

            updateEquipmentDisplay(equipmentId, response.data);
        },
        error: function () {
            setEquipmentAvailabilityState(equipmentId, 'error');
            $('#status-' + equipmentId).text('載入失敗').removeClass().addClass('status-offline');
            $('#reserve-btn-' + equipmentId).addClass('disabled-btn').prop('disabled', true);
            applyEquipmentFiltersAndPagination();
        }
    });
}

function updateEquipmentDisplay(equipmentId, data) {
    const $status = $('#status-' + equipmentId);
    const $users = $('#users-' + equipmentId);
    const $queue = $('#queue-' + equipmentId);
    const $queueInfo = $('#queue-info-' + equipmentId);
    const $waitTime = $('#wait-time-' + equipmentId);
    const $availableTime = $('#available-time-' + equipmentId);
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    const currentUsers = data.currentUsers || 0;
    const waitingCount = data.waitingCount || 0;

    $users.text(currentUsers);
    $queue.text(waitingCount);

    checkEquipmentAvailabilityWithTaiwanTime(equipmentId).then(availability => {
        if (!availability.canReserve) {
            setEquipmentAvailabilityState(equipmentId, 'closed');
            $status.text('非開放時間').removeClass().addClass('status-closed');
            $reserveBtn.text('立即使用').addClass('disabled-btn').prop('disabled', true);
            $queueInfo.hide();
            return;
        }

        if (availability.isFull) {
            setEquipmentAvailabilityState(equipmentId, 'queue');
            $status.text('需要排隊').removeClass().addClass('status-full');
            $reserveBtn.text('加入排隊').removeClass('disabled-btn').prop('disabled', false);
            $queueInfo.show();

            const averageUsageTime = availability.averageUsageTime || 30;
            const waitMinutes = calculateAccurateWaitTime(waitingCount, currentUsers, availability.maxUsers, averageUsageTime);
            $waitTime.text(waitMinutes);

            const availableTime = new Date(new Date().getTime() + waitMinutes * 60000);
            $availableTime.text(availableTime.toLocaleTimeString('zh-TW', {
                hour: '2-digit',
                minute: '2-digit'
            }));
        } else {
            setEquipmentAvailabilityState(equipmentId, 'immediate');
            $status.text('可立即使用').removeClass().addClass('status-available');
            $reserveBtn.text('立即使用').removeClass('disabled-btn').prop('disabled', false);
            $queueInfo.hide();
        }

        applyEquipmentFiltersAndPagination();
    }).catch(() => {
        setEquipmentAvailabilityState(equipmentId, 'error');
        $status.text('載入失敗').removeClass().addClass('status-offline');
        $reserveBtn.text('立即使用').addClass('disabled-btn').prop('disabled', true);
        applyEquipmentFiltersAndPagination();
    });
}

function setEquipmentAvailabilityState(equipmentId, state) {
    $(`.equipment-item[data-equipment-id="${equipmentId}"]`).attr('data-availability-state', state);
}

function calculateAccurateWaitTime(queueCount, currentUsers, maxUsers, averageUsageTime = 30) {
    if (queueCount === 0) {
        return 0;
    }

    if (currentUsers < maxUsers) {
        return 0;
    }

    const currentUsersRemainingTime = currentUsers * (averageUsageTime * 0.25);
    const peopleAhead = queueCount;
    const queueAheadTime = peopleAhead * averageUsageTime;
    const totalWaitTime = currentUsersRemainingTime + queueAheadTime;

    return Math.max(5, Math.min(totalWaitTime, 240));
}

function checkEquipmentAvailabilityWithTaiwanTime(equipmentId) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/Equipment/CheckEquipmentAvailability',
            type: 'GET',
            data: { equipmentId: equipmentId },
            success: function (response) {
                if (!response.success || !response.data) {
                    reject(response.message || '檢查設備可用性時發生錯誤');
                    return;
                }

                resolve(response.data);
            },
            error: function (xhr, status, error) {
                reject(error);
            }
        });
    });
}

function applyEquipmentFiltersAndPagination() {
    const availabilityFilter = $('#availabilityFilter').val();
    const categoryFilter = $('#categoryFilter').val();
    const allItems = $('.equipment-item').toArray();

    const filteredItems = allItems.filter(item => {
        const $item = $(item);
        const category = $item.data('category');
        const availabilityState = $item.attr('data-availability-state');

        const matchCategory = categoryFilter === 'all' || category === categoryFilter;
        const matchAvailability =
            availabilityFilter === 'all' ||
            (availabilityFilter === 'immediate' && availabilityState === 'immediate') ||
            (availabilityFilter === 'not-immediate' && availabilityState !== 'immediate');

        return matchCategory && matchAvailability;
    });

    const totalItems = filteredItems.length;
    const totalPages = Math.max(1, Math.ceil(totalItems / reservationPageState.pageSize));

    if (reservationPageState.currentPage > totalPages) {
        reservationPageState.currentPage = totalPages;
    }

    const startIndex = (reservationPageState.currentPage - 1) * reservationPageState.pageSize;
    const endIndex = startIndex + reservationPageState.pageSize;

    $('.equipment-item').hide();
    filteredItems.slice(startIndex, endIndex).forEach(item => $(item).show());

    $('#emptyEquipmentState').toggle(totalItems === 0);
    $('#equipmentCountLabel').text(buildEquipmentCountLabel(totalItems, totalPages));
    renderPagination(totalPages);
}

function buildEquipmentCountLabel(totalItems, totalPages) {
    if (totalItems === 0) {
        return '目前沒有符合條件的設備';
    }

    return `目前共有 ${totalItems} 台設備，共 ${totalPages} 頁，現在顯示第 ${reservationPageState.currentPage} 頁`;
}

function renderPagination(totalPages) {
    const $pagination = $('#reservationPagination');
    $pagination.empty();

    if (totalPages <= 1) {
        return;
    }

    const $nav = $('<nav aria-label="設備分頁"></nav>');
    const $list = $('<ul class="pagination justify-content-center flex-wrap mb-0"></ul>');

    $list.append(createPaginationItem('上一頁', reservationPageState.currentPage === 1, function () {
        changePage(reservationPageState.currentPage - 1);
    }));

    for (let page = 1; page <= totalPages; page++) {
        const isActive = page === reservationPageState.currentPage;
        const $item = $('<li class="page-item"></li>').toggleClass('active', isActive);
        const $button = $('<button type="button" class="page-link"></button>').text(page);
        $button.on('click', function () {
            changePage(page);
        });
        $item.append($button);
        $list.append($item);
    }

    $list.append(createPaginationItem('下一頁', reservationPageState.currentPage === totalPages, function () {
        changePage(reservationPageState.currentPage + 1);
    }));

    $nav.append($list);
    $pagination.append($nav);
}

function createPaginationItem(text, disabled, onClick) {
    const $item = $('<li class="page-item"></li>').toggleClass('disabled', disabled);
    const $button = $('<button type="button" class="page-link"></button>').text(text);

    if (!disabled) {
        $button.on('click', onClick);
    }

    $item.append($button);
    return $item;
}

function changePage(page) {
    if (page < 1) {
        return;
    }

    reservationPageState.currentPage = page;
    applyEquipmentFiltersAndPagination();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function toggleFuturePlanner(equipmentId) {
    const $planner = $('#future-planner-' + equipmentId);
    const shouldShow = !$planner.is(':visible');

    $('.future-planner').hide();
    if (shouldShow) {
        $planner.show();
        loadFutureReservationPlanning(equipmentId);
    }
}

function loadFutureReservationPlanning(equipmentId) {
    const reservationDate = $("#future-date-" + equipmentId).val();
    const $select = $("#future-slot-select-" + equipmentId);
    const $hint = $("#future-slot-hint-" + equipmentId);
    const todayString = formatLocalDate(getLocalDateOnly(new Date()));

    // 有些瀏覽器允許手動輸入日期，所以送查詢前先擋掉過去日期。
    if (reservationDate && reservationDate < todayString) {
        $("#future-date-" + equipmentId).val(todayString);
        $select.html('<option value="">請重新選擇今天之後的日期</option>');
        $hint.text('過去日期不能預約，已自動切回今天。');
        return;
    }

    if (!reservationDate) {
        $select.html('<option value="">請先選擇日期</option>');
        $hint.text('');
        return;
    }

    $select.html('<option value="">正在載入可預約時段...</option>');
    $hint.text('');

    $.ajax({
        url: '/Equipment/GetFutureReservationPlanning',
        type: 'GET',
        data: {
            equipmentId: equipmentId,
            reservationDate: reservationDate
        },
        success: function (response) {
            if (!response.success || !response.data) {
                $select.html('<option value="">載入失敗</option>');
                $hint.text(response.message || '目前無法載入可預約時段');
                return;
            }

            renderFutureReservationPlanning(equipmentId, response.data);
        },
        error: function (xhr, status, error) {
            $select.html('<option value="">載入失敗</option>');
            $hint.text('目前無法載入可預約時段：' + error);
        }
    });
}

function renderFutureReservationPlanning(equipmentId, planning) {
    const $select = $('#future-slot-select-' + equipmentId);
    const selectableSlots = (planning.slots || []).filter(slot => slot.isSelectable);

    if (selectableSlots.length === 0) {
        $select.html('<option value="">當天沒有可預約時段</option>');
        $('#future-slot-hint-' + equipmentId).text('請改選其他日期。');
        return;
    }

    const options = ['<option value="">請選擇預約時間</option>']
        .concat(selectableSlots.map(slot => {
            const queueExpected = slot.requiresQueueConfirmation ? 'true' : 'false';
            const statusNote = escapeHtml(slot.statusNote || '');
            return `<option value="${slot.slotStartTime}" data-queue-expected="${queueExpected}" data-status-note="${statusNote}">${slot.displayLabel}</option>`;
        }));

    $select.html(options.join(''));
    $('#future-slot-hint-' + equipmentId).text('請選擇你要預約的開始時間。');
}

function updateFutureSlotHint(equipmentId) {
    const $selected = $(`#future-slot-select-${equipmentId} option:selected`);
    const statusNote = $selected.data('status-note') || '';
    const queueExpected = $selected.data('queue-expected') === true || $selected.data('queue-expected') === 'true';

    if (!$selected.val()) {
        $('#future-slot-hint-' + equipmentId).text('');
        return;
    }

    if (queueExpected) {
        $('#future-slot-hint-' + equipmentId).text(statusNote || '此時段依目前推算仍可能需要排隊，建立前請再次確認。');
        return;
    }

    $('#future-slot-hint-' + equipmentId).text(statusNote || '此時段目前可正常建立預約。');
}

function createFutureReservation(equipmentId) {
    const reservationDate = $('#future-date-' + equipmentId).val();
    const $selected = $(`#future-slot-select-${equipmentId} option:selected`);
    const slotStartTime = $selected.val();
    const requiresQueueConfirmation = $selected.data('queue-expected') === true || $selected.data('queue-expected') === 'true';

    if (!reservationDate || !slotStartTime) {
        showError('請先選擇日期與預約時間');
        return;
    }

    const confirmMessage = requiresQueueConfirmation
        ? `系統推算 ${reservationDate} ${slotStartTime} 這個時段仍可能需要排隊。\n\n如果你同意，系統會建立預約，並在到點後視狀況排入隊列。\n\n是否仍要建立預約？`
        : `確定要建立 ${reservationDate} ${slotStartTime} 的預約嗎？`;

    showConfirm(confirmMessage, function () {
        submitFutureReservation(equipmentId, reservationDate, slotStartTime, requiresQueueConfirmation);
    });
}

function submitFutureReservation(equipmentId, reservationDate, slotStartTime, confirmQueueExpected) {
    showLoading(true);

    $.ajax({
        url: '/Equipment/CreateFutureReservation',
        type: 'POST',
        data: {
            equipmentId: equipmentId,
            reservationDate: reservationDate,
            selectedSlotStartTime: slotStartTime,
            confirmQueueExpected: confirmQueueExpected
        },
        success: function (response) {
            showLoading(false);

            if (response.success) {
                showSuccess(`${response.message}\n\n預約時間：${reservationDate} ${slotStartTime}`);
                loadFutureReservationPlanning(equipmentId);
                refreshEquipmentStatus(equipmentId);
                return;
            }

            if (response.requiresConfirmation && response.queueExpected) {
                showConfirm(`${response.message}\n\n是否仍要建立這筆預約？`, function () {
                    submitFutureReservation(equipmentId, reservationDate, slotStartTime, true);
                });
                return;
            }

            showError(response.message || '建立預約失敗');
        },
        error: function (xhr, status, error) {
            showLoading(false);
            showError('建立預約失敗：' + error);
        }
    });
}

function makeReservation(equipmentId) {
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    if ($reserveBtn.prop('disabled')) {
        showError('目前無法立即使用這台設備');
        return;
    }

    const currentStatus = $('#status-' + equipmentId).text();
    const confirmMessage = currentStatus.includes('排隊')
        ? '這台設備目前需要排隊，是否加入排隊？'
        : '確定要立即使用這台設備嗎？';

    showConfirm(confirmMessage, function () {
        showLoading(true);
        $reserveBtn.prop('disabled', true).text('處理中...');

        $.ajax({
            url: '/Equipment/MakeReservation',
            type: 'POST',
            data: { equipmentId: equipmentId },
            success: function (response) {
                showLoading(false);
                $reserveBtn.prop('disabled', false);

                if (response.success) {
                    let message = response.message;
                    if (response.waitingPosition) {
                        message += `\n\n排隊順位：第 ${response.waitingPosition} 位`;
                        message += `\n預估等待時間：約 ${response.estimatedWaitTime} 分鐘`;
                    }

                    showSuccess(message);
                    refreshEquipmentStatus(equipmentId);
                    refreshAllEquipmentStatus();
                } else {
                    showError(response.message || '立即使用失敗');
                }
            },
            error: function (xhr, status, error) {
                showLoading(false);
                $reserveBtn.prop('disabled', false).text(currentStatus.includes('排隊') ? '加入排隊' : '立即使用');

                if (xhr.status === 401) {
                    showError('請先登入再進行預約');
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 1500);
                } else {
                    showError('立即使用失敗：' + error);
                }
            }
        });
    });
}

function showSuccess(message) {
    $('#successMessage').text(message);
    successModalInstance.show();
}

function showError(message) {
    $('#errorMessage').text(message);
    errorModalInstance.show();
}

function showConfirm(message, confirmCallback) {
    reservationPageState.pendingConfirmAction = confirmCallback;
    $('#confirmMessage').text(message);
    confirmModalInstance.show();
}

function showLoading(show) {
    $('#loadingSpinner').toggle(show);
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function getLocalDateOnly(date) {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addDays(date, days) {
    const nextDate = new Date(date);
    nextDate.setDate(nextDate.getDate() + days);
    return nextDate;
}

function formatLocalDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
