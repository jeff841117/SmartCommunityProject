// 頁面載入時初始化
$(document).ready(function () {
    updateUserWelcome();
    refreshAllEquipmentStatus();

    // 每30秒自動更新狀態
    setInterval(refreshAllEquipmentStatus, 30000);

});

// 更新使用者歡迎訊息
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

// 刷新所有設備狀態
function refreshAllEquipmentStatus() {
    // console.log('刷新所有設備狀態...');
    $('.equipment-item').each(function () {
        const equipmentId = $(this).data('equipment-id');
        refreshEquipmentStatus(equipmentId);
    });
}

// 刷新單個設備狀態
function refreshEquipmentStatus(equipmentId) {
    $.ajax({
        url: '/Equipment/GetQueueInfo',
        type: 'GET',
        data: { equipmentId: equipmentId },
        success: function (response) {
            updateEquipmentDisplay(equipmentId, response);
        },
        error: function (xhr, status, error) {
            console.error('獲取設備狀態失敗:', error);
            $('#status-' + equipmentId).text('狀態更新失敗').removeClass().addClass('status-offline');
            $('#reserve-btn-' + equipmentId).addClass('disabled-btn').prop('disabled', true);
        }
    });
}

// 更新設備顯示狀態
function updateEquipmentDisplay(equipmentId, data) {
    const $status = $('#status-' + equipmentId);
    const $users = $('#users-' + equipmentId);
    const $queue = $('#queue-' + equipmentId);
    const $queueInfo = $('#queue-info-' + equipmentId);
    const $waitTime = $('#wait-time-' + equipmentId);
    const $availableTime = $('#available-time-' + equipmentId);
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    // 更新使用人數和排隊人數
    const currentUsers = data.currentUsers || 0;
    const waitingCount = data.waitingCount || 0;

    $users.text(currentUsers);
    $queue.text(waitingCount);

    // 檢查設備可用性
    checkEquipmentAvailabilityWithTaiwanTime(equipmentId).then(availability => {
        console.log(`設備 ${equipmentId} 可用性:`, availability);

        if (!availability.canReserve) {
            // 不在開放時間內 - 禁用按鈕
            $status.text('不可預約').removeClass().addClass('status-closed');
            $reserveBtn.addClass('disabled-btn').prop('disabled', true);
            $queueInfo.hide();
            $status.attr('title', availability.message);
        } else if (availability.isFull) {
            // 設備已滿 - 但按鈕不禁用，可以進入排隊
            $status.text('已滿，可排隊').removeClass().addClass('status-full');
            $reserveBtn.removeClass('disabled-btn').prop('disabled', false);
            $queueInfo.show();

            // 等待時間計算
            const averageUsageTime = availability.averageUsageTime || 30; // 從後端獲取或使用默認值
            const waitMinutes = calculateAccurateWaitTime(waitingCount, currentUsers, availability.maxUsers, averageUsageTime);

            $waitTime.text(waitMinutes);

            // 計算預計可用時間
            const availableTime = new Date(new Date().getTime() + waitMinutes * 60000);
            $availableTime.text(availableTime.toLocaleTimeString('zh-TW', {
                hour: '2-digit',
                minute: '2-digit'
            }));

            // 顯示詳細的排隊信息
            let queueMessage = `設備已滿\n`;
            queueMessage += `當前使用: ${currentUsers}/${availability.maxUsers}人\n`;
            queueMessage += `排隊人數: ${waitingCount}人\n`;

            if (waitMinutes > 0) {
                queueMessage += `預計等待: ${waitMinutes}分鐘`;
            } else {
                queueMessage += `有空位時立即開始`;
            }

            $status.attr('title', queueMessage);
            $reserveBtn.text('加入排隊');
        } else {
            // 可預約
            $status.text('可預約').removeClass().addClass('status-available');
            $reserveBtn.removeClass('disabled-btn').prop('disabled', false);
            $reserveBtn.text('立即預約');
            $queueInfo.hide();
            $status.attr('title', availability.message);
        }
    }).catch(error => {
        console.error('檢查設備可用性失敗:', error);
        $status.text('狀態未知').removeClass().addClass('status-closed');
        $reserveBtn.addClass('disabled-btn').prop('disabled', true);
    });
}

