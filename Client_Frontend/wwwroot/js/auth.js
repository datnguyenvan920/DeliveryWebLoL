// AJAX helpers for client-side calls to the API.
// If the API is hosted on a different origin, enable CORS on the API; otherwise use the server-side proxy handlers.

(function () {
    // window.__apiBase__ is injected in the Razor layout. If it's empty, fall back to the known API dev URL.
    const apiBase = window.__apiBase__ || 'https://localhost:7008';

    async function ajaxPost(path, payload) {
        const url = (apiBase ? apiBase.replace(/\/$/, '') : '') + '/' + path.replace(/^\//, '');
        try {
            const resp = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                credentials: 'include'
            });

            // Read raw text first to avoid json() throwing on empty/non-json responses
            const text = await resp.text();
            const contentType = (resp.headers.get('content-type') || '').toLowerCase();

            let json = null;
            if (text && contentType.includes('application/json')) {
                try {
                    json = JSON.parse(text);
                } catch (e) {
                    json = { message: 'Invalid JSON response', raw: text };
                }
            } else if (text) {
                // Non-JSON response (likely HTML error page) — include raw for debugging
                json = { message: 'Non-JSON response', raw: text };
            } else {
                json = null; // empty body
            }

            const result = { ok: resp.ok, json, status: resp.status, raw: text, headers: Object.fromEntries(resp.headers.entries()) };

            // Always log detailed response for debugging (visible in browser console)
            console.debug('ajaxPost response:', {
                url,
                status: result.status,
                ok: result.ok,
                headers: result.headers,
                json: result.json,
                raw: result.raw,
                apiBaseUsed: apiBase
            });

            return result;
        } catch (err) {
            console.debug('ajaxPost network error:', err.message);
            return { ok: false, json: { message: err.message }, status: 0 };
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        // Login AJAX
        const loginBtn = document.getElementById('ajaxLoginBtn');
        if (loginBtn) {
            loginBtn.addEventListener('click', async () => {
                const username = document.getElementById('ajaxUsername').value;
                const password = document.getElementById('ajaxPassword').value;
                const r = await ajaxPost('/auth/login', { username, password });
                const el = document.getElementById('result');
                el.style.color = r.ok ? 'green' : 'red';
                // Include HTTP status code in the UI message and show raw/json for failures
                if (r.ok) {
                    el.textContent = `Login successful (ajax). [HTTP ${r.status}]`;
                } else {
                    el.textContent = `Login failed: ${JSON.stringify(r.json)} [HTTP ${r.status}]`;
                }

                // Also print a concise log entry for quick visibility
                console.log('AJAX login result:', { status: r.status, ok: r.ok, json: r.json, raw: r.raw, apiBaseUsed: apiBase });
            });
        }

        // Register AJAX
        const regBtn = document.getElementById('ajaxRegisterBtn');
        if (regBtn) {
            regBtn.addEventListener('click', async () => {
                const username = document.getElementById('ajaxRegUsername').value;
                const password = document.getElementById('ajaxRegPassword').value;
                const phone = document.getElementById('ajaxRegPhone').value;
                const email = document.getElementById('ajaxRegEmail').value;
                const r = await ajaxPost('/auth/register', { username, password, phoneNum: phone, email, role: 4 });
                const el = document.getElementById('regResult');
                el.style.color = r.ok ? 'green' : 'red';
                if (r.ok) {
                    el.textContent = `Register request successful (ajax). [HTTP ${r.status}]`;
                } else {
                    el.textContent = `Register failed: ${JSON.stringify(r.json)} [HTTP ${r.status}]`;
                }
                console.log('AJAX register result:', { status: r.status, ok: r.ok, json: r.json, raw: r.raw, apiBaseUsed: apiBase });
            });
        }
    });
})();