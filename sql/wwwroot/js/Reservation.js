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
    const today = new Date();
    const minDate = today.toISOString().split('T')[0];
    const maxDate = new Date(today.getTime() + advanceDays * 24 * 60 * 60 * 1000)
        .toISOString()
        .split('T')[0];

    $('.future-date-input').each(function () {
        $(this).attr('min', minDate);
        $(this).attr('max', maxDate);
        $(this).val(minDate);
    });
}

function updateUserWelcome() {
    $.ajax({
        url: '/Equipment/GetCurrentUser',
        type: 'GET',
        success: function (response) {
            if (response.userName) {
                $('#userWelcome').html(`歡迎，<strong>${response.userName}</strong>！`);
            } else {
                $('#userWelcome').html('請先<a href="/Account/Login">登入</a>');
            }
        },
        error: function () {
            $('#userWelcome').html('無法載入使用者資訊');
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
                $('#status-' + equipmentId).text('狀態更新失敗').removeClass().addClass('status-offline');
                $('#reserve-btn-' + equipmentId).addClass('disabled-btn').prop('disabled', true);
                applyEquipmentFiltersAndPagination();
                return;
            }

            updateEquipmentDisplay(equipmentId, response.data);
        },
        error: function () {
            setEquipmentAvailabilityState(equipmentId, 'error');
            $('#status-' + equipmentId).text('狀態更新失敗').removeClass().addClass('status-offline');
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
            $status.text('目前不可使用').removeClass().addClass('status-closed');
            $reserveBtn.text('立即使用').addClass('disabled-btn').prop('disabled', true);
            $queueInfo.hide();
            return;
        }

        if (availability.isFull) {
            setEquipmentAvailabilityState(equipmentId, 'queue');
            $status.text('目前需排隊').removeClass().addClass('status-full');
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
        $status.text('狀態未知').removeClass().addClass('status-offline');
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
                    reject(response.message || '設備可用性資料格式錯誤');
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
        return '目前沒有符合篩選條件的設備。';
    }

    return `目前共 ${totalItems} 台設備，分成 ${totalPages} 頁，現在在第 ${reservationPageState.currentPage} 頁。`;
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
    const reservationDate = $('#future-date-' + equipmentId).val();
    const $select = $('#future-slot-select-' + equipmentId);
    const $hint = $('#future-slot-hint-' + equipmentId);

    if (!reservationDate) {
        $select.html('<option value="">請先選擇日期</option>');
        $hint.text('');
        return;
    }

    $select.html('<option value="">載入時間點中...</option>');
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
                $select.html('<option value="">時段載入失敗</option>');
                $hint.text(response.message || '無法取得可預約時間點');
                return;
            }

            renderFutureReservationPlanning(equipmentId, response.data);
        },
        error: function (xhr, status, error) {
            $select.html('<option value="">時段載入失敗</option>');
            $hint.text('無法取得可預約時間點：' + error);
        }
    });
}

function renderFutureReservationPlanning(equipmentId, planning) {
    const $select = $('#future-slot-select-' + equipmentId);
    const selectableSlots = (planning.slots || []).filter(slot => slot.isSelectable);

    if (selectableSlots.length === 0) {
        $select.html('<option value="">當天沒有可預約時間點</option>');
        $('#future-slot-hint-' + equipmentId).text('請改選其他日期。');
        return;
    }

    const options = ['<option value="">請選擇時間</option>']
        .concat(selectableSlots.map(slot => {
            const queueExpected = slot.requiresQueueConfirmation ? 'true' : 'false';
            const statusNote = escapeHtml(slot.statusNote || '');
            return `<option value="${slot.slotStartTime}" data-queue-expected="${queueExpected}" data-status-note="${statusNote}">${slot.displayLabel}</option>`;
        }));

    $select.html(options.join(''));
    $('#future-slot-hint-' + equipmentId).text('選擇時間後即可建立預約。');
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
        $('#future-slot-hint-' + equipmentId).text(statusNote || '依目前推算，這個時間點之後可能仍需排隊。');
        return;
    }

    $('#future-slot-hint-' + equipmentId).text(statusNote || '此時間點目前可直接建立預約。');
}

function createFutureReservation(equipmentId) {
    const reservationDate = $('#future-date-' + equipmentId).val();
    const $selected = $(`#future-slot-select-${equipmentId} option:selected`);
    const slotStartTime = $selected.val();
    const requiresQueueConfirmation = $selected.data('queue-expected') === true || $selected.data('queue-expected') === 'true';

    if (!reservationDate || !slotStartTime) {
        showError('請先選擇日期與時間。');
        return;
    }

    const confirmMessage = requiresQueueConfirmation
        ? `系統推算到 ${reservationDate} ${slotStartTime} 時，前面仍可能有人排隊。\n\n如果你仍要建立，系統會先保留這個時間點，並在到點時視為預約排隊。\n\n確定仍要建立嗎？`
        : `確定要預約 ${reservationDate} ${slotStartTime} 的設備時間嗎？`;

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
                showConfirm(`${response.message}\n\n確定仍要建立這筆未來預約嗎？`, function () {
                    submitFutureReservation(equipmentId, reservationDate, slotStartTime, true);
                });
                return;
            }

            showError(response.message || '建立未來預約失敗');
        },
        error: function (xhr, status, error) {
            showLoading(false);
            showError('建立未來預約失敗：' + error);
        }
    });
}

function makeReservation(equipmentId) {
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    if ($reserveBtn.prop('disabled')) {
        showError('當前無法使用此設備');
        return;
    }

    const currentStatus = $('#status-' + equipmentId).text();
    const confirmMessage = currentStatus.includes('排隊')
        ? '設備目前需要排隊，確定要加入排隊嗎？'
        : '確定要立即使用此設備嗎？';

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
                        message += `\n\n排隊位置：第 ${response.waitingPosition} 位`;
                        message += `\n預計等待：約 ${response.estimatedWaitTime} 分鐘`;
                    }

                    showSuccess(message);
                    refreshEquipmentStatus(equipmentId);
                    refreshAllEquipmentStatus();
                } else {
                    showError(response.message || '處理失敗');
                }
            },
            error: function (xhr, status, error) {
                showLoading(false);
                $reserveBtn.prop('disabled', false).text(currentStatus.includes('排隊') ? '加入排隊' : '立即使用');

                if (xhr.status === 401) {
                    showError('請先登入系統');
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 1500);
                } else {
                    showError('使用請求失敗：' + error);
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
