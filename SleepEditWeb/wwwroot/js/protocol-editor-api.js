function buildHeaders(requestVerificationToken, includeJsonContentType = true) {
    const headers = {};
    if (includeJsonContentType) {
        headers["Content-Type"] = "application/json";
    }

    if (requestVerificationToken) {
        headers.RequestVerificationToken = requestVerificationToken;
    }

    return headers;
}

async function safeReadJson(response) {
    return await response.json().catch(() => null);
}

export async function getProtocolEditorState(stateUrl) {
    try {
        const response = await fetch(stateUrl);
        const payload = await safeReadJson(response);

        if (!response.ok) {
            return {
                ok: false,
                error: payload?.error || "Unable to load protocol state."
            };
        }

        return { ok: true, payload };
    } catch {
        return {
            ok: false,
            error: "Unable to reach server while loading protocol state."
        };
    }
}

export async function postProtocolEditorAction(
    action,
    payload,
    requestVerificationToken,
    actionUrlTemplate = "/ProtocolEditor/${action}") {
    const actionUrl = actionUrlTemplate.replace("${action}", action);

    try {
        const response = await fetch(actionUrl, {
            method: "POST",
            headers: buildHeaders(requestVerificationToken),
            body: JSON.stringify(payload || {})
        });

        const responsePayload = await safeReadJson(response);
        if (!response.ok) {
            return {
                ok: false,
                error: responsePayload?.error || `Request failed: ${action}`
            };
        }

        return { ok: true, payload: responsePayload };
    } catch {
        return {
            ok: false,
            error: `Unable to reach server while processing ${action}.`
        };
    }
}

export async function uploadProtocolEditorXml(
    file,
    requestVerificationToken,
    uploadUrl = "/ProtocolEditor/ImportXmlUpload") {
    const payload = new FormData();
    payload.append("file", file);

    try {
        const response = await fetch(uploadUrl, {
            method: "POST",
            headers: buildHeaders(requestVerificationToken, false),
            body: payload
        });

        const responsePayload = await safeReadJson(response);
        if (!response.ok) {
            return {
                ok: false,
                error: responsePayload?.error || "Request failed: Import Protocol"
            };
        }

        return { ok: true, payload: responsePayload };
    } catch {
        return {
            ok: false,
            error: "Unable to reach server while importing protocol."
        };
    }
}

export async function saveProtocolEditorXml(
    requestVerificationToken,
    saveUrl = "/ProtocolEditor/SaveXml") {
    try {
        const response = await fetch(saveUrl, {
            method: "POST",
            headers: buildHeaders(requestVerificationToken, false)
        });

        const payload = await safeReadJson(response);
        if (!response.ok) {
            return {
                ok: false,
                error: payload?.error || "Request failed: Save Protocol"
            };
        }

        return { ok: true, payload };
    } catch {
        return {
            ok: false,
            error: "Unable to reach server while saving protocol."
        };
    }
}

export async function setProtocolEditorDefault(
    requestVerificationToken,
    setDefaultUrl = "/ProtocolEditor/SetDefaultProtocol") {
    try {
        const response = await fetch(setDefaultUrl, {
            method: "POST",
            headers: buildHeaders(requestVerificationToken, false)
        });

        const payload = await safeReadJson(response);
        if (!response.ok) {
            return {
                ok: false,
                error: payload?.error || "Request failed: SetDefaultProtocol"
            };
        }

        return { ok: true, payload };
    } catch {
        return {
            ok: false,
            error: "Unable to reach server while setting default protocol."
        };
    }
}