function calculateAccurateWaitTime(queueCount, currentUsers, maxUsers, averageUsageTime = 30) {
    console.log(`準確計算: 排隊${queueCount}人, 使用${currentUsers}/${maxUsers}, 平均${averageUsageTime}分鐘`);

    // 沒人排隊
    if (queueCount === 0) {
        return 0;
    }

    // 設備還有空位
    if (currentUsers < maxUsers) {
        // 還有空位，排隊的人可以立即使用
        return 0;
    }

    // 情況3: 設備已滿，有人排隊
    // 等待時間 = 當前使用者剩餘時間 + 前面排隊的人的使用時間

    // 假設當前使用者平均還剩 25% 的使用時間（更保守的估計）
    const currentUsersRemainingTime = currentUsers * (averageUsageTime * 0.25);

    // 排隊在你前面的人數
    const peopleAhead = queueCount; // 注意：這裡是總排隊人數，新加入的會在最後

    // 前面的人總使用時間（不包括你自己）
    const queueAheadTime = peopleAhead * averageUsageTime;

    // 總等待時間
    let totalWaitTime = currentUsersRemainingTime + queueAheadTime;

    console.log(`等待時間分解: 當前使用者剩餘${currentUsersRemainingTime}分鐘 + 前面${peopleAhead}人排隊${queueAheadTime}分鐘 = ${totalWaitTime}分鐘`);

    // 最少等待5分鐘，最多不超過合理範圍
    return Math.max(5, Math.min(totalWaitTime, 240)); // 最多4小時
}



function checkEquipmentAvailabilityWithTaiwanTime(equipmentId) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/Equipment/CheckEquipmentAvailability',
            type: 'GET',
            data: { equipmentId: equipmentId },
            success: function (response) {
                resolve(response);
            },
            error: function (xhr, status, error) {
                reject(error);
            }
        });
    });
}
function showSuccess(message) {
    $('#successMessage').text(message);
    $('#successModal').modal('show');
}

function showError(message) {
    $('#errorMessage').text(message);
    $('#errorModal').modal('show');
}

function showConfirm(message, confirmCallback) {
    $('#confirmMessage').text(message);
    $('#confirmModal').modal('show');

    // 移除舊的事件監聽器
    $('#confirmAction').off('click');

    // 添加新的事件監聽器
    $('#confirmAction').on('click', function () {
        $('#confirmModal').modal('hide');
        if (confirmCallback) confirmCallback();
    });
}
// 發起預約
function makeReservation(equipmentId) {
    const $reserveBtn = $('#reserve-btn-' + equipmentId);

    if ($reserveBtn.prop('disabled')) {
        showError('當前無法預約此設備');
        return;
    }

    // 根據當前狀態顯示不同的確認訊息
    const currentStatus = $('#status-' + equipmentId).text();
    let confirmMessage = '確定要預約此設備嗎？';

    if (currentStatus.includes('排隊')) {
        confirmMessage = '設備當前已滿，確定要加入排隊嗎？';
    }

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
                        message += '\n\n📋 排隊信息：';
                        message += '\n📍 排隊位置：第 ' + response.waitingPosition + ' 位';
                        message += '\n⏱️ 預計等待：約 ' + response.estimatedWaitTime + ' 分鐘';
                        if (response.expectedStartTime) {
                            const startTime = new Date(response.expectedStartTime);
                            message += '\n🕐 預計開始：' + startTime.toLocaleString('zh-TW');
                        }
                        message += '\n\n系統會在輪到您時自動通知';
                    }
                    showSuccess(message);

                    // 更新設備狀態
                    refreshEquipmentStatus(equipmentId);
                    refreshAllEquipmentStatus();

                    // 如果是立即使用，詢問是否跳轉
                    if (!response.waitingPosition) {
                        setTimeout(() => {
                            showConfirm('預約成功！是否要查看您的預約狀態？', function () {
                                window.location.href = '/Equipment/MyReservations';
                            });
                        }, 1500);
                    }
                } else {
                    showError(response.message);
                    $reserveBtn.text(currentStatus.includes('排隊') ? '加入排隊' : '立即預約');
                }
            },
            error: function (xhr, status, error) {
                showLoading(false);
                $reserveBtn.prop('disabled', false).text(currentStatus.includes('排隊') ? '加入排隊' : '立即預約');

                if (xhr.status === 401) {
                    showError('請先登入系統');
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 2000);
                } else {
                    showError('預約請求失敗：' + error);
                }
            }
        });
    });
}

// 顯示/隱藏載入動畫
function showLoading(show) {
    if (show) {
        $('#loadingSpinner').show();
    } else {
        $('#loadingSpinner').hide();
    }
}
