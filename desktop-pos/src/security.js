const { URL } = require("node:url");

const DEFAULT_WEB_URL = "https://pizzeriapadua-production.up.railway.app/PizzeriaPadua/gestion.html";

function resolvePaduaUrl() {
  const configured = process.env.PIZZERIA_PADUA_URL;
  if (configured) return configured.endsWith("/") ? configured : `${configured}/`;
  return DEFAULT_WEB_URL;
}

function isAllowedNavigation(candidate, appUrl) {
  try {
    const target = new URL(candidate);
    const app = new URL(appUrl);
    if (target.origin === app.origin) return true;
    return ["localhost", "127.0.0.1"].includes(target.hostname);
  } catch {
    return false;
  }
}

module.exports = { resolvePaduaUrl, isAllowedNavigation };
