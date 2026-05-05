$(document).ready(function () {
    const addEquipmentModalElement = document.getElementById('addEquipmentModal');
    const addEquipmentModal = addEquipmentModalElement
        ? bootstrap.Modal.getOrCreateInstance(addEquipmentModalElement)
        : null;

    $('.edit-btn').on('click', function () {
        const $row = $(this).closest('tr');
        $(this).hide();
        $row.find('.save-btn, .cancel-btn').show();
        $row.find('.field-display').hide();
        $row.find('.field-edit').show();
    });

    $('.cancel-btn').on('click', function () {
        const $row = $(this).closest('tr');
        $row.find('.edit-btn').show();
        $row.find('.save-btn, .cancel-btn').hide();

        $row.find('.field-edit').each(function () {
            const $input = $(this);
            const $display = $input.siblings('.field-display');
            if ($input.is('select')) {
                $input.val($display.text().trim());
            } else {
                $input.val($display.text().trim());
            }
        });

        $row.find('.field-display').show();
        $row.find('.field-edit').hide();
    });

    $('.save-btn').on('click', function () {
        const $row = $(this).closest('tr');
        const id = $row.data('id');

        const equipment = {
            Id: id,
            EquipmentName: $row.find('input[data-field="equipmentName"]').val(),
            // 這裡一定要抓編輯中的 select，否則會抓到顯示用 span 導致設備種類沒送出去。
            EquipmentCategory: $row.find('.field-edit[data-field="EquipmentCategory"]').val(),
            MaxUsers: $row.find('input[data-field="MaxUsers"]').val(),
            AvailableTime: $row.find('input[data-field="AvailableTime"]').val(),
            OpenTime: $row.find('input[data-field="OpenTime"]').val(),
            CloseTime: $row.find('input[data-field="CloseTime"]').val()
        };

        $.ajax({
            url: '/Equipment/UpdateEquipment',
            type: 'POST',
            data: equipment,
            success: function (response) {
                if (!response.success) {
                    alert('更新失敗：' + (response.message || '請稍後再試'));
                    return;
                }

                $row.find('.field-display[data-field="equipmentName"]').text(equipment.EquipmentName);
                $row.find('.field-display[data-field="EquipmentCategory"]').text(equipment.EquipmentCategory);
                $row.find('.field-display[data-field="MaxUsers"]').text(equipment.MaxUsers);
                $row.find('.field-display[data-field="AvailableTime"]').text(equipment.AvailableTime);
                $row.find('.field-display[data-field="OpenTime"]').text(equipment.OpenTime);
                $row.find('.field-display[data-field="CloseTime"]').text(equipment.CloseTime);

                $row.find('.edit-btn').show();
                $row.find('.save-btn, .cancel-btn').hide();
                $row.find('.field-display').show();
                $row.find('.field-edit').hide();

                alert('更新成功');
            },
            error: function (xhr, status, error) {
                alert('更新失敗：' + error);
            }
        });
    });

    $('#addEquipmentForm').on('submit', function (event) {
        event.preventDefault();

        const $form = $(this);
        const $error = $('#addEquipmentError');
        $error.addClass('d-none').text('');

        $.ajax({
            url: '/Equipment/CreateEquipmentModal',
            type: 'POST',
            data: $form.serialize(),
            success: function (response) {
                if (!response.success) {
                    $error.removeClass('d-none').text(response.message || '新增設備失敗');
                    return;
                }

                if (addEquipmentModal) {
                    addEquipmentModal.hide();
                }

                $form[0].reset();
                window.location.reload();
            },
            error: function (xhr, status, error) {
                $error.removeClass('d-none').text('新增設備失敗：' + error);
            }
        });
    });
});
