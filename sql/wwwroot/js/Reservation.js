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
                $('#userWelcome').html(`甇∟?嚗?strong>${response.userName}</strong>嚗);
            } else {
                $('#userWelcome').html('隢?<a href="/Account/Login">?餃</a>');
            }
        },
        error: function () {
            $('#userWelcome').html('?⊥?頛雿輻??閮?);
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
                $('#status-' + equipmentId).text('???啣仃??).removeClass().addClass('status-offline');
                $('#reserve-btn-' + equipmentId).addClass('disabled-btn').prop('disabled', true);
                applyEquipmentFiltersAndPagination();
                return;
            }

            updateEquipmentDisplay(equipmentId, response.data);
        },
        error: function () {
            setEquipmentAvailabilityState(equipmentId, 'error');
            $('#status-' + equipmentId).text('???啣仃??).removeClass().addClass('status-offline');
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
            $status.text('?桀?銝雿輻').removeClass().addClass('status-closed');
            $reserveBtn.text('蝡雿輻').addClass('disabled-btn').prop('disabled', true);
            $queueInfo.hide();
            return;
        }

        if (availability.isFull) {
            setEquipmentAvailabilityState(equipmentId, 'queue');
            $status.text('?桀????').removeClass().addClass('status-full');
            $reserveBtn.text('???').removeClass('disabled-btn').prop('disabled', false);
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
            $status.text('?舐??喃蝙??).removeClass().addClass('status-available');
            $reserveBtn.text('蝡雿輻').removeClass('disabled-btn').prop('disabled', false);
            $queueInfo.hide();
        }

        applyEquipmentFiltersAndPagination();
    }).catch(() => {
        setEquipmentAvailabilityState(equipmentId, 'error');
        $status.text('????).removeClass().addClass('status-offline');
        $reserveBtn.text('蝡雿輻').addClass('disabled-btn').prop('disabled', true);
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
                    reject(response.message || '閮剖??舐?扯??撘隤?);
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
        return '?桀?瘝?蝚血?蝭拚璇辣?身??;
    }

    return `?桀???${totalItems} ?啗身???? ${totalPages} ???曉?函洵 ${reservationPageState.currentPage} ?;
}

function renderPagination(totalPages) {
    const $pagination = $('#reservationPagination');
    $pagination.empty();

    if (totalPages <= 1) {
        return;
    }

    const $nav = $('<nav aria-label="閮剖???"></nav>');
    const $list = $('<ul class="pagination justify-content-center flex-wrap mb-0"></ul>');

    $list.append(createPaginationItem('銝???, reservationPageState.currentPage === 1, function () {
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

    $list.append(createPaginationItem('銝???, reservationPageState.currentPage === totalPages, function () {
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
                $select.html('<option value="">?挾頛憭望?</option>');
                $hint.text(response.message || '?⊥????舫?蝝???');
                return;
            }

            renderFutureReservationPlanning(equipmentId, response.data);
        },
        error: function (xhr, status, error) {
            $select.html('<option value="">?挾頛憭望?</option>');
            $hint.text('?⊥????舫?蝝???嚗? + error);
        }
    });
}

function renderFutureReservationPlanning(equipmentId, planning) {
    const $select = $('#future-slot-select-' + equipmentId);
    const selectableSlots = (planning.slots || []).filter(slot => slot.isSelectable);

    if (selectableSlots.length === 0) {
        $select.html('<option value="">?嗅予瘝??舫?蝝???</option>');
        $('#future-slot-hint-' + equipmentId).text('隢?詨隞??);
        return;
    }

    const options = ['<option value="">隢????/option>']
        .concat(selectableSlots.map(slot => {
            const queueExpected = slot.requiresQueueConfirmation ? 'true' : 'false';
            const statusNote = escapeHtml(slot.statusNote || '');
            return `<option value="${slot.slotStartTime}" data-queue-expected="${queueExpected}" data-status-note="${statusNote}">${slot.displayLabel}</option>`;
        }));

    $select.html(options.join(''));
    $('#future-slot-hint-' + equipmentId).text('?豢???敺?臬遣蝡?蝝?);
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
        $('#future-slot-hint-' + equipmentId).text(statusNote || '靘?蝞?????銋??航隞?????);
        return;
    }

    $('#future-slot-hint-' + equipmentId).text(statusNote || '甇斗????桀??舐?亙遣蝡?蝝?);
}

function createFutureReservation(equipmentId) {
    const reservationDate = $('#future-date-' + equipmentId).val();
    const $selected = $(`#future-slot-select-${equipmentId} option:selected`);
    const slotStartTime = $selected.val();
    const requiresQueueConfirmation = $selected.data('queue-expected') === true || $selected.data('queue-expected') === 'true';

    if (!reservationDate || !slotStartTime) {
        showError('隢??豢??交?????);
        return;
    }

    const confirmMessage = requiresQueueConfirmation
        ? `蝟餌絞?函???${reservationDate} ${slotStartTime} ???隞?賣?鈭箸??n\n憒?雿?閬遣蝡?蝟餌絞??靽?????嚗蒂?典暺?閬?????n\n蝣箏?隞?撱箇???`
        : `蝣箏?閬?蝝?${reservationDate} ${slotStartTime} ?身????嚗;

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
                showSuccess(`${response.message}\n\n????嚗?{reservationDate} ${slotStartTime}`);
                loadFutureReservationPlanning(equipmentId);
                refreshEquipmentStatus(equipmentId);
                return;
            }

            if (response.requiresConfirmation && response.queueExpected) {
                showConfirm(`${response.message}\n\n蝣箏?隞?撱箇????芯?????`, function () {
                    submitFutureReservation(equipmentId, reservationDate, slotStartTime, true);
                });
                return;
            }

            showError(response.message || '撱箇??芯???憭望?');
        },
        error: function (xhr, status, error) {
            showLoading(false);
            showError('撱箇??芯???憭望?嚗? + error);
        }
    });
}

function makeReservation(equipmentId) {
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    if ($reserveBtn.prop('disabled')) {
        showError('?嗅??⊥?雿輻甇方身??);
        return;
    }

    const currentStatus = $('#status-' + equipmentId).text();
    const confirmMessage = currentStatus.includes('??')
        ? '閮剖??桀??閬???蝣箏?閬??交???嚗?
        : '蝣箏?閬??喃蝙?冽迨閮剖???';

    showConfirm(confirmMessage, function () {
        showLoading(true);
        $reserveBtn.prop('disabled', true).text('??銝?..');

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
                        message += `\n\n??雿蔭嚗洵 ${response.waitingPosition} 雿;
                        message += `\n??蝑?嚗? ${response.estimatedWaitTime} ??`;
                    }

                    showSuccess(message);
                    refreshEquipmentStatus(equipmentId);
                    refreshAllEquipmentStatus();
                } else {
                    showError(response.message || '??憭望?');
                }
            },
            error: function (xhr, status, error) {
                showLoading(false);
                $reserveBtn.prop('disabled', false).text(currentStatus.includes('??') ? '???' : '蝡雿輻');

                if (xhr.status === 401) {
                    showError('隢??餃蝟餌絞');
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 1500);
                } else {
                    showError('雿輻隢?憭望?嚗? + error);
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
