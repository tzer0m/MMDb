// Shared by the talent and films pages: a filter form driving a DataTable that the server filters, sorts and pages.

/**
 * Escapes text for use in HTML.
 * @param {*} text The text to escape.
 */
function escapeHtml(text) {
    return String(text).replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
}

/**
 * Builds a coloured rating badge, or nothing if there is no rating.
 * @param {number|null} rating The rating.
 * @param {string} colour The background colour.
 * @param {string} classes Extra classes for the badge.
 * @param {string} [title] The tooltip.
 * @param {number} [decimals] How many decimal places to show.
 */
function ratingBadge(rating, colour, classes, title, decimals = 1) {
    return rating == null ? '' : `<span class="badge ${classes} text-black" style="background-color: ${colour}"${title ? ` title="${title}"` : ''}>${rating.toFixed(decimals)}</span>`;
}

/**
 * Formats a difference with its sign, e.g. +0.5, or nothing if there isn't one.
 * @param {number|null} delta The difference.
 */
function formatDelta(delta) {
    return delta == null ? '' : `${delta > 0 ? '+' : ''}${delta.toFixed(1)}`;
}

/**
 * Creates a DataTable whose rows come from the server, filtered by a form and kept in sync with the address bar.
 * The page needs elements with IDs based on the name: the table itself, name-filters (the form), name-filter-panel,
 * name-filter-toggle, name-filter-count, name-filter-clear and name-paging.
 * @param {string} name The table's ID, e.g. talent.
 * @param {object} options DataTable options: ajax as the data URL, order as the default sort, plus columns, language and so on.
 */
function createFilterTable(name, options) {
    const form = document.getElementById(`${name}-filters`);
    const panel = document.getElementById(`${name}-filter-panel`);
    const filterCount = document.getElementById(`${name}-filter-count`);
    const defaultOrder = options.order;
    // The form fields that have a value, which are sent with each request and kept in the address bar.
    const filterValues = () => [...new FormData(form)].filter(([, value]) => value.trim() !== '');
    const panelFilterCount = () => [...panel.querySelectorAll('[name]')].filter(field => field.value.trim() !== '').length;
    const params = new URLSearchParams(location.search);
    const sortColumn = Number(params.get('sort'));
    const table = new DataTable(`#${name}`, {
        serverSide: true,
        searching: false,
        autoWidth: false,
        pageLength: 100,
        createdRow: row => row.classList.add('position-relative'),
        layout: { topStart: null, topEnd: null, bottomStart: 'info', bottomEnd: 'paging' },
        ...options,
        ajax: { url: options.ajax, data: request => { for (const [key, value] of filterValues()) { request[key] = value; } } },
        order: sortColumn >= 1 && sortColumn < options.columns.length ? [[sortColumn, params.get('dir') === 'asc' ? 'asc' : 'desc']] : defaultOrder
    });
    document.getElementById(`${name}-paging`).append(document.querySelector(`#${name}_wrapper .dt-paging`));
    // After each load, put the filters and any non-default sort in the address bar and count the active panel filters.
    table.on('draw', () => {
        const query = new URLSearchParams(filterValues());
        const order = table.order();
        if (JSON.stringify(order) !== JSON.stringify(defaultOrder)) {
            query.set('sort', order[0][0]);
            query.set('dir', order[0][1]);
        }
        history.replaceState(null, '', query.size > 0 ? `?${query}` : location.pathname);
        filterCount.textContent = panelFilterCount() || '';
    });
    // Show the filters on larger screens, or whenever some are already set.
    if (window.matchMedia('(min-width: 768px)').matches || panelFilterCount() > 0) {
        panel.classList.add('show');
        document.getElementById(`${name}-filter-toggle`).setAttribute('aria-expanded', 'true');
    }
    // Reload after typing pauses or a dropdown changes, so each keystroke doesn't hit the server.
    let filterTimer;
    const reload = () => {
        clearTimeout(filterTimer);
        table.draw();
    };
    form.addEventListener('input', () => {
        clearTimeout(filterTimer);
        filterTimer = setTimeout(reload, 300);
    });
    form.addEventListener('submit', event => {
        event.preventDefault();
        reload();
    });
    document.getElementById(`${name}-filter-clear`).addEventListener('click', () => {
        panel.querySelectorAll('[name]').forEach(field => field.value = '');
        reload();
    });
    window.addEventListener('pageshow', event => {
        if (event.persisted) {
            reload();
        }
    });
    return table;
}
