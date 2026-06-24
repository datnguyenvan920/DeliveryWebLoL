// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function apiBase() {
    const b = (window.__apiBase__ || "").trim();
    return b.endsWith("/") ? b.slice(0, -1) : b;
}

async function postJson(url, body) {
    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(body)
    });

    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { }

    if (!res.ok) {
        const msg = (data && data.message) ? data.message : (text || `${res.status} ${res.statusText}`);
        throw new Error(msg);
    }

    return data;
}

function showError(elId, message) {
    const el = document.getElementById(elId);
    if (!el) return;
    el.textContent = message;
    el.classList.remove("d-none");
}

function clearError(elId) {
    const el = document.getElementById(elId);
    if (!el) return;
    el.textContent = "";
    el.classList.add("d-none");
}

document.addEventListener("DOMContentLoaded", () => {
    const btnCreateWarehouse = document.getElementById("btnCreateWarehouse");
    if (btnCreateWarehouse) {
        btnCreateWarehouse.addEventListener("click", async () => {
            clearError("addWarehouseError");

            const name = (document.getElementById("whName")?.value || "").trim();
            const address = (document.getElementById("whAddress")?.value || "").trim();

            const latRaw = (document.getElementById("whLat")?.value || "").trim();
            const lngRaw = (document.getElementById("whLng")?.value || "").trim();

            const latitude = latRaw === "" ? null : Number(latRaw);
            const longitude = lngRaw === "" ? null : Number(lngRaw);

            if (!name) {
                showError("addWarehouseError", "Name is required.");
                return;
            }
            if ((latRaw !== "" && Number.isNaN(latitude)) || (lngRaw !== "" && Number.isNaN(longitude))) {
                showError("addWarehouseError", "Latitude/Longitude must be numbers.");
                return;
            }

            try {
                await postJson(`${apiBase()}/manager/warehouse`, {
                    name,
                    address: address === "" ? null : address,
                    latitude,
                    longitude
                });

                window.location.reload();
            } catch (e) {
                showError("addWarehouseError", e?.message || "Failed to create warehouse.");
            }
        });
    }

    const btnCreateAffiliate = document.getElementById("btnCreateAffiliate");
    if (btnCreateAffiliate) {
        btnCreateAffiliate.addEventListener("click", async () => {
            clearError("addAffiliateError");

            const whId = (document.getElementById("affWarehouseId")?.value || "").trim();
            const primaryLocId = (document.getElementById("affPrimaryLocationId")?.value || "").trim();

            if (!whId) {
                showError("addAffiliateError", "Warehouse Location ID is required.");
                return;
            }

            try {
                await postJson(`${apiBase()}/manager/deliverer`, {
                    warehouseLocationId: whId,
                    affiliatePrimaryLocationId: primaryLocId === "" ? null : primaryLocId
                });

                window.location.reload();
            } catch (e) {
                showError("addAffiliateError", e?.message || "Failed to create affiliate.");
            }
        });
    }
});
