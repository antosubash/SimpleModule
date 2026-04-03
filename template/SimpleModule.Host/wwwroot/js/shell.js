function _toggleUserMenu() {
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

document.addEventListener('click', (e) => {
  var wrap = document.getElementById('user-menu');
  if (wrap && !wrap.contains(e.target)) {
    var dropdown = document.getElementById('user-dropdown');
    var btn = document.getElementById('user-menu-btn');
    var chevron = document.getElementById('user-menu-chevron');
    if (dropdown?.classList.contains('open')) {
      dropdown.classList.remove('open');
      if (btn) btn.setAttribute('aria-expanded', 'false');
      if (chevron) chevron.style.transform = '';
    }
  }
});

// ── Mobile sidebar ──────────────────────────────────────
(function () {
  var sidebar = document.getElementById('app-sidebar');
  var backdrop = document.getElementById('app-sidebar-backdrop');
  var hamburger = document.getElementById('app-mobile-hamburger');
  if (!sidebar) return;

  function openSidebar() {
    sidebar.classList.add('app-sidebar-open');
    if (backdrop) backdrop.classList.add('visible');
    if (hamburger) {
      hamburger.setAttribute('aria-expanded', 'true');
      hamburger.setAttribute('aria-label', 'Close navigation');
    }
    document.body.style.overflow = 'hidden';
  }

  function closeSidebar() {
    sidebar.classList.remove('app-sidebar-open');
    if (backdrop) backdrop.classList.remove('visible');
    if (hamburger) {
      hamburger.setAttribute('aria-expanded', 'false');
      hamburger.setAttribute('aria-label', 'Open navigation');
    }
    document.body.style.overflow = '';
  }

  function isMobile() {
    return !window.matchMedia('(min-width: 768px)').matches;
  }

  // Public API
  window.openMobileSidebar = openSidebar;
  window.closeMobileSidebar = closeSidebar;

  // Backdrop click
  if (backdrop) {
    backdrop.addEventListener('click', closeSidebar);
  }

  // Escape key
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && isMobile() && sidebar.classList.contains('app-sidebar-open')) {
      closeSidebar();
    }
  });

  // Close on desktop resize
  var mql = window.matchMedia('(min-width: 768px)');
  mql.addEventListener('change', function (e) {
    if (e.matches) {
      closeSidebar();
    }
  });

  // Swipe gesture
  var touchStartX = 0;
  var touchStartY = 0;
  var swiping = false;

  document.addEventListener('touchstart', function (e) {
    var touch = e.touches[0];
    touchStartX = touch.clientX;
    touchStartY = touch.clientY;
    swiping = false;
    // Only detect edge swipe when sidebar is closed
    if (!sidebar.classList.contains('app-sidebar-open') && touchStartX <= 25) {
      swiping = true;
    }
    // Detect swipe-to-close on sidebar or backdrop
    if (sidebar.classList.contains('app-sidebar-open')) {
      swiping = true;
    }
  }, { passive: true });

  document.addEventListener('touchend', function (e) {
    if (!swiping || !isMobile()) return;
    var touch = e.changedTouches[0];
    var dx = touch.clientX - touchStartX;
    var dy = Math.abs(touch.clientY - touchStartY);
    // Ignore vertical scrolls
    if (dy > Math.abs(dx)) return;
    var isOpen = sidebar.classList.contains('app-sidebar-open');
    if (!isOpen && dx > 50 && touchStartX <= 25) {
      openSidebar();
    } else if (isOpen && dx < -50) {
      closeSidebar();
    }
  }, { passive: true });
})();
