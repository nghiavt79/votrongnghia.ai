/*
  JS của trang quản trị, chép từ khuôn /cms của Dokma, bỏ phần giá tiền.
  Mọi thứ ở đây là phần thêm cho tiện: tắt JS thì form vẫn lưu được, máy chủ vẫn
  kiểm đủ — chỉ mất phần gợi ý tại chỗ.
*/
(function () {
    'use strict';

    // Tiêu đề → đường dẫn, chỉ khi ô đường dẫn còn trống. Người gõ tay vào ô đường
    // dẫn thì thôi không điền đè lên nữa.
    var ten = document.querySelector('[data-tieu-de]');
    var slug = document.querySelector('[data-slug]');

    if (ten && slug) {
        var slugTuGo = slug.value !== '';
        var thanhSlug = function (text) {
            return text.toLowerCase().replace(/đ/g, 'd')
                .normalize('NFD').replace(/[̀-ͯ]/g, '')
                .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
        };
        slug.addEventListener('input', function () { slugTuGo = slug.value !== ''; });
        ten.addEventListener('input', function () {
            if (!slugTuGo) { slug.value = thanhSlug(ten.value); }
        });
    }

    // Bộ đếm ký tự: data-counter="120-200" là khoảng nên viết.
    document.querySelectorAll('[data-counter]').forEach(function (area) {
        var range = area.getAttribute('data-counter').split('-');
        var min = +range[0], max = +range[1];
        var out = document.createElement('div');
        out.className = 'form-text';
        area.insertAdjacentElement('afterend', out);

        var show = function () {
            var n = area.value.trim().length;
            out.textContent = n + ' ký tự' + (n === 0 ? '' : n < min ? ' — hơi ngắn' : n > max ? ' — hơi dài' : ' — vừa');
            out.classList.toggle('text-success', n >= min && n <= max);
        };
        area.addEventListener('input', show);
        show();
    });

    // Ô chọn tự gửi form khi đổi, đỡ một lần bấm "Lọc".
    document.querySelectorAll('[data-tu-gui]').forEach(function (select) {
        select.addEventListener('change', function () { select.form.submit(); });
    });

    document.querySelectorAll('[data-confirm]').forEach(function (button) {
        button.addEventListener('click', function (event) {
            if (!window.confirm(button.getAttribute('data-confirm'))) {
                event.preventDefault();
            }
        });
    });

    // Nút chép nhanh (link, email học viên).
    document.querySelectorAll('[data-chep]').forEach(function (button) {
        button.addEventListener('click', function () {
            navigator.clipboard.writeText(button.getAttribute('data-chep')).then(function () {
                var old = button.innerHTML;
                button.textContent = 'Đã chép';
                setTimeout(function () { button.innerHTML = old; }, 1500);
            });
        });
    });

    // Sửa dở mà bấm sang chỗ khác là mất chữ vừa gõ. Hỏi lại một nhịp.
    var guarded = document.querySelector('[data-dirty-guard]');

    if (guarded) {
        var dirty = false;
        var submitting = false;

        document.addEventListener('input', function (event) {
            if (event.target.form === guarded) { dirty = true; }
        });
        guarded.addEventListener('submit', function () { submitting = true; });
        window.addEventListener('beforeunload', function (event) {
            if (dirty && !submitting) {
                event.preventDefault();
                event.returnValue = '';
            }
        });
    }
})();
