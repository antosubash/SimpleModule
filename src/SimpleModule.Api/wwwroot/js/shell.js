function toggleUserMenu() {
    var dropdown = document.getElementById('user-dropdown');
    var btn = document.getElementById('user-menu-btn');
    var chevron = document.getElementById('user-menu-chevron');
    if (!dropdown) return;
    var isOpen = dropdown.classList.contains('open');
    if (isOpen) {
        dropdown.classList.remove('open');
        if (btn) btn.setAttribute('aria-expanded', 'false');
        if (chevron) chevron.style.transform = '';
    } else {
        dropdown.classList.add('open');
        if (btn) btn.setAttribute('aria-expanded', 'true');
        if (chevron) chevron.style.transform = 'rotate(180deg)';
    }
}

document.addEventListener('click', function(e) {
    var wrap = document.getElementById('user-menu');
    if (wrap && !wrap.contains(e.target)) {
        var dropdown = document.getElementById('user-dropdown');
        var btn = document.getElementById('user-menu-btn');
        var chevron = document.getElementById('user-menu-chevron');
        if (dropdown && dropdown.classList.contains('open')) {
            dropdown.classList.remove('open');
            if (btn) btn.setAttribute('aria-expanded', 'false');
            if (chevron) chevron.style.transform = '';
        }
    }
});
