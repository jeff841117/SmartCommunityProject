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
                    $error.removeClass('d-none').text(response.message || '新增帳號失敗');
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
            password: row.find('.password-text').text().trim(),
            email: row.find('.email-text').text().trim(),
            phone: row.find('.phone-text').text().trim()
        });

        row.find('.password-text, .email-text, .phone-text').hide();
        row.find('.password-input').show();
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
                email: row.find('.email-input').val(),
                phone: row.find('.phone-input').val()
            },
            success: function (response) {
                if (!response.success) {
                    alert('更新失敗：' + response.message);
                    return;
                }

                row.find('.password-text').text(row.find('.password-input').val()).show();
                row.find('.email-text').text(row.find('.email-input').val() || '').show();
                row.find('.phone-text').text(row.find('.phone-input').val() || '').show();
                row.find('.password-input, .email-input, .phone-input').hide();
                exitEditMode(userId);
                alert('更新成功');
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
            row.find('.password-text').text(original.password).show();
            row.find('.email-text').text(original.email).show();
            row.find('.phone-text').text(original.phone).show();
            row.find('.password-input').val(original.password).hide();
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
});
