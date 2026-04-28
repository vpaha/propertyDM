const documentView = { handle: null };
const claimSearchView = { handle: null };
const themeStorageKey = "themevar";
const defaultThemeName = "custom1";
const defaultCulture = "en-US";

document.addEventListener("DOMContentLoaded", () =>
{
    console.log("DOMContentLoaded fired");
});

document.addEventListener("blazor:enhancedload", async () =>
{
    console.log("blazor:enhancedload fired");

    const settings = window.getTheme();
    await window.setCulture(settings.culture);
    window.setTheme(settings);
});

function getCookie(name)
{
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length !== 2)
        return null;

    return parts.pop().split(";").shift();
}

function getCultureFromCookie()
{
    const cultureCookie = getCookie(".AspNetCore.Culture");
    if (!cultureCookie)
        return defaultCulture;

    const match = cultureCookie.match(/c=([^|]+)/);
    return match?.[1] || defaultCulture;
}

function parseThemeCookie(raw)
{
    if (!raw)
        return null;

    const parts = raw.split("|");
    if (parts.length !== 2)
        return null;

    return {
        themeName: parts[0] || defaultThemeName,
        isDarkMode: parts[1] === "1",
        culture: getCultureFromCookie()
    };
}

function getDefaultSettings()
{
    return {
        themeName: defaultThemeName,
        isDarkMode: false,
        culture: getCultureFromCookie()
    };
}

function tryGetLocalSettings()
{
    try
    {
        const raw = localStorage.getItem(themeStorageKey);
        if (!raw)
            return null;

        const parsed = JSON.parse(raw);
        if (!parsed)
            return null;

        return {
            themeName: parsed.themeName || defaultThemeName,
            isDarkMode: parsed.isDarkMode ?? false,
            culture: parsed.culture || getCultureFromCookie()
        };
    }
    catch
    {
        return null;
    }
}

window.getTheme = function ()
{
    const cookieTheme = parseThemeCookie(getCookie("theme"));
    const localSettings = tryGetLocalSettings();

    if (cookieTheme)
    {
        return {
            themeName: cookieTheme.themeName,
            isDarkMode: cookieTheme.isDarkMode,
            culture: localSettings?.culture || cookieTheme.culture || defaultCulture
        };
    }

    return localSettings || getDefaultSettings();
};

window.setTheme = function (options)
{
    const settings = {
        themeName: options?.themeName || defaultThemeName,
        isDarkMode: options?.isDarkMode ?? false,
        culture: options?.culture || getCultureFromCookie()
    };

    const link = document.getElementById("theme");
    if (link)
    {
        link.href = `css/${settings.themeName}${settings.isDarkMode ? "-dark" : ""}.css`;
    }

    localStorage.setItem(themeStorageKey, JSON.stringify(settings));

    return settings;
};

window.setCultureInTheme = function (culture)
{
    const settings = window.getTheme();
    settings.culture = culture || defaultCulture;

    localStorage.setItem(themeStorageKey, JSON.stringify(settings));

    return settings;
};

window.setCulture = async function (culture)
{
    const targetCulture = culture || defaultCulture;

    const response = await fetch("config/culture", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ Culture: targetCulture })
    });

    if (!response.ok)
    {
        throw new Error(`Failed to persist culture. Status: ${response.status}`);
    }

    window.setCultureInTheme(targetCulture);
};

async function initAttachment(name, parameters)
{
    console.log("Registering the Attachment handler");
    console.log({ name, parameters });
}

async function initClaimSearch(name, parameters)
{
    console.log("Registering the Claim Search handler");
    console.log({ name, parameters });
}

async function renderComponent(handleRef, elementId, componentName, params = {})
{
    if (!window.Blazor?.rootComponents)
        return;

    const element = document.getElementById(elementId);
    if (!element)
        return;

    handleRef.handle = await Blazor.rootComponents.add(element, componentName, params);
}

async function removeAttachments()
{
    await removeComponent(documentView);
}

async function removeClaims()
{
    await removeComponent(claimSearchView);
}

async function removeComponent(handleRef)
{
    if (!handleRef?.handle)
        return;

    await handleRef.handle.dispose().catch(() => { });
    handleRef.handle = null;
}

function renderAttachment(referenceType, referenceId)
{
    removeAttachments();

    return renderComponent(documentView, "blazor-document", "document", {
        ReferenceType: referenceType,
        ReferenceId: referenceId
    });
}

function renderClaimSearchView()
{
    removeClaims();
    return renderComponent(claimSearchView, "blazor-search", "claimSearch");
}

(() =>
{
    const settings = window.getTheme();
    window.setTheme(settings);
})();

window.getBrowserLocation = () =>
{
    return new Promise((resolve, reject) =>
    {
        navigator.geolocation.getCurrentPosition(
            pos => resolve({
                lat: pos.coords.latitude,
                lng: pos.coords.longitude
            }),
            err => reject(err.message)
        );
    });
};

window.setClipboard = function (text)
{
    if (!text) return;
    navigator.clipboard.writeText(text);
};

window.addEventListener("error", function (event)
{
    if (event.message && event.message.includes("Failed to complete negotiation"))
    {
        showConnectionError();
    }
});

window.addEventListener("unhandledrejection", function (event)
{
    const msg = event.reason?.message || "";
    if (msg.includes("Failed to complete negotiation") ||
        msg.includes("403") ||
        msg.includes("negotiate"))
    {
        showConnectionError();
    }
});

function showConnectionError()
{
    const el = document.getElementById("blazor-error-ui");

    if (el)
    {
        el.innerHTML = `
                <div style="
                    padding:16px;
                    background:#fff3cd;
                    color:#856404;
                    border:1px solid #ffeeba;
                    border-radius:6px;
                    font-family:Segoe UI, Arial;
                ">
                    Connection to the server failed.  
                    Please refresh the page or contact support if the issue persists.
                </div>
            `;
        el.style.display = "block";
    }
}