$(document).ready(function () {
    // 編輯按鈕點擊事件
    $(document).on('click', '.edit-btn', function () {
        var userId = $(this).data('id');
        enterEditMode(userId);
    });

    // 儲存按鈕點擊事件
    $(document).on('click', '.save-btn', function () {
        var userId = $(this).data('id');
        saveChanges(userId);
    });

    // 取消按鈕點擊事件
    $(document).on('click', '.cancel-btn', function () {
        var userId = $(this).data('id');
        cancelEdit(userId);
    });

    function enterEditMode(userId) {
        var row = $('#row-' + userId);

        // 儲存原始值
        var originalPassword = row.find('.password-text').text().trim();
        var originalEmail = row.find('.email-text').text().trim();
        var originalPhone = row.find('.phone-text').text().trim();

        row.data('original', {
            password: originalPassword,
            email: originalEmail,
            phone: originalPhone
        });

        // 顯示輸入框，隱藏文字
        row.find('.password-text').hide();
        row.find('.password-input').val(originalPassword).show();

        row.find('.email-text').hide();
        row.find('.email-input').val(originalEmail).show();

        row.find('.phone-text').hide();
        row.find('.phone-input').val(originalPhone).show();

        // 切換按鈕顯示
        row.find('.edit-btn').hide();
        row.find('.save-btn').show();
        row.find('.cancel-btn').show();

        // 添加編輯模式樣式
        row.addClass('edit-mode');
    }

    function saveChanges(userId) {
        var row = $('#row-' + userId);
        var newPassword = row.find('.password-input').val();
        var newEmail = row.find('.email-input').val();
        var newPhone = row.find('.phone-input').val();

        console.log('準備更新資料:', {
            id: userId,
            password: newPassword,
            email: newEmail,
            phone: newPhone
        });

        // 發送 AJAX 請求到後端
        $.ajax({
            url: '/Home/UpdateAccount',
            type: 'POST',
            data: {
                id: userId,
                password: newPassword,
                email: newEmail,
                phone: newPhone
            },
            success: function (response) {
                console.log('伺服器回應:', response);
                if (response.success) {
                    // 更新顯示的文字
                    row.find('.password-text').text(newPassword).show();
                    row.find('.email-text').text(newEmail || '').show();
                    row.find('.phone-text').text(newPhone || '').show();

                    // 隱藏輸入框
                    row.find('.password-input').hide();
                    row.find('.email-input').hide();
                    row.find('.phone-input').hide();

                    exitEditMode(userId);
                    alert('更新成功！');
                } else {
                    alert('更新失敗：' + response.message);
                    console.error('更新失敗詳細信息:', response);
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX 錯誤:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
                alert('更新失敗，請查看控制台獲取詳細信息');
            }
        });
    }

    function cancelEdit(userId) {
        var row = $('#row-' + userId);
        var original = row.data('original');

        if (original) {
            // 恢復原始值到文字顯示
            row.find('.password-text').text(original.password).show();
            row.find('.email-text').text(original.email).show();
            row.find('.phone-text').text(original.phone).show();

            // 恢復原始值到輸入框
            row.find('.password-input').val(original.password).hide();
            row.find('.email-input').val(original.email).hide();
            row.find('.phone-input').val(original.phone).hide();
        }

        exitEditMode(userId);
    }

    function exitEditMode(userId) {
        var row = $('#row-' + userId);

        // 切換按鈕顯示
        row.find('.edit-btn').show();
        row.find('.save-btn').hide();
        row.find('.cancel-btn').hide();

        // 移除編輯模式樣式
        row.removeClass('edit-mode');
    }
});