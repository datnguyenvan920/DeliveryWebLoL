// DeliveryWebLoL — Dashboard JS (shared)
// Tab system, sidebar toggle, modal, CRUD helpers

(function () {
  'use strict';

  // ── Tab switching ──────────────────────────────────────
  document.querySelectorAll('.dash-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      const group = tab.closest('.dash-tabs')?.dataset.group || tab.dataset.group;
      const target = tab.dataset.tab;

      // Deactivate all tabs & panels in this group
      document.querySelectorAll(`.dash-tab[data-group="${group}"]`).forEach(t => t.classList.remove('active'));
      document.querySelectorAll(`.dash-tab-panel[data-group="${group}"]`).forEach(p => p.classList.remove('active'));

      tab.classList.add('active');
      const panel = document.querySelector(`.dash-tab-panel[data-group="${group}"][data-tab="${target}"]`);
      if (panel) panel.classList.add('active');

      // Sync sidebar item active state
      document.querySelectorAll('.sidebar-item').forEach(si => si.classList.remove('active'));
      const sidebarMatch = document.querySelector(`.sidebar-item[data-tab="${target}"]`);
      if (sidebarMatch) sidebarMatch.classList.add('active');
    });
  });

  // Sidebar item → activate tab
  document.querySelectorAll('.sidebar-item[data-tab]').forEach(item => {
    item.addEventListener('click', () => {
      const target = item.dataset.tab;
      const group = item.dataset.group || 'main';
      const matchingTab = document.querySelector(`.dash-tab[data-tab="${target}"]`);
      if (matchingTab) matchingTab.click();
    });
  });

  // ── Sidebar mobile toggle ──────────────────────────────
  const hamburger = document.getElementById('sidebarToggle');
  const sidebar = document.querySelector('.dash-sidebar');
  if (hamburger && sidebar) {
    hamburger.addEventListener('click', () => sidebar.classList.toggle('open'));
    document.addEventListener('click', e => {
      if (!sidebar.contains(e.target) && !hamburger.contains(e.target)) {
        sidebar.classList.remove('open');
      }
    });
  }

  // ── Modal system ───────────────────────────────────────
  window.openModal = function (id, data = {}) {
    const overlay = document.getElementById(id);
    if (!overlay) return;
    // Populate form fields from data
    Object.entries(data).forEach(([key, val]) => {
      const el = overlay.querySelector(`[name="${key}"], #modal-${key}`);
      if (el) { el.tagName === 'SELECT' ? (el.value = val) : (el.value = val); }
    });
    overlay.classList.add('open');
    document.body.style.overflow = 'hidden';
  };

  window.closeModal = function (id) {
    const overlay = document.getElementById(id);
    if (!overlay) return;
    overlay.classList.remove('open');
    document.body.style.overflow = '';
  };

  // Close on overlay click
  document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', e => {
      if (e.target === overlay) window.closeModal(overlay.id);
    });
    overlay.querySelector('.modal-close')?.addEventListener('click', () => window.closeModal(overlay.id));
  });

  // ── Driver order row expand/collapse ──────────────────
  document.querySelectorAll('.order-row').forEach(row => {
    row.addEventListener('click', (e) => {
      if (e.target.closest('button')) return; // don't collapse when clicking buttons
      const panel = row.nextElementSibling;
      if (panel && panel.classList.contains('order-detail-panel')) {
        const isOpen = panel.classList.contains('open');
        // Collapse all
        document.querySelectorAll('.order-detail-panel.open').forEach(p => {
          p.classList.remove('open');
          p.previousElementSibling?.classList.remove('active');
        });
        if (!isOpen) {
          panel.classList.add('open');
          row.classList.add('active');
        }
      }
    });
  });

  // ── Status option picker (Driver) ─────────────────────
  document.querySelectorAll('.status-option').forEach(opt => {
    opt.addEventListener('click', () => {
      opt.closest('.status-option-grid')?.querySelectorAll('.status-option').forEach(o => o.classList.remove('selected'));
      opt.classList.add('selected');
      const hiddenInput = opt.closest('.modal-box')?.querySelector('[name="newStatus"]');
      if (hiddenInput) hiddenInput.value = opt.dataset.status;
    });
  });

  // ── Skin card search filter ────────────────────────────
  window.filterCards = function (inputEl, gridSelector) {
    const q = inputEl.value.toLowerCase();
    document.querySelectorAll(gridSelector + ' .skin-card').forEach(card => {
      const name = card.querySelector('.skin-card-name')?.textContent.toLowerCase() || '';
      card.style.display = name.includes(q) ? '' : 'none';
    });
  };

  // ── Table search filter ────────────────────────────────
  window.filterTable = function (inputEl, tableSelector) {
    const q = inputEl.value.toLowerCase();
    document.querySelectorAll(tableSelector + ' tbody tr').forEach(row => {
      row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
    });
  };

  // ── Confirm delete ─────────────────────────────────────
  window.confirmDelete = function (name, callback) {
    if (confirm(`Delete "${name}"? This cannot be undone.`)) callback();
  };

  // ── Toggle deactivate ──────────────────────────────────
  window.toggleDeactivate = function (btn, userId) {
    const isActive = btn.textContent.trim() === 'Deactivate';
    btn.textContent = isActive ? 'Activate' : 'Deactivate';
    btn.classList.toggle('deactivate', !isActive);
    const badge = btn.closest('tr')?.querySelector('.badge');
    if (badge) {
      badge.className = 'badge ' + (isActive ? 'badge-inactive' : 'badge-active');
      badge.innerHTML = (isActive ? '' : '') + (isActive ? 'Inactive' : 'Active');
    }
    // TODO: call real API — fetch(`/api/users/${userId}/toggle`, { method:'POST' })
  };

  // No-sidebar pages: nothing else needed
  console.log('[DWL] Dashboard JS initialized');
})();
