const path = require("node:path");
const { app, BrowserWindow, shell, session } = require("electron");
const { resolvePaduaUrl, isAllowedNavigation } = require("./security");

const PADUA_URL = resolvePaduaUrl();
const APP_ID = "mx.pizzeriapadua.desktop";
let mainWindow;

app.setAppUserModelId(APP_ID);
app.commandLine.appendSwitch("disable-pinch");

function openExternalSafely(url) {
  if (/^https:\/\//i.test(url)) void shell.openExternal(url);
}

function showOfflinePage(window, failedUrl = PADUA_URL) {
  if (window.isDestroyed()) return;
  const retryUrl = isAllowedNavigation(failedUrl, PADUA_URL) ? failedUrl : PADUA_URL;
  void window.loadFile(path.join(__dirname, "offline.html"), {
    query: { url: retryUrl }
  });
}

function createMainWindow() {
  mainWindow = new BrowserWindow({
    width: 1440,
    height: 900,
    minWidth: 960,
    minHeight: 640,
    show: false,
    autoHideMenuBar: true,
    backgroundColor: "#f1eff8",
    icon: path.join(__dirname, "..", "assets", "icon.png"),
    title: "Pizzería Padua",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      spellcheck: false
    }
  });

  mainWindow.maximize();
  mainWindow.once("ready-to-show", () => mainWindow.show());

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    if (isAllowedNavigation(url, PADUA_URL)) {
      void mainWindow.loadURL(url);
    } else {
      openExternalSafely(url);
    }
    return { action: "deny" };
  });

  mainWindow.webContents.on("will-navigate", (event, url) => {
    if (isAllowedNavigation(url, PADUA_URL)) return;
    event.preventDefault();
    openExternalSafely(url);
  });

  mainWindow.webContents.on("did-fail-load", (_event, errorCode, _description, validatedUrl, isMainFrame) => {
    if (!isMainFrame || errorCode === -3) return;
    showOfflinePage(mainWindow, validatedUrl);
  });

  mainWindow.webContents.on("did-finish-load", () => {
    mainWindow.webContents.insertCSS(`
      html { scrollbar-color: #714b67 #f1eff8; }
      ::-webkit-scrollbar { width: 10px; height: 10px; }
      ::-webkit-scrollbar-thumb { background: #714b67; border-radius: 8px; }
    `).catch(() => {});
  });

  void mainWindow.loadURL(PADUA_URL).catch(() => showOfflinePage(mainWindow));
  mainWindow.on("closed", () => { mainWindow = undefined; });
}

const lock = app.requestSingleInstanceLock();
if (!lock) {
  app.quit();
} else {
  app.on("second-instance", () => {
    if (!mainWindow) return;
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
  });

  app.whenReady().then(() => {
    session.defaultSession.setPermissionRequestHandler((_webContents, permission, callback) => {
      callback(permission === "media");
    });
    createMainWindow();
  });

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createMainWindow();
  });

  app.on("window-all-closed", () => {
    if (process.platform !== "darwin") app.quit();
  });
}
