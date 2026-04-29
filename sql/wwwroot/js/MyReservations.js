
    $(document).ready(function() {
        displayCurrentTime();
            

        loadMyReservations();

    // 每30秒自動更新狀態
    setInterval(loadMyReservations, 30000);
        });

            

        // 顯示/隱藏全域載入動畫
        function showGlobalLoading(show) {
            if (show) {
                $('#globalLoading').show();
            } else {
                $('#globalLoading').hide();
            }
        }

        // 載入我的預約 載入完成後也觸發排隊檢查
        function loadMyReservations() {
        $.ajax({
            url: '/Equipment/GetMyReservations',
            type: 'GET',
            success: function (response) {
                // 後端現在把資料包成 ApiDataResponse，
                // 所以不能再直接把整個 response 當成預約資料本體使用。
                if (!response.success) {
                    showError(response.message || '載入預約資料失敗');
                    return;
                }

                if (!response.data) {
                    showError('預約資料格式不正確');
                    return;
                }

                displayMyReservations(response.data);

                // 載入完成後檢查是否有排隊需要推進
                // setTimeout(checkAndProcessQueues, 1000);
            },
            error: function (xhr, status, error) {
                console.error('獲取預約信息失敗:', error);
                showError('載入失敗，請重新整理頁面');
            }
        });
        }

    // Modal 控制函數
    function showConfirm(message, confirmCallback) {
        // console.log('顯示確認對話框:', message);
        $('#confirmMessage').text(message);

    // 移除舊的事件監聽器
    $('#confirmAction').off('click');

    // 添加新的事件監聽器
    $('#confirmAction').on('click', function() {
        // console.log('用戶點擊確認');
        $('#confirmModal').modal('hide');
    if (confirmCallback) {
        confirmCallback();
                }
            });

    $('#confirmModal').modal('show');
        }

    function showSuccess(message) {
        // console.log('顯示成功訊息:', message);
        $('#successMessage').text(message);
    $('#successModal').modal('show');
        }

    function showError(message) {
        // console.log('顯示錯誤訊息:', message);
        $('#errorMessage').text(message);
    $('#errorModal').modal('show');
        }

    function showGlobalLoading(show) {
            // console.log('顯示載入動畫:', show);
            if (show) {
        $('#globalLoading').show();
            } else {
        $('#globalLoading').hide();
            }
        }

    // 顯示預約資料
    function displayMyReservations(data) {
        console.log("完整的返回數據:", data);

    try {
        // 顯示進行中的預約
        displayActiveReservations(data.activeReservations || []);

    // 顯示排隊中的預約
    displayWaitingReservations(data.waitingReservations || []);

    // 顯示歷史記錄
    displayHistoryReservations(data.historyReservations || []);
            } catch (error) {
        console.error("顯示預約數據時發生錯誤:", error);
    showError('載入預約數據時發生錯誤: ' + error.message);
            }
        }

    function displayActiveReservations(activeReservations) {
        console.log("進行中的預約數據:", activeReservations);

            if (activeReservations && activeReservations.length > 0) {
        let activeHtml = '';
                activeReservations.forEach(reservation => {
                    try {
                        const id = safeGetProperty(reservation, 'Id', 'id');
    const equipmentName = safeGetProperty(reservation, 'EquipmentName', 'equipmentName');
    const startTime = safeGetProperty(reservation, 'StartTime', 'startTime');
    const availableTime = safeGetProperty(reservation, 'AvailableTime', 'availableTime');


    let remainingTime = safeGetProperty(reservation, 'RemainingTime', 'remainingTime');

    if (!remainingTime || remainingTime === '0') {
        remainingTime = calculateRemainingTimeFrontend(startTime, availableTime);
                        }

    const remainingTimeInt = parseInt(remainingTime) || 0;
                        const timeDisplay = remainingTimeInt > 0 ?
    `<span class="time-remaining">${remainingTimeInt} 分鐘</span>` :
    `<span class="text-danger">已到期</span>`;

    const formattedStartTime = formatDateTime(startTime);
    const formattedEndTime = formatDateTime(calculateEndTime(startTime, availableTime));

    activeHtml += `
    <div class="reservation-card" data-reservation-id="${id}">
        <div class="row align-items-center">
            <div class="col-md-8">
                <h5 class="text-primary">${equipmentName}</h5>
                <p class="mb-1"><strong>開始時間:</strong> ${formattedStartTime}</p>
                <p class="mb-1"><strong>剩餘時間:</strong> ${timeDisplay}</p>
                <p class="mb-1"><strong>預計結束:</strong> ${formattedEndTime}</p>
                <p class="mb-1"><strong>可用時間:</strong> ${availableTime} 分鐘</p>
            </div>
            <div class="col-md-4 text-end">
                <button class="btn btn-danger btn-action" onclick="endUsage(${id})">
                    結束使用
                </button>
            </div>
        </div>
    </div>
    `;
                    } catch (error) {
        console.error('處理單個預約時錯誤:', error, reservation);
                    }
                });
    $('#activeReservations').html(activeHtml);
            } else {
        $('#activeReservations').html(`
                    <div class="empty-state">
                        <p>暫無進行中的預約</p>
                        <a href="/Equipment/Reservation" class="btn btn-primary">立即預約</a>
                    </div>
                `);
            }
        }

        

        function displayWaitingReservations(waitingReservations) {
            if (waitingReservations.length > 0) {
                let waitingHtml = '';
                waitingReservations.forEach(queue => {
                    try {
                        const id = safeGetProperty(queue, 'Id', 'id');
                        const equipmentName = safeGetProperty(queue, 'EquipmentName', 'equipmentName');
                        const position = safeGetProperty(queue, 'Position', 'position');
                        const averageUsageTime = safeGetProperty(queue, 'AverageUsageTime', 'averageUsageTime');
                        const queueTime = safeGetProperty(queue, 'QueueTime', 'queueTime');
                        const equipmentId = safeGetProperty(queue, 'EquipmentId', 'equipmentId');

                        // 調試時間信息
                        debugTimeInfo(queueTime, `排隊記錄 ${id}`);

                        const waitTime = calculateWaitTime(position, averageUsageTime);
                        const formattedQueueTime = formatQueueTime(queueTime);

                        waitingHtml += `
                            <div class="reservation-card">
                                <div class="row align-items-center">
                                    <div class="col-md-8">
                                        <h5 class="text-warning">${equipmentName}</h5>
                                        <p class="mb-1"><strong>排隊位置:</strong> 第 ${position} 位</p>
                                        <p class="mb-1"><strong>預計等待:</strong> 約 ${waitTime} 分鐘</p>
                                        <p class="mb-1"><strong>加入時間:</strong> ${formattedQueueTime}</p>
                                        <p class="mb-1"><strong>前方人數:</strong> ${position - 1} 人</p>
                                    </div>
                                    <div class="col-md-4 text-end">
                                        <button class="btn btn-outline-warning btn-action" onclick="cancelQueue('${id}')">
                                            取消排隊
                                        </button>
                                    </div>
                                </div>
                            </div>
                        `;
                    } catch (error) {
                        console.error('處理排隊記錄時錯誤:', error, queue);
                        waitingHtml += `<div class="alert alert-warning">排隊數據格式錯誤: ${error.message}</div>`;
                    }
                });
                $('#waitingReservations').html(waitingHtml);
            } else {
                $('#waitingReservations').html(`
                    <div class="empty-state">
                        <p>暫無排隊中的預約</p>
                    </div>
                `);
            }
        }


        function displayHistoryReservations(historyReservations) {
            if (historyReservations.length > 0) {
        let historyHtml = '';
                historyReservations.forEach(reservation => {
                    try {
                        const equipmentName = safeGetProperty(reservation, 'EquipmentName', 'equipmentName');
    const reservationTime = safeGetProperty(reservation, 'ReservationTime', 'reservationTime');
    const startTime = safeGetProperty(reservation, 'StartTime', 'startTime');
    const endTime = safeGetProperty(reservation, 'EndTime', 'endTime');
    const status = safeGetProperty(reservation, 'Status', 'status');

    const statusText = getStatusText(status);
    const statusClass = getStatusClass(status);

    historyHtml += `
    <div class="reservation-card">
        <div class="row">
            <div class="col-md-10">
                <h5>${equipmentName}</h5>
                <p class="mb-1"><strong>預約時間:</strong> ${formatDateTime(reservationTime)}</p>
                <p class="mb-1"><strong>開始時間:</strong> ${startTime ? formatDateTime(startTime) : '未開始'}</p>
                <p class="mb-1"><strong>結束時間:</strong> ${endTime ? formatDateTime(endTime) : '未結束'}</p>
                <p class="mb-0"><strong>狀態:</strong> <span class="${statusClass}">${statusText}</span></p>
            </div>
            <div class="col-md-2 text-end">
                <small class="text-muted">${formatDate(reservationTime)}</small>
            </div>
        </div>
    </div>
    `;
                    } catch (error) {
        console.error('處理歷史記錄時錯誤:', error, reservation);
    historyHtml += `<div class="alert alert-warning">歷史記錄數據格式錯誤</div>`;
                    }
                });
    $('#historyReservations').html(historyHtml);
            } else {
        $('#historyReservations').html(`
                    <div class="empty-state">
                        <p>暫無歷史記錄</p>
                    </div>
                `);
            }
        }

    // 安全獲取屬性的輔助函數
    function safeGetProperty(obj, prop1, prop2) {
            if (!obj) return '';

    // 嘗試第一個屬性名
    if (obj[prop1] !== undefined) {
                return String(obj[prop1]);
            }

    // 嘗試第二個屬性名
    if (obj[prop2] !== undefined) {
                return String(obj[prop2]);
            }

    // 都沒有找到，返回空字符串
    return '';
        }

    // 輔助函數
    function calculateRemainingTime(startTime, availableTime) {
            const start = new Date(startTime);
    const now = new Date();
    const elapsedMinutes = Math.floor((now - start) / (1000 * 60));
    const remaining = availableTime - elapsedMinutes;
    return Math.max(0, remaining);
        }

    function calculateEndTime(startTime, availableTime) {
            const start = new Date(startTime);
    return new Date(start.getTime() + availableTime * 60000);
        }

    function calculateWaitTime(position, averageUsageTime) {
            return (position - 1) * (averageUsageTime || 30); // 預設每人30分鐘
        }

    function formatDateTime(dateTimeStr) {
            try {
                // 確保輸入是有效的日期字符串
                if (!dateTimeStr) return '無時間信息';

    const date = new Date(dateTimeStr);

    // 檢查日期是否有效
    if (isNaN(date.getTime())) {
        console.error('無效的日期:', dateTimeStr);
    return '時間格式錯誤';
                }

    // 轉換為北京時間 (UTC+8)
    const beijingOffset = 8 * 60; // 北京時區偏移量（分鐘）
    const localOffset = date.getTimezoneOffset(); // 本地時區偏移量
    const beijingTime = new Date(date.getTime() + (beijingOffset + localOffset) * 60000);

    return beijingTime.toLocaleString('zh-TW', {
        year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false // 使用24小時制
                }).replace(/\//g, '-'); // 將斜線替換為橫線
            } catch (error) {
        console.error('格式化日期時錯誤:', error, dateTimeStr);
    return '時間格式錯誤';
            }
        }

    // 排隊時間格式化函數
    function formatQueueTime(dateTimeStr) {
            try {
                if (!dateTimeStr) return '無時間信息';

    const date = new Date(dateTimeStr);

    if (isNaN(date.getTime())) {
                    return '時間格式錯誤';
                }

    // 轉換為北京時間
    const beijingOffset = 8 * 60;
    const localOffset = date.getTimezoneOffset();
    const beijingTime = new Date(date.getTime() + (beijingOffset + localOffset) * 60000);

    // 顯示更詳細的時間信息
    return beijingTime.toLocaleString('zh-TW', {
        year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
                }).replace(/\//g, '-');
            } catch (error) {
        console.error('格式化排隊時間時錯誤:', error);
    return '時間格式錯誤';
            }
        }

    // 添加調試函數來檢查時間
    function debugTimeInfo(dateTimeStr, context) {
        console.log(`${context} 時間調試:`, {
            原始字符串: dateTimeStr,
            轉換的Date對象: new Date(dateTimeStr),
            本地時間: new Date(dateTimeStr).toLocaleString('zh-TW'),
            UTC時間: new Date(dateTimeStr).toUTCString(),
            時間戳: new Date(dateTimeStr).getTime()
        });
        }

    // 添加當前時間顯示函數用於調試
    function displayCurrentTime() {
            const now = new Date();
    const beijingOffset = 8 * 60;
    const localOffset = now.getTimezoneOffset();
    const beijingTime = new Date(now.getTime() + (beijingOffset + localOffset) * 60000);

    
    }

    function formatDate(dateTimeStr) {
            const date = new Date(dateTimeStr);
    return date.toLocaleDateString('zh-TW');
    }

    function getStatusText(status) {
            switch(status) {
                case 0: return '等待中';
    case 1: return '使用中';
    case 2: return '已完成';
    case 3: return '已取消';
    default: return '未知';
            }
        }

    function getStatusClass(status) {
            switch(status) {
                case 0: return 'status-waiting';
    case 1: return 'status-inprogress';
    case 2: return 'status-completed';
    case 3: return 'status-cancelled';
    default: return '';
            }
        }

    // 結束使用函數
    function endUsage(reservationId) {
        console.log('=== 結束使用函數被調用 ===');
    console.log('預約ID:', reservationId);
    console.log('函數作用域:', this);

    // 檢查 reservationId 是否有效
    if (!reservationId || reservationId === '0' || reservationId === 'undefined') {
        console.error('無效的預約ID:', reservationId);
    showError('無效的預約ID');
    return;
            }

    // 確保 showConfirm 函數存在
    if (typeof showConfirm !== 'function') {
        console.error('showConfirm 函數未定義');
    alert('確定要結束使用嗎？'); //  fallback
    return;
            }

    showConfirm('確定要結束使用嗎？', function() {
        console.log('用戶確認結束使用，預約ID:', reservationId);
    executeEndUsage(reservationId);
            });
        }

    // 分離實際的 AJAX 調用 在結束使用和取消排隊的成功回調中也觸發排隊檢查
    function executeEndUsage(reservationId) {
        console.log('執行結束使用 AJAX，預約ID:', reservationId);
    showGlobalLoading(true);

    $.ajax({
        url: '/Equipment/EndUsage',
    type: 'POST',
    data: {
        reservationId: reservationId
                },
    success: function(response) {
        console.log('結束使用 AJAX 成功:', response);
    showGlobalLoading(false);

    if (response.success) {
        showSuccess('已結束使用');
                        // 結束使用後立即檢查排隊
                        setTimeout(() => {
        checkAndProcessQueues();
    loadMyReservations();
                        }, 500);
                    } else {
        showError('結束使用失敗：' + response.message);
                    }
                },
    error: function(xhr, status, error) {
        console.error('結束使用 AJAX 錯誤:', error);
    showGlobalLoading(false);
    showError('請求失敗：' + error);
                }
            });
        }

    // 取消排隊函數
    function cancelQueue(queueId) {
        showConfirm('確定要取消排隊嗎？', function () {
            showGlobalLoading(true);

            $.ajax({
                url: '/Equipment/CancelQueue',
                type: 'POST',
                data: {
                    queueId: queueId
                },
                success: function (response) {
                    showGlobalLoading(false);
                    if (response.success) {
                        showSuccess('已取消');
                        // 取消排隊後立即檢查排隊
                        setTimeout(() => {
                            checkAndProcessQueues();
                            loadMyReservations();
                        }, 500);
                    } else {
                        showError('取消排隊失敗：' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    showGlobalLoading(false);
                    showError('請求失敗：' + error);
                }
            });
        });
        }

   
    // 檢查並推進排隊
    function checkAndProcessQueues() {
            // 只有在有排隊記錄或空位時才處理
            if ($('#waitingReservations .reservation-card').length > 0) {
        $.ajax({
            url: '/Equipment/ProcessAllQueues',
            type: 'POST',
            success: function (response) {
                if (response.success) {
                    console.log('排隊處理完成');
                    loadMyReservations();
                }
            },
            error: function (xhr, status, error) {
                console.error('處理排隊時錯誤:', error);
            }
        });
            }
        }
        
        // 前端計算剩餘時間的備用方法
        function calculateRemainingTimeFrontend(startTimeStr, availableTimeStr) {
            try {
                const startTime = new Date(startTimeStr);
                const now = new Date();
                const availableTime = parseInt(availableTimeStr) || 0;

                // 計算經過的分鐘數
                const elapsedMinutes = Math.floor((now - startTime) / (1000 * 60));
                const remaining = availableTime - elapsedMinutes;

                console.log(`前端計算剩餘時間:`, {
                    startTime: startTime.toLocaleString('zh-TW'),
                    now: now.toLocaleString('zh-TW'),
                    availableTime,
                    elapsedMinutes,
                    remaining
                });

                return Math.max(0, remaining).toString();
            } catch (error) {
                console.error('前端計算剩餘時間錯誤:', error);
                return '0';
            }
        }

        
