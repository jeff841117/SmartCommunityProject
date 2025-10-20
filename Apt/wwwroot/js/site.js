// === 導航欄功能 ===
document.addEventListener('DOMContentLoaded', function () {
    initNavbar();
});

// === 導航欄初始化 ===
function initNavbar() {
    const navbar = document.getElementById('mainNavbar');

    // 滾動效果
    window.addEventListener('scroll', function () {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    });

    // 行動裝置下拉選單
    document.querySelectorAll('.dropdown-toggle').forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            if (window.innerWidth < 992) {
                e.preventDefault();
                e.stopPropagation();
                toggle.classList.toggle('show');
                const menu = toggle.nextElementSibling;
                menu && menu.classList.toggle('show');
            }
        });
    });
}