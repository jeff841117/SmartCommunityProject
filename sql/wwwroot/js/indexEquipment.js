$(document).ready(function () {
    // 編輯按鈕點擊事件
    $('.edit-btn').click(function () {
        var $row = $(this).closest('tr');
        // 隱藏編輯按鈕，顯示保存和取消按鈕
        $(this).hide();
        $row.find('.save-btn, .cancel-btn').show();
        // 隱藏顯示文本，顯示輸入框
        $row.find('.field-display').hide();
        $row.find('.field-edit').show();
    });

    // 取消按鈕點擊事件
    $('.cancel-btn').click(function () {
        var $row = $(this).closest('tr');
        // 顯示編輯按鈕，隱藏保存和取消按鈕
        $row.find('.edit-btn').show();
        $row.find('.save-btn, .cancel-btn').hide();
        // 顯示顯示文本，隱藏輸入框，並重置輸入框的值（取消修改）
        $row.find('.field-edit').each(function () {
            var $input = $(this);
            var $display = $input.siblings('.field-display');
            $input.val($display.text());
        });
        $row.find('.field-display').show();
        $row.find('.field-edit').hide();
    });

    // 保存按鈕點擊事件
    $('.save-btn').click(function () {
        var $row = $(this).closest('tr');
        var id = $row.data('id');

        // 確保屬性名稱與 Equipment 類別完全一致
        var equipment = {
            Id: id,
            EquipmentName: $row.find('input[data-field="equipmentName"]').val(),
            EquipmentCategory: $row.find('[data-field="EquipmentCategory"]').val(),
            MaxUsers: $row.find('input[data-field="MaxUsers"]').val(),
            AvailableTime: $row.find('input[data-field="AvailableTime"]').val(),
            OpenTime: $row.find('input[data-field="OpenTime"]').val(),
            CloseTime: $row.find('input[data-field="CloseTime"]').val()
        };

        console.log('發送數據:', equipment); // 除錯用

        // 發送AJAX請求到伺服器
        $.ajax({
            url: '/Equipment/UpdateEquipment',
            type: 'POST',
            data: equipment,
            success: function (response) {
                if (response.success) {
                    // 更新顯示文本
                    $row.find('.field-display[data-field="equipmentName"]').text(equipment.EquipmentName);
                    $row.find('.field-display[data-field="EquipmentCategory"]').text(equipment.EquipmentCategory);
                    $row.find('.field-display[data-field="MaxUsers"]').text(equipment.MaxUsers);
                    $row.find('.field-display[data-field="AvailableTime"]').text(equipment.AvailableTime);
                    $row.find('.field-display[data-field="OpenTime"]').text(equipment.OpenTime);
                    $row.find('.field-display[data-field="CloseTime"]').text(equipment.CloseTime);

                    // 切換回顯示模式
                    $row.find('.edit-btn').show();
                    $row.find('.save-btn, .cancel-btn').hide();
                    $row.find('.field-display').show();
                    $row.find('.field-edit').hide();

                    alert('更新成功');
                } else {
                    alert('更新失敗: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                alert('請求失敗: ' + error);
            }
        });
    });
});
