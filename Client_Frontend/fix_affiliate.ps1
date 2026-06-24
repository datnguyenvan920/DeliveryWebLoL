param([string]$filePath)

$text = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)

# ─── Fix 1: Claim banner ─────────────────────────────────────────────────────
$oldClaimBlock = @'
    @if (Model.NeedAffiliationClaim)
    {
        <div class="af-claim">
            <div class="af-claim-title">Missing affiliation</div>
            <div class="af-claim-sub">
                Your account isn't linked to a warehouse yet. Paste the <strong>Affiliate Location Code (GUID)</strong> provided by your manager to claim your affiliation.
            </div>

            <form method="post" asp-page-handler="ClaimAffiliation">
                <div class="af-claim-row">
                    <input class="af-input" asp-for="ClaimCode" placeholder="e.g. 2f5b2c2f-7d7c-4f05-9a5a-5d4f8b6c9d12" autocomplete="off" />
                    <button class="af-btn-primary" type="submit">Claim</button>
                </div>
            </form>

            @if (!string.IsNullOrWhiteSpace(Model.ClaimMessage))
            {
                <div class="af-claim-msg">@Model.ClaimMessage</div>
            }
        </div>
    }
'@

# Try with smart-quote apostrophe (the actual corrupted char)
$oldClaimBlock2 = $oldClaimBlock -replace "isn't", "isn" + [char]0x2019 + "t"

$newClaimBlock = @'
    @{
        var claimSuccess = !string.IsNullOrWhiteSpace(Model.ClaimMessage) && Model.ClaimMessage.StartsWith("Affiliation claimed");
    }
    @if (Model.NeedAffiliationClaim && !claimSuccess)
    {
        <div class="af-claim">
            <div class="af-claim-title">Missing affiliation</div>
            <div class="af-claim-sub">
                Your account is not linked to a warehouse yet. Paste the <strong>Affiliate Location Code (GUID)</strong> provided by your manager to claim your affiliation.
            </div>

            <form method="post" asp-page-handler="ClaimAffiliation">
                @Html.AntiForgeryToken()
                <div class="af-claim-row">
                    <input class="af-input" asp-for="ClaimCode" placeholder="e.g. 2f5b2c2f-7d7c-4f05-9a5a-5d4f8b6c9d12" autocomplete="off" />
                    <button class="af-btn-primary" type="submit">Claim</button>
                </div>
            </form>

            @if (!string.IsNullOrWhiteSpace(Model.ClaimMessage))
            {
                <div class="af-claim-msg">@Model.ClaimMessage</div>
            }
        </div>
    }
    @if (claimSuccess)
    {
        <div class="af-claim" style="border-color:rgba(34,197,94,0.35);background:rgba(34,197,94,0.06);">
            <div class="af-claim-title" style="color:#4ade80;">Affiliation Active</div>
            <div class="af-claim-sub">@Model.ClaimMessage Reload to see your dashboard.</div>
        </div>
    }
'@

if ($text.IndexOf("Your account isn") -ge 0) {
    $text = $text -replace '(?s)@if \(Model\.NeedAffiliationClaim\).*?}(\s*)', $newClaimBlock
    Write-Host "Claim block replaced"
} else {
    Write-Host "Claim block NOT found in file"
}

