$(document).ready(function () {
    const addAccountModalElement = document.getElementById('addAccountModal');
    const addAccountModal = addAccountModalElement
        ? bootstrap.Modal.getOrCreateInstance(addAccountModalElement)
        : null;

    $(document).on('click', '.edit-btn', function () {
        const userId = $(this).data('id');
        enterEditMode(userId);
    });

    $(document).on('click', '.save-btn', function () {
        const userId = $(this).data('id');
        saveChanges(userId);
    });

    $(document).on('click', '.cancel-btn', function () {
        const userId = $(this).data('id');
        cancelEdit(userId);
    });

    $(document).on('click', '.toggle-status-btn', function () {
        const userId = $(this).data('id');
        const willBeActive = String($(this).data('active')).toLowerCase() === 'true';
        toggleAccountStatus(userId, willBeActive);
    });

    $('#addAccountForm').on('submit', function (event) {
        event.preventDefault();

        const $form = $(this);
        const $error = $('#addAccountError');
        $error.addClass('d-none').text('');

        $.ajax({
            url: '/Home/CreateAccountModal',
            type: 'POST',
            data: $form.serialize(),
            success: function (response) {
                if (!response.success) {
                    $error.removeClass('d-none').text(response.message || '新增帳號失敗。');
                    return;
                }

                if (addAccountModal) {
                    addAccountModal.hide();
                }

                $form[0].reset();
                window.location.reload();
            },
            error: function (xhr, status, error) {
                $error.removeClass('d-none').text('新增帳號失敗：' + error);
            }
        });
    });

    function enterEditMode(userId) {
        const row = $('#row-' + userId);

        row.data('original', {
            age: row.find('.age-text').text().trim(),
            email: row.find('.email-text').text().trim(),
            phone: row.find('.phone-text').text().trim()
        });

        row.find('.password-text, .age-text, .email-text, .phone-text').hide();
        row.find('.password-input').val('').show();
        row.find('.age-input').show();
        row.find('.email-input').show();
        row.find('.phone-input').show();
        row.find('.edit-btn').hide();
        row.find('.save-btn, .cancel-btn').show();
        row.addClass('edit-mode');
    }

    function saveChanges(userId) {
        const row = $('#row-' + userId);

        $.ajax({
            url: '/Home/UpdateAccount',
            type: 'POST',
            data: {
                id: userId,
                password: row.find('.password-input').val(),
                age: row.find('.age-input').val(),
                email: row.find('.email-input').val(),
                phone: row.find('.phone-input').val()
            },
            success: function (response) {
                if (!response.success) {
                    alert('更新失敗：' + response.message);
                    return;
                }

                row.find('.password-text').text('已加密').show();
                row.find('.age-text').text(row.find('.age-input').val() || '').show();
                row.find('.email-text').text(row.find('.email-input').val() || '').show();
                row.find('.phone-text').text(row.find('.phone-input').val() || '').show();
                row.find('.password-input, .age-input, .email-input, .phone-input').hide();
                exitEditMode(userId);
                alert('更新成功。');
            },
            error: function (xhr, status, error) {
                alert('更新失敗：' + error);
            }
        });
    }

    function cancelEdit(userId) {
        const row = $('#row-' + userId);
        const original = row.data('original');
        if (original) {
            row.find('.password-text').text('已加密').show();
            row.find('.age-text').text(original.age).show();
            row.find('.email-text').text(original.email).show();
            row.find('.phone-text').text(original.phone).show();
            row.find('.password-input').val('').hide();
            row.find('.age-input').val(original.age).hide();
            row.find('.email-input').val(original.email).hide();
            row.find('.phone-input').val(original.phone).hide();
        }

        exitEditMode(userId);
    }

    function exitEditMode(userId) {
        const row = $('#row-' + userId);
        row.find('.edit-btn').show();
        row.find('.save-btn, .cancel-btn').hide();
        row.removeClass('edit-mode');
    }

    function toggleAccountStatus(userId, willBeActive) {
        const actionText = willBeActive ? '啟用' : '停用';
        if (!confirm('確定要' + actionText + '這個帳號嗎？')) {
            return;
        }

        $.ajax({
            url: '/Home/ToggleAccountStatus',
            type: 'POST',
            data: {
                id: userId,
                isActive: willBeActive
            },
            success: function (response) {
                if (!response.success) {
                    alert(actionText + '失敗：' + response.message);
                    return;
                }

                window.location.reload();
            },
            error: function (xhr, status, error) {
                alert(actionText + '失敗：' + error);
            }
        });
    }
});