# ─── Fix 2: Create order fetch ────────────────────────────────────────────────
$oldFetch = @'
    // Create order
    document.getElementById('afModalCreate')?.addEventListener('click', async function(){
        hideErr();

        if (cart.size === 0) {
            showErr('Add at least one item.');
            return;
        }

        var dest = document.getElementById('afDestLocationId')?.value?.trim();
        if (!dest) {
            showErr('Destination Location ID is required (GUID).');
            return;
        }

        // Resolve warehouse: take from first item row (they all belong to same warehouse in this UI)
        // We need the source warehouse locationId; backend validates affiliate linkage anyway.
        // For now, ask backend to use the linked warehouse by passing it from first order item list's inventory row.
        // We don't have warehouse id in the DTO here, so the API requires it.
        // Derive from existing orders if possible.
        var sourceWarehouse = '@(Model.Orders.FirstOrDefault()?.OrderId ?? "")';
        // The real source warehouse id is embedded in API; this UI expects API to validate.

        // Instead: require the user to input a source warehouse id is not desired.
        // We'll set it to empty and the API will reject. Provide a hint.

        var payload = {
            sourceWarehouseLocationId: (document.getElementById('afWarehouseLocationId')?.value || '').trim(),
            destinationLocationId: dest,
            orderType: parseInt(document.getElementById('afOrderType').value || '0', 10),
            items: Array.from(cart.values()).map(function(v){ return { itemId: v.itemId, quantity: v.qty }; })
        };

        if (!payload.sourceWarehouseLocationId) {
            showErr('No linked warehouse found for your account. Contact manager to link your affiliate to a warehouse.');
            return;
        }

        try {
            var apiBase = (window.__apiBase__ || '').toString();
            var url = (apiBase ? apiBase.replace(/\/+$/, '') + '/' : '/') + 'affiliate/order';

            var token = sessionStorage.getItem('access_token');
            // token is stored in server session, not browser storage. call same-origin relative path via BFF is out of scope.
            // Use relative URL to frontend's configured API client via server-side proxy is not available here.

            var res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                var txt = await res.text();
                throw new Error('API error ' + res.status + ': ' + txt);
            }

            closeModal();
            location.reload();
        } catch (e) {
            showErr(e.message || String(e));
        }
    });
'@

$newFetch = @'
    // Create order via BFF (server-side proxy carries the Bearer token)
    document.getElementById('afModalCreate')?.addEventListener('click', async function(){
        hideErr();

        if (cart.size === 0) { showErr('Add at least one item.'); return; }

        var dest = (document.getElementById('afDestLocationId')?.value || '').trim();
        if (!dest) { showErr('Destination Location ID is required (GUID).'); return; }

        var src = (document.getElementById('afWarehouseLocationId')?.value || '').trim();
        if (!src) { showErr('No linked warehouse found for your account. Contact manager to link your affiliate to a warehouse.'); return; }

        var items = Array.from(cart.values()).map(function(v){ return { itemId: v.itemId, quantity: v.qty }; });
        var aft = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';

        var fd = new FormData();
        fd.append('__RequestVerificationToken', aft);
        fd.append('sourceWarehouseLocationId', src);
        fd.append('destinationLocationId', dest);
        fd.append('orderType', document.getElementById('afOrderType').value || '0');
        fd.append('itemsJson', JSON.stringify(items));

        var createBtn = document.getElementById('afModalCreate');
        createBtn.disabled = true;
        createBtn.textContent = 'Creating' + String.fromCharCode(0x2026);

        try {
            var res = await fetch('?handler=CreateOrder', { method: 'POST', body: fd });
            var json = await res.json();

            createBtn.disabled = false;
            createBtn.textContent = 'Create';

            if (!json.success) { showErr(json.message || 'Failed to create order.'); return; }

            closeModal();
            cart.clear();
            location.reload();
        } catch (e) {
            createBtn.disabled = false;
            createBtn.textContent = 'Create';
            showErr('Network error: ' + (e.message || String(e)));
        }
    });
'@

if ($text.IndexOf("affiliate/order") -ge 0) {
    # Use regex to replace the whole create order handler
    $text = $text -replace '(?s)    // Create order\r?\n    document\.getElementById\(''afModalCreate''\).*?    \}\);', $newFetch.TrimEnd()
    Write-Host "Create order fetch replaced"
} else {
    Write-Host "Create order fetch NOT found"
}

[System.IO.File]::WriteAllText($filePath, $text, [System.Text.Encoding]::UTF8)
Write-Host "File saved."
