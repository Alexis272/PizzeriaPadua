let order = [];
let total = 0;
let myLastOrderId = parseInt(localStorage.getItem("myLastOrderId") || "0", 10) || null;
try { var supportsWebP = document.createElement("canvas").toDataURL("image/webp").indexOf("image/webp") === 5; } catch (e) { var supportsWebP = false; }

function resolveImageUrl(url) {
  if (!url) return "img/hero-tech.jpg";
  if (/^(https?:|data:|blob:)/i.test(url)) return url;
  if (url.startsWith("/img/")) return url.substring(1);
  if (url.startsWith("/promo/")) return url.substring(1);
  return url;
}

// --- Auth ---
var currentUser = null;
try { var u = localStorage.getItem("mt_user"); if (u) currentUser = JSON.parse(u); } catch(e) {}
var sessionHeartbeatTimer = null;
var mercadoPagoConfigured = false;

var licenseValid = false;

function checkLicense() {
  return fetch("/Pizzería Padua/api/v1/License/status?_=" + Date.now()).then(function(r) { return r.json(); }).then(function(d) {
    licenseValid = d.licensed && !d.expired;
    var btn = document.getElementById("userBtn");
    if (!licenseValid) {
      if (btn) { btn.style.pointerEvents = "none"; btn.style.opacity = "0.4"; btn.title = "Licencia expirada"; }
    } else if (btn) {
      btn.style.pointerEvents = "";
      btn.style.opacity = "";
      btn.title = "Iniciar sesión";
    }
    document.querySelectorAll(".product button").forEach(function(productButton) {
      if (productButton.textContent.trim() === "+") productButton.style.display = licenseValid ? "" : "none";
    });
    return licenseValid;
  }).catch(function() {
    licenseValid = false;
    return false;
  });
}

function openAuth() {
  document.getElementById("authModal").classList.add("open");
  document.getElementById("facebookError").textContent = "";
  initializeGoogleSignIn();
}

function checkMercadoPagoConfiguration() {
  const button = document.getElementById("checkoutButton");
  const note = document.getElementById("paymentFeeNote");
  return fetch("/Pizzería Padua/api/v1/Payments/mercadopago/status?_=" + Date.now())
    .then(response => response.ok ? response.json() : { configured: false })
    .then(data => {
      mercadoPagoConfigured = data.configured === true;
      if (button) {
        button.disabled = false;
        button.textContent = mercadoPagoConfigured ? "Comprar con Mercado Pago" : "Enviar pedido";
      }
      if (note) note.style.display = mercadoPagoConfigured ? "block" : "none";
    })
    .catch(() => {
      mercadoPagoConfigured = false;
      if (button) { button.disabled = false; button.textContent = "Enviar pedido"; }
      if (note) note.style.display = "none";
    });
}
function closeAuth() { document.getElementById("authModal").classList.remove("open"); }

function toggleUserMenu() {
  var menu = document.getElementById("userMenu");
  menu.classList.toggle("show");
}

document.addEventListener("click", function(e) {
  var menu = document.getElementById("userMenu");
  var wrapper = document.querySelector(".user-icon-wrapper");
  if (menu && wrapper && !wrapper.contains(e.target)) {
    menu.classList.remove("show");
  }
});

function switchAuthTab(tab) {
  document.querySelectorAll(".auth-tab").forEach(function(t) { t.classList.toggle("active", t.dataset.tab === tab); });
  document.getElementById("loginForm").style.display = tab === "login" ? "" : "none";
  document.getElementById("registerStep1").style.display = tab === "register" ? "" : "none";
  document.getElementById("registerForm").style.display = "none";
}
function togglePwd(id, el) {
  var inp = document.getElementById(id);
  inp.type = inp.type === "password" ? "text" : "password";
  el.innerHTML = inp.type === "password" ? "&#128065;" : "&#128064;";
}

function setUser(user) {
  currentUser = user;
  if (user) {
    localStorage.setItem("mt_user", JSON.stringify(user));
    document.getElementById("userBtn").style.display = "none";
    document.getElementById("userProfile").style.display = "flex";
    document.getElementById("userDisplay").textContent = user.username;
    
    var profilePic = document.getElementById("userProfilePic");
    var iconDefault = document.getElementById("userIconDefault");
    
    if (user.profilePicture) {
      profilePic.src = user.profilePicture;
      profilePic.style.display = "block";
      iconDefault.style.display = "none";
    } else {
      profilePic.style.display = "none";
      iconDefault.style.display = "block";
    }
    
    // Actualizar nombre en el carrito
    var customerNameDisplay = document.getElementById("customerNameDisplay");
    if (customerNameDisplay) {
      customerNameDisplay.textContent = "Pedido para: " + user.username;
    }
    
    // Cargar pedidos activos del usuario
    loadUserActiveOrders();
    startUserSessionHeartbeat();
    var orderMsg = document.getElementById("orderMsg");
    if (orderMsg) orderMsg.textContent = "";
    checkLicense().then(function() { loadProducts(currentCategoryId || null); });
  } else {
    stopUserSessionHeartbeat();
    localStorage.removeItem("mt_user");
    document.getElementById("userBtn").style.display = "flex";
    document.getElementById("userProfile").style.display = "none";
    
    var customerNameDisplay = document.getElementById("customerNameDisplay");
    if (customerNameDisplay) {
      customerNameDisplay.textContent = "";
    }
  }
  if (typeof showReviewForm === "function") showReviewForm();
}

function stopUserSessionHeartbeat() {
  if (sessionHeartbeatTimer) {
    clearInterval(sessionHeartbeatTimer);
    sessionHeartbeatTimer = null;
  }
}

function startUserSessionHeartbeat() {
  stopUserSessionHeartbeat();
  if (!currentUser || !currentUser.sessionToken) return;
  sendUserSessionHeartbeat();
  sessionHeartbeatTimer = setInterval(sendUserSessionHeartbeat, 15000);
}

function sendUserSessionHeartbeat() {
  if (!currentUser || !currentUser.sessionToken) return;
  var sessionUserId = currentUser.id;
  var sessionToken = currentUser.sessionToken;

  fetch("/Pizzería Padua/api/v1/Accounts/session/heartbeat", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: sessionUserId, sessionToken: sessionToken })
  }).then(function(response) {
    if (response.status === 401 && currentUser && currentUser.sessionToken === sessionToken) {
      logout(false);
      alert("Tu sesión fue cerrada por el administrador.");
    }
  }).catch(function() {
    // Una falla temporal de red no debe cerrar la sesión del usuario.
  });
}

function login(e) {
  e.preventDefault();
  var btn = e.target.querySelector("button[type=submit]");
  btn.disabled = true; btn.textContent = "Entrando...";
  fetch("/Pizzería Padua/api/v1/Accounts/login", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: document.getElementById("loginUser").value.trim(), password: document.getElementById("loginPass").value })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) { setUser(d.user); closeAuth(); } else { document.getElementById("loginError").textContent = d.message || "Error"; }
  }).catch(function() { document.getElementById("loginError").textContent = "Error de conexión"; })
  .then(function() { btn.disabled = false; btn.textContent = "Entrar"; });
}

function facebookLogin() {
  if (typeof FB === 'undefined') {
    document.getElementById("facebookError").textContent = "Facebook SDK no cargado. Recarga la página.";
    document.getElementById("facebookError").style.color = "#e74c3c";
    return;
  }

  FB.login(function(response) {
    if (response.authResponse) {
      var accessToken = response.authResponse.accessToken;
      fetch("/Pizzería Padua/api/v1/Accounts/facebook-login", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ accessToken: accessToken })
      }).then(function(r) { return r.json(); }).then(function(d) {
        if (d.success) {
          setUser(d.user);
          closeAuth();
        } else {
          document.getElementById("facebookError").textContent = d.message || "Error al iniciar sesión con Facebook";
          document.getElementById("facebookError").style.color = "#e74c3c";
        }
      }).catch(function(err) {
        document.getElementById("facebookError").textContent = "Error de conexión. El servidor puede estar reiniciándose. Espera un momento e intenta de nuevo.";
        document.getElementById("facebookError").style.color = "#e74c3c";
      });
    } else {
      document.getElementById("facebookError").textContent = "Inicio de sesión con Facebook cancelado";
      document.getElementById("facebookError").style.color = "#e74c3c";
    }
  }, { scope: 'public_profile' });
}

var googleSignInInitialized = false;
var googleSignInRendered = false;
var googleClientId = "348814839865-v9fmac5qjs6v90uivp977g24t4uqn3o4.apps.googleusercontent.com";

function initializeGoogleSignIn() {
  var errorEl = document.getElementById("facebookError");
  var buttonContainer = document.getElementById("googleSignInButton");
  if (!buttonContainer) return;

  if (typeof google === "undefined" || !google.accounts || !google.accounts.id) {
    errorEl.textContent = "No se pudo cargar el acceso con Google. Revisa tu conexión y recarga la página.";
    errorEl.style.color = "#e74c3c";
    return;
  }

  try {
    if (!googleSignInInitialized) {
      google.accounts.id.initialize({
        client_id: googleClientId,
        callback: handleGoogleResponse,
        auto_select: false,
        cancel_on_tap_outside: true
      });
      googleSignInInitialized = true;
    }

    if (!googleSignInRendered) {
      google.accounts.id.renderButton(buttonContainer, {
        type: "standard",
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "rectangular",
        logo_alignment: "left",
        width: 320,
        locale: "es"
      });
      googleSignInRendered = true;
    }

    errorEl.textContent = "";
  } catch (error) {
    console.error("Error al preparar Google Sign-In:", error);
    errorEl.textContent = "No fue posible preparar el acceso con Google. Recarga la página e intenta de nuevo.";
    errorEl.style.color = "#e74c3c";
  }
}

function handleGoogleSdkError() {
  var errorEl = document.getElementById("facebookError");
  if (!errorEl) return;
  errorEl.textContent = "No se pudo conectar con Google. Revisa tu conexión, bloqueadores del navegador y vuelve a intentar.";
  errorEl.style.color = "#e74c3c";
}

var googleGsiScript = document.getElementById("googleGsiScript");
if (googleGsiScript) {
  googleGsiScript.addEventListener("load", initializeGoogleSignIn);
  googleGsiScript.addEventListener("error", handleGoogleSdkError);
}

function handleGoogleResponse(response) {
  var errorEl = document.getElementById("facebookError");
  if (!response || !response.credential) {
    errorEl.textContent = "Google no devolvió una credencial válida.";
    errorEl.style.color = "#e74c3c";
    return;
  }

  errorEl.textContent = "Validando tu cuenta de Google...";
  errorEl.style.color = "#555";
  fetch("/Pizzería Padua/api/v1/Accounts/google-login", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ credential: response.credential })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      setUser(d.user);
      closeAuth();
    } else {
      errorEl.textContent = d.message || "Error al iniciar sesión con Google";
      errorEl.style.color = "#e74c3c";
    }
  }).catch(function(err) {
    errorEl.textContent = "Error de conexión. El servidor puede estar reiniciándose. Espera un momento e intenta de nuevo.";
    errorEl.style.color = "#e74c3c";
  });
}

function openPhoneLogin() {
  document.getElementById("whatsappModal").classList.add("open");
  document.getElementById("whatsappStep1").style.display = "block";
  document.getElementById("whatsappStep2").style.display = "none";
  document.getElementById("whatsappError").textContent = "";
  document.getElementById("whatsappVerifyError").textContent = "";
  document.getElementById("whatsappPhone").value = "";
  document.getElementById("whatsappCode").value = "";
}

function closePhoneLogin() {
  document.getElementById("whatsappModal").classList.remove("open");
}

function normalizeMexicanPhone(phone) {
  var digits = (phone || "").replace(/\D/g, "");
  if (digits.length === 10) return "+52" + digits;
  if (digits.length === 12 && digits.indexOf("52") === 0) return "+" + digits;
  return "";
}

function sendPhoneCode() {
  var rawPhone = document.getElementById("whatsappPhone").value.trim();
  var phone = normalizeMexicanPhone(rawPhone);
  var errorEl = document.getElementById("whatsappError");
  
  if (!phone) {
    errorEl.textContent = "Ingresa tu número de celular de 10 dígitos";
    errorEl.style.color = "#e74c3c";
    return;
  }

  document.getElementById("whatsappPhone").value = rawPhone.replace(/\D/g, "").slice(-10);

  errorEl.textContent = "Enviando código...";
  errorEl.style.color = "#555";

  fetch("/Pizzería Padua/api/v1/Accounts/send-phone-code", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ phoneNumber: phone })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      document.getElementById("whatsappStep1").style.display = "none";
      document.getElementById("whatsappStep2").style.display = "block";
      errorEl.textContent = "";
    } else {
      errorEl.textContent = d.message || "Error al enviar el código";
      errorEl.style.color = "#e74c3c";
    }
  }).catch(function(err) {
    errorEl.textContent = "Error de conexión. El servidor puede estar reiniciándose.";
    errorEl.style.color = "#e74c3c";
  });
}

function verifyPhoneCode() {
  var phone = normalizeMexicanPhone(document.getElementById("whatsappPhone").value.trim());
  var code = document.getElementById("whatsappCode").value.trim();
  var errorEl = document.getElementById("whatsappVerifyError");

  if (!phone) {
    errorEl.textContent = "Ingresa tu número de celular de 10 dígitos";
    errorEl.style.color = "#e74c3c";
    return;
  }

  if (!code) {
    errorEl.textContent = "Ingresa el código de verificación";
    errorEl.style.color = "#e74c3c";
    return;
  }

  errorEl.textContent = "Verificando código...";
  errorEl.style.color = "#555";

  fetch("/Pizzería Padua/api/v1/Accounts/phone-login", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ phoneNumber: phone, code: code })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      setUser(d.user);
      closePhoneLogin();
      closeAuth();
    } else {
      errorEl.textContent = d.message || "Código inválido o expirado";
      errorEl.style.color = "#e74c3c";
    }
  }).catch(function(err) {
    errorEl.textContent = "Error de conexión. El servidor puede estar reiniciándose.";
    errorEl.style.color = "#e74c3c";
  });
}

function sendCode() {
  var email = document.getElementById("regEmail").value.trim();
  if (!email) { document.getElementById("codeSentMsg").textContent = "Ingresa un correo electrónico"; return; }
  document.getElementById("codeSentMsg").style.color = "#27ae60";
  var btn = document.querySelector("#registerStep1 .auth-submit");
  btn.disabled = true; btn.textContent = "Enviando...";
  fetch("/Pizzería Padua/api/v1/Accounts/send-code", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: email })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      document.getElementById("codeSentMsg").textContent = "Código enviado a " + email + ". Revisa tu bandeja de entrada o SPAM.";
      document.getElementById("registerStep1").style.display = "none";
      document.getElementById("registerForm").style.display = "";
      document.getElementById("regCode").focus();
    } else {
      document.getElementById("codeSentMsg").textContent = d.message || "Error";
      document.getElementById("codeSentMsg").style.color = "#e74c3c";
    }
  }).catch(function() {
    document.getElementById("codeSentMsg").textContent = "Error de conexión";
    document.getElementById("codeSentMsg").style.color = "#e74c3c";
  }).then(function() { btn.disabled = false; btn.textContent = "Enviar código"; });
}

function register(e) {
  e.preventDefault();
  var btn = e.target.querySelector("button[type=submit]");
  var phone = normalizeMexicanPhone(document.getElementById("regPhone").value.trim());
  if (!phone) {
    document.getElementById("regError").textContent = "Ingresa tu número de celular de 10 dígitos";
    return;
  }
  btn.disabled = true; btn.textContent = "Creando...";
  fetch("/Pizzería Padua/api/v1/Accounts/register", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      username: document.getElementById("regUser").value.trim(),
      email: document.getElementById("regEmail").value.trim(),
      phoneNumber: phone,
      password: document.getElementById("regPass").value,
      code: document.getElementById("regCode").value.trim()
    })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) { setUser(d.user); closeAuth(); } else { document.getElementById("regError").textContent = d.message || "Error"; }
  }).catch(function() { document.getElementById("regError").textContent = "Error de conexión"; })
  .then(function() { btn.disabled = false; btn.textContent = "Crear cuenta"; });
}

function logout(notifyServer) {
  var userToLogout = currentUser;
  if (notifyServer !== false && userToLogout && userToLogout.sessionToken) {
    fetch("/Pizzería Padua/api/v1/Accounts/session/logout", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId: userToLogout.id, sessionToken: userToLogout.sessionToken }),
      keepalive: true
    }).catch(function() {});
  }
  setUser(null);
  document.getElementById("orderList").innerHTML = "<p style='text-align:center;color:#999;'>Debes iniciar sesión para pedir</p>";
  document.getElementById("total").textContent = "0";
  order = []; total = 0; saveCart();
}

function openMyAccount() {
  if (!currentUser) return;
  document.getElementById("myAccountModal").classList.add("open");
  document.getElementById("myAccountMsg").textContent = "";
  document.getElementById("myAccountMsg").style.color = "#27ae60";
  fetch("/Pizzería Padua/api/v1/Accounts/profile/" + currentUser.id).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      document.getElementById("myAccountUsername").value = d.user.username;
      document.getElementById("myAccountPhone").value = d.user.phoneNumber || "No registrado";
      document.getElementById("myAccountEmail").value = d.user.email || "No registrado";
      document.getElementById("myAccountCreatedAt").value = d.user.createdAt;
    }
  }).catch(function() {});
}

function closeMyAccount() {
  document.getElementById("myAccountModal").classList.remove("open");
}

function updateUsername() {
  var newUsername = document.getElementById("myAccountUsername").value.trim();
  if (!newUsername) {
    document.getElementById("myAccountMsg").textContent = "Nombre de usuario requerido";
    document.getElementById("myAccountMsg").style.color = "#e74c3c";
    return;
  }
  var btn = document.querySelector("#myAccountInfo .auth-submit");
  btn.disabled = true; btn.textContent = "Actualizando...";
  fetch("/Pizzería Padua/api/v1/Accounts/update-username", {
    method: "PUT", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: currentUser.id, newUsername: newUsername })
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      document.getElementById("myAccountMsg").textContent = "Usuario actualizado";
      document.getElementById("myAccountMsg").style.color = "#27ae60";
      currentUser.username = newUsername;
      localStorage.setItem("mt_user", JSON.stringify(currentUser));
      document.getElementById("userDisplay").textContent = newUsername;
    } else {
      document.getElementById("myAccountMsg").textContent = d.message || "Error";
      document.getElementById("myAccountMsg").style.color = "#e74c3c";
    }
  }).catch(function() {
    document.getElementById("myAccountMsg").textContent = "Error de conexión";
    document.getElementById("myAccountMsg").style.color = "#e74c3c";
  }).then(function() { btn.disabled = false; btn.textContent = "Actualizar usuario"; });
}

function confirmDeleteAccount() {
  if (confirm("¿Estás seguro de que deseas eliminar tu cuenta? Esta acción no se puede deshacer.")) {
    deleteAccount();
  }
}

function deleteAccount() {
  fetch("/Pizzería Padua/api/v1/Accounts/" + currentUser.id, {
    method: "DELETE"
  }).then(function(r) { return r.json(); }).then(function(d) {
    if (d.success) {
      closeMyAccount();
      logout();
      alert("Tu cuenta ha sido eliminada");
    } else {
      alert(d.message || "Error al eliminar la cuenta");
    }
  }).catch(function() {
    alert("Error de conexión");
  });
}

function saveCache(key, data) {
  try { localStorage.setItem("cache_" + key, JSON.stringify(data)); } catch (e) {}
}
function loadCache(key) {
  try { const r = localStorage.getItem("cache_" + key); return r ? JSON.parse(r) : null; } catch (e) { return null; }
}
function saveCart() {
  try { localStorage.setItem("cart_order", JSON.stringify(order)); localStorage.setItem("cart_total", total.toString()); } catch (e) {}
}
function loadCart() {
  try {
    const o = localStorage.getItem("cart_order");
    if (o) { order = JSON.parse(o); total = parseFloat(localStorage.getItem("cart_total") || "0"); renderOrder(); }
  } catch (e) {}
}
function setServerOnline(online) {
  const b = document.getElementById("offlineBanner");
  if (!b) return;
  b.classList.toggle("show", !online);
}

let audioCtx = null;
function ensureAudio() {
  if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
  if (audioCtx.state === "suspended") audioCtx.resume();
}
document.addEventListener("click", ensureAudio, { once: true });
document.addEventListener("touchstart", ensureAudio, { once: true });

if ("Notification" in window && Notification.permission === "default") {
  Notification.requestPermission();
}

function sendBrowserNotification(title, body) {
  if (!("Notification" in window)) return;
  if (Notification.permission === "granted") {
    new Notification(title, { body, icon: "/favicon.ico" });
  } else if (Notification.permission === "default") {
    Notification.requestPermission();
  }
}

function toggleCart() {
  const sidebar = document.getElementById("cartSidebar");
  sidebar.classList.toggle("open");
  
  // Ocultar/mostrar botones flotantes cuando el carrito está abierto
  const floatingButtons = document.querySelector(".floating-buttons-top");
  if (floatingButtons) {
    if (sidebar.classList.contains("open")) {
      floatingButtons.style.display = "none";
    } else {
      floatingButtons.style.display = "flex";
    }
  }
}

document.addEventListener("click", function (e) {
  const sidebar = document.getElementById("cartSidebar");
  const icon = document.querySelector(".cart-icon");
  if (sidebar.classList.contains("open") && !sidebar.contains(e.target) && !icon.contains(e.target)) {
    sidebar.classList.remove("open");
  }
});
function toggleOrderTracker() {
  document.getElementById("orderTrackerSidebar").classList.toggle("open");
  document.getElementById("trackerOverlay").classList.toggle("show");
  if (document.getElementById("orderTrackerSidebar").classList.contains("open")) {
    loadActiveOrders();
  }
}

function openMyOrders() {
  // Cerrar otros paneles
  document.getElementById("orderTrackerSidebar").classList.remove("open");
  document.getElementById("trackerOverlay").classList.remove("show");
  document.getElementById("cartSidebar").classList.remove("open");
  
  // Abrir mis pedidos
  document.getElementById("myOrdersSidebar").classList.add("open");
  document.getElementById("myOrdersOverlay").classList.add("show");
  
  // Cerrar menú de usuario
  document.getElementById("userMenu").classList.remove("show");
  
  loadMyOrders();
}

function closeMyOrders() {
  document.getElementById("myOrdersSidebar").classList.remove("open");
  document.getElementById("myOrdersOverlay").classList.remove("show");
}

async function loadMyOrders() {
  if (!currentUser) return;
  
  const container = document.getElementById("myOrdersList");
  container.innerHTML = "<p style='text-align:center;color:#999;'>Cargando pedidos...</p>";
  
  try {
    const res = await fetch(`/Pizzería Padua/api/v1/Orders/user/${currentUser.id}`, {
      headers: { "X-Session-Token": currentUser.sessionToken }
    });
    if (!res.ok) throw new Error("Error al cargar pedidos");
    
    const orders = await res.json();
    
    if (orders.length === 0) {
      container.innerHTML = "<p style='text-align:center;color:#999;'>No tienes pedidos aún</p>";
      return;
    }
    
    const statusMap = { 0: "Pendiente", 1: "En preparación", 2: "Entregado", 3: "Cancelado", 4: "Pendiente de entrega", 5: "Esperando pago" };
    const statusColors = { 0: "#f39c12", 1: "#3498db", 2: "#27ae60", 3: "#e74c3c", 4: "#8e44ad", 5: "#7f8c8d" };
    
    container.innerHTML = orders.map(o => `
      <div style="background:white;border-radius:12px;padding:12px;margin-bottom:10px;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px;">
          <strong>Pedido #${o.orderId}</strong>
          <span style="background:${statusColors[o.status]};color:white;padding:4px 8px;border-radius:12px;font-size:11px;">${statusMap[o.status]}</span>
        </div>
        <div style="font-size:12px;color:#666;margin-bottom:6px;">
          ${new Date(o.orderDate).toLocaleString("es-MX")}
        </div>
        <div style="font-size:13px;margin-bottom:6px;">
          ${o.orderItems ? o.orderItems.map(oi => `<div>${oi.quantity}x ${oi.product ? escapeHtml(oi.product.name) : "Producto"}${oi.customizations ? `<small class="cart-customizations">${escapeHtml(oi.customizations)}</small>` : ""}</div>`).join("") : ""}
        </div>
        <div style="font-weight:700;color:var(--pink);font-size:14px;">
          Total: $${o.totalAmount.toFixed(2)}
        </div>
      </div>
    `).join("");
  } catch (err) {
    container.innerHTML = "<p style='text-align:center;color:#e74c3c;'>Error al cargar pedidos</p>";
  }
}

async function loadUserActiveOrders() {
  if (!currentUser) return;
  
  try {
    const res = await fetch(`/Pizzería Padua/api/v1/Orders/user/${currentUser.id}`, {
      headers: { "X-Session-Token": currentUser.sessionToken }
    });
    if (!res.ok) return;
    
    const orders = await res.json();
    const activeOrders = orders.filter(o => o.status === 0 || o.status === 1 || o.status === 4);
    
    if (activeOrders.length > 0) {
      // Mostrar notificación de pedido activo
      const statusMap = { 0: "Pendiente", 1: "En preparación", 4: "Pendiente de entrega" };
      const order = activeOrders[0];
      showNotification(`Tienes un pedido activo #${order.orderId} (${statusMap[order.status]})`);
    }
  } catch (err) {
    console.error("Error al cargar pedidos activos:", err);
  }
}

function showNotification(message) {
  const notification = document.createElement('div');
  notification.style.cssText = `
    position: fixed;
    top: 100px;
    right: 20px;
    background: linear-gradient(135deg, #6c5ce7, #a29bfe);
    color: white;
    padding: 16px 24px;
    border-radius: 12px;
    box-shadow: 0 4px 16px rgba(108, 92, 231, 0.4);
    z-index: 10000;
    font-weight: 600;
    animation: slideIn 0.3s ease;
  `;
  notification.textContent = message;
  document.body.appendChild(notification);
  
  setTimeout(() => {
    notification.style.animation = 'slideOut 0.3s ease';
    setTimeout(() => notification.remove(), 300);
  }, 5000);
}

document.addEventListener("click", function (e) {
  const sidebar = document.getElementById("cartSidebar");
  const icon = document.querySelector(".cart-icon");
  if (sidebar.classList.contains("open") && !sidebar.contains(e.target) && !icon.contains(e.target)) {
    sidebar.classList.remove("open");
  }
});

function openCart() {
  document.getElementById("cartSidebar").classList.add("open");
}

let productsById = {};
let pendingCustomizationProduct = null;

function parseCustomizationOptions(value) {
  return String(value || "")
    .split(/\r?\n|,/)
    .map(option => option.trim())
    .filter(Boolean);
}

function openProductCustomization(productId) {
  const product = productsById[productId];
  if (!product) return;
  const options = parseCustomizationOptions(product.customizationOptions);
  if (!options.length) {
    addToOrder(product.productId, product.name, Number(product.price || 0), "");
    return;
  }

  pendingCustomizationProduct = product;
  document.getElementById("customizationTitle").textContent = "Personalizar " + product.name;
  document.getElementById("customizationNotes").value = "";
  document.getElementById("customizationOptions").innerHTML = options.map((option, index) => `
    <label class="customization-option">
      <input type="checkbox" value="${escapeHtml(option)}" id="customOption${index}" />
      <span>${escapeHtml(option)}</span>
    </label>
  `).join("");
  document.getElementById("customizationModal").classList.add("open");
}

function closeCustomizationModal() {
  document.getElementById("customizationModal")?.classList.remove("open");
  pendingCustomizationProduct = null;
}

function confirmCustomization() {
  if (!pendingCustomizationProduct) return;
  const selected = Array.from(document.querySelectorAll("#customizationOptions input:checked")).map(input => input.value);
  const notes = document.getElementById("customizationNotes").value.trim();
  const parts = [];
  if (selected.length) parts.push(selected.join(", "));
  if (notes) parts.push("Nota: " + notes);
  addToOrder(
    pendingCustomizationProduct.productId,
    pendingCustomizationProduct.name,
    Number(pendingCustomizationProduct.price || 0),
    parts.join(" | ")
  );
  closeCustomizationModal();
}

function addToOrder(productId, productName, price, customizations) {
  order.push({ productId, productName, price, customizations: customizations || "" });
  total += price;
  renderOrder();
  saveCart();
  const icon = document.querySelector(".cart-icon");
  if (icon) {
    icon.classList.remove("cart-flash");
    void icon.offsetWidth;
    icon.classList.add("cart-flash");
  }
}

function removeFromOrder(index, e) {
  if (e) e.stopPropagation();
  total -= order[index].price;
  order.splice(index, 1);
  renderOrder();
  saveCart();
}

function clearCart() {
  order = []; total = 0;
  renderOrder(); saveCart();
}

function renderOrder() {
  const orderList = document.getElementById("orderList");
  const totalElement = document.getElementById("total");
  const cartCount = document.getElementById("cartCount");
  const clearBtn = document.getElementById("clearCartBtn");

  cartCount.textContent = order.length;

  if (order.length === 0) {
    orderList.innerHTML = "<p style='text-align:center;color:#999;'>Aun no has agregado productos</p>";
    totalElement.textContent = "0";
    if (clearBtn) clearBtn.style.display = "none";
    return;
  }

  if (clearBtn) clearBtn.style.display = "";

  orderList.innerHTML = "";
  order.forEach((item, index) => {
    const custom = item.customizations ? `<small class="cart-customizations">${escapeHtml(item.customizations)}</small>` : "";
    orderList.innerHTML += `
      <p>
        <span>${index + 1}. ${escapeHtml(item.productName)}${custom}</span>
        <span>$${item.price.toFixed(2)} <a href="#" onclick="removeFromOrder(${index}, event); return false;" style="color:#ff4f93;text-decoration:none;">&times;</a></span>
      </p>
    `;
  });
  totalElement.textContent = total.toFixed(2);
}

var storeOpen = true;
var storeHours = null;
async function checkStoreHours() {
  try {
    var res = await fetch("/Pizzería Padua/api/v1/Settings?_=" + Date.now());
    if (!res.ok) return;
    storeHours = await res.json();
    var parts = (storeHours.address || "").split("|");
    var now = new Date();
    var cur = now.getHours() * 60 + now.getMinutes();
    var open = storeHours.openingTime.split(":").map(Number);
    var close = storeHours.closingTime.split(":").map(Number);
    var openMin = open[0] * 60 + open[1];
    var closeMin = close[0] * 60 + close[1];
    var today = now.getDay();
    var days = (parts[3] || "1,2,3,4,5,6,0").split(",").map(Number);
    storeOpen = days.indexOf(today) !== -1 && cur >= openMin && cur < closeMin;
  } catch (e) {}
}

async function placeOrder() {
  const msgEl = document.getElementById("orderMsg");
  if (!currentUser) { openAuth(); msgEl.style.color = "red"; msgEl.textContent = "Debes iniciar sesión para pedir"; return; }
  await checkStoreHours();
  if (!storeOpen && storeHours) {
    msgEl.style.color = "red";
    var parts = (storeHours.address || "").split("|");
    var days = (parts[3] || "1,2,3,4,5,6,0").split(",").map(Number);
    var dayStr = days.sort(function(a,b){return a-b}).map(function(d){return daysNames[d] || "";}).filter(Boolean).join(", ");
    msgEl.textContent = "El local est\u00e1 cerrado. Horario: " + storeHours.openingTime + " a " + storeHours.closingTime + " | " + dayStr;
    return;
  }
  const name = currentUser.username.replace(/\s/g, "");

  if (order.length === 0) {
    msgEl.style.color = "red";
    msgEl.textContent = "Agrega productos a tu pedido.";
    return;
  }

  msgEl.textContent = "Verificando nombre...";
  msgEl.style.color = "#666";

  try {
    const checkRes = await fetch(`/Pizzería Padua/api/v1/Orders/user/${currentUser.id}`, {
      headers: { "X-Session-Token": currentUser.sessionToken }
    });
    if (checkRes.ok) {
      const activeOrders = await checkRes.json();
      const duplicate = activeOrders.find(o => o.status === 0 || o.status === 1 || o.status === 4);
      if (duplicate) {
        msgEl.style.color = "red";
        msgEl.textContent = "Ya tienes un pedido activo (#" + duplicate.orderId + "). Espera a que sea entregado antes de hacer otro.";
        return;
      }
    }
    if (mercadoPagoConfigured) {
      openMercadoPagoSummary();
      msgEl.textContent = "";
    } else {
      await submitLegacyOrder(name);
    }
  } catch (err) {
    msgEl.style.color = "red";
    msgEl.textContent = "No se pudo preparar el pago. Intenta nuevamente.";
  }
}

async function submitLegacyOrder(name) {
  const orderRes = await fetch("/Pizzería Padua/api/v1/Orders/add", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: currentUser.id, tableId: null, employeeId: null, customerName: name })
  });
  if (!orderRes.ok) throw new Error("Error al crear orden");
  const newOrder = await orderRes.json();

  for (const item of order) {
    const itemRes = await fetch("/Pizzería Padua/api/v1/Orders/add-item", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ orderId: newOrder.orderId, productId: item.productId, quantity: 1, customizations: item.customizations || "" })
    });
    if (!itemRes.ok) throw new Error("Error al agregar producto");
  }

  myLastOrderId = newOrder.orderId;
  localStorage.setItem("myLastOrderId", newOrder.orderId.toString());
  order = [];
  total = 0;
  renderOrder();
  saveCart();
  loadActiveOrders();
  const msgEl = document.getElementById("orderMsg");
  msgEl.style.color = "green";
  msgEl.textContent = `Pedido #${newOrder.orderId} enviado.`;
}

function getMercadoPagoBreakdown() {
  const subtotal = decimalMoney(total);
  const commission = decimalMoney(subtotal * 0.0319);
  const vat = decimalMoney(commission * 0.16);
  const fee = decimalMoney(commission + vat);
  return { subtotal, commission, vat, fee, total: decimalMoney(subtotal + fee) };
}

function decimalMoney(value) {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
}

function openMercadoPagoSummary() {
  const modal = document.getElementById("paymentFeeModal");
  if (!modal) return;
  const amounts = getMercadoPagoBreakdown();
  document.getElementById("paymentSubtotal").textContent = amounts.subtotal.toFixed(2);
  document.getElementById("paymentCommission").textContent = amounts.commission.toFixed(2);
  document.getElementById("paymentVat").textContent = amounts.vat.toFixed(2);
  document.getElementById("paymentIncrease").textContent = amounts.fee.toFixed(2);
  document.getElementById("paymentGrandTotal").textContent = amounts.total.toFixed(2);
  document.getElementById("paymentCheckoutError").textContent = "";
  modal.classList.add("open");
}

function closeMercadoPagoSummary() {
  document.getElementById("paymentFeeModal")?.classList.remove("open");
}

async function startMercadoPagoCheckout() {
  if (!currentUser || order.length === 0) return;
  const button = document.getElementById("confirmMercadoPagoBtn");
  const error = document.getElementById("paymentCheckoutError");
  button.disabled = true;
  button.textContent = "Conectando con Mercado Pago...";
  error.textContent = "";

  const groupedItems = {};
  order.forEach(item => {
    const key = item.productId + "|" + (item.customizations || "");
    if (!groupedItems[key]) {
      groupedItems[key] = { productId: item.productId, quantity: 0, customizations: item.customizations || "" };
    }
    groupedItems[key].quantity += 1;
  });

  try {
    const response = await fetch("/Pizzería Padua/api/v1/Payments/mercadopago/checkout", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: currentUser.id,
        sessionToken: currentUser.sessionToken,
        customerName: currentUser.username.replace(/\s/g, ""),
        items: Object.values(groupedItems)
      })
    });
    const data = await response.json();
    if (!response.ok || !data.success || !data.initPoint) throw new Error(data.message || "No se pudo iniciar el pago");

    localStorage.setItem("pendingPaymentOrderId", data.orderId.toString());
    window.location.assign(data.initPoint);
  } catch (checkoutError) {
    error.textContent = checkoutError.message || "No se pudo conectar con Mercado Pago";
    button.disabled = false;
    button.textContent = "Pagar con Mercado Pago";
  }
}

async function handleMercadoPagoReturn() {
  const params = new URLSearchParams(window.location.search);
  const result = params.get("payment");
  if (!result) return;

  const msgEl = document.getElementById("orderMsg");
  const paymentId = params.get("payment_id") || params.get("collection_id");
  window.history.replaceState({}, document.title, window.location.pathname + window.location.hash);

  if (result === "failure") {
    msgEl.style.color = "#e74c3c";
    msgEl.textContent = "El pago fue rechazado o cancelado. Tu carrito sigue guardado para intentarlo nuevamente.";
    return;
  }

  if (!paymentId) {
    msgEl.style.color = "#8a6d3b";
    msgEl.textContent = "El pago está pendiente de confirmación en Mercado Pago.";
    return;
  }

  try {
    const response = await fetch(`/Pizzería Padua/api/v1/Payments/mercadopago/confirm?paymentId=${encodeURIComponent(paymentId)}`);
    const data = await response.json();
    if (response.ok && data.approved) {
      myLastOrderId = data.orderId;
      localStorage.setItem("myLastOrderId", data.orderId.toString());
      localStorage.removeItem("pendingPaymentOrderId");
      order = [];
      total = 0;
      renderOrder();
      saveCart();
      msgEl.style.color = "green";
      msgEl.textContent = `Pago aprobado. Tu pedido #${data.orderId} ya fue enviado.`;
      loadActiveOrders();
    } else {
      msgEl.style.color = "#8a6d3b";
      msgEl.textContent = "Tu pago está pendiente. El pedido aparecerá en cuanto Mercado Pago lo apruebe.";
    }
  } catch (err) {
    msgEl.style.color = "#8a6d3b";
    msgEl.textContent = "Mercado Pago está confirmando tu pago. Conserva tu comprobante.";
  }
}

async function loadActiveOrders() {
  const container = document.getElementById("activeOrdersList");
  if (!currentUser || !currentUser.sessionToken) {
    container.innerHTML = '<p style="text-align:center;color:#999;">Inicia sesión para ver tus pedidos activos</p>';
    updateActiveOrderBadge(0);
    return;
  }

  const cacheKey = `activeOrders:${currentUser.id}`;
  try {
    const res = await fetch(`/Pizzería Padua/api/v1/Orders/active/${currentUser.id}`, {
      headers: { "X-Session-Token": currentUser.sessionToken }
    });
    if (!res.ok) throw new Error("HTTP " + res.status);
    const orders = await res.json();
    renderActiveOrders(orders);
    updateActiveOrderBadge(orders.length);
    saveCache(cacheKey, orders);
    setServerOnline(true);
  } catch (err) {
    const cached = loadCache(cacheKey);
    if (cached) { renderActiveOrders(cached); updateActiveOrderBadge(cached.length); }
    setServerOnline(false);
  }
}

function renderActiveOrders(orders) {
  const container = document.getElementById("activeOrdersList");
  if (!orders || orders.length === 0) {
    container.innerHTML = '<p style="text-align:center;color:#999;">No hay pedidos activos</p>';
    return;
  }
  const statusMap = { 0: "Pendiente", 1: "En preparación" };
  container.innerHTML = orders.map(o => {
    const isMine = o.orderId === myLastOrderId;
    return `
      <div class="tracker-order ${isMine ? 'tracker-mine' : ''}">
        <div class="tracker-order-header">
          <span class="tracker-order-id">Pedido #${o.orderId}</span>
          <span class="tracker-status ${o.status === 0 ? 'status-pending' : 'status-progress'}">${statusMap[o.status] || "Desconocido"}</span>
        </div>
        <p class="tracker-customer">${o.customerName || "Anónimo"} ${isMine ? '(T\u00fa)' : ''}</p>
        <p class="tracker-total">$${o.totalAmount.toFixed(2)}</p>
      </div>
    `;
  }).join("");
}

async function loadCompletedOrders() {
  try {
    const res = await fetch("/Pizzería Padua/api/v1/Orders/recent-completed");
    if (!res.ok) throw new Error("HTTP " + res.status);
    const orders = await res.json();
    renderCompletedOrders(orders);
    saveCache("completedOrders", orders);
    setServerOnline(true);
  } catch (err) {
    const cached = loadCache("completedOrders");
    if (cached) renderCompletedOrders(cached);
    setServerOnline(false);
  }
}

function renderCompletedOrders(orders) {
  const container = document.getElementById("completedOrdersList");
  if (!orders || orders.length === 0) {
    container.innerHTML = '<p style="text-align:center;color:#999;">Ninguno a&uacute;n</p>';
    return;
  }
  container.innerHTML = orders.map(o => {
    const isMine = o.orderId === myLastOrderId;
    return `
      <div class="tracker-order tracker-done ${isMine ? 'tracker-mine' : ''}">
        <div class="tracker-order-header">
          <span class="tracker-order-id">Pedido #${o.orderId}</span>
          <span class="tracker-status status-completed">Listo</span>
        </div>
        <p class="tracker-customer">${o.customerName || "Anónimo"} ${isMine ? '(T\u00fa)' : ''}</p>
        <p class="tracker-total">$${o.totalAmount.toFixed(2)}</p>
      </div>
    `;
  }).join("");
}

function updateActiveOrderBadge(count) {
  const badge = document.getElementById("activeOrderCount");
  if (badge) badge.textContent = count;
}

function playNotificationSound() {
  try {
    ensureAudio();
    if (!audioCtx || audioCtx.state === "suspended") return;
    const ctx = audioCtx;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.frequency.value = 880;
    osc.type = "sine";
    gain.gain.setValueAtTime(0.3, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.4);
    osc.start(ctx.currentTime);
    osc.stop(ctx.currentTime + 0.4);
    setTimeout(() => {
      const osc2 = ctx.createOscillator();
      const gain2 = ctx.createGain();
      osc2.connect(gain2);
      gain2.connect(ctx.destination);
      osc2.frequency.value = 1320;
      osc2.type = "sine";
      gain2.gain.setValueAtTime(0.3, ctx.currentTime);
      gain2.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.3);
      osc2.start(ctx.currentTime);
      osc2.stop(ctx.currentTime + 0.3);
    }, 150);
  } catch (err) {}
}

function showOrderNotification(customerName, orderId, status) {
  if (document.hidden) {
    sendBrowserNotification(
      status === 1 ? "Pedido en preparación!" : "Pedido listo!",
      customerName + " - Pedido #" + orderId
    );
  }
  const statusMsgs = {
    1: { icon: "\uD83D\uDC68\u200D\uD83C\uDF73", title: "Pedido en preparación!", text: customerName + " - Pedido #" + orderId + " ya se est\u00e1 preparando" },
    2: { icon: "\uD83C\uDF89", title: "Pedido listo!", text: customerName + " - Pedido #" + orderId + " est\u00e1 listo para recoger" }
  };
  const msg = statusMsgs[status] || statusMsgs[2];
  const notif = document.getElementById("orderNotification");
  document.querySelector("#orderNotification .notif-icon").textContent = msg.icon;
  document.querySelector("#orderNotification strong").textContent = msg.title;
  document.getElementById("notifText").textContent = msg.text;
  notif.className = "order-notification";
  if (status === 1) notif.classList.add("preparing");
  else if (status === 2) notif.classList.add("ready");
  notif.style.display = "block";
  notif.classList.add("notif-show");
  playNotificationSound();
  setTimeout(closeNotification, 8000);
}

function closeNotification() {
  const notif = document.getElementById("orderNotification");
  notif.style.display = "none";
  notif.classList.remove("notif-show");
}

async function loadProducts(categoryId) {
  try {
    const url = categoryId
      ? `/Pizzería Padua/api/v1/Products/by-category/${categoryId}`
      : `/Pizzería Padua/api/v1/Products`;
    const response = await fetch(url);
    if (!response.ok) throw new Error("HTTP " + response.status);
    const products = await response.json();
    renderProducts(products);
    saveCache("products", products);
    setServerOnline(true);
  } catch (err) {
    const cached = loadCache("products");
    if (cached) { renderProducts(cached); setServerOnline(false); return; }
    document.getElementById("productGrid").innerHTML = `
      <div style="grid-column:1/-1;text-align:center;padding:60px 20px;color:#766d86;">
        <p style="font-size:48px;margin-bottom:16px;">&#128272;</p>
        <h3 style="margin-bottom:8px;">Servicio no disponible</h3>
        <p style="font-size:14px;">El sistema se encuentra en mantenimiento. Contacta al administrador.</p>
      </div>`;
    setServerOnline(false);
  }
}

function getCategoryIcon(name) {
  var normalized = (name || "").toLowerCase();
  if (normalized.includes("celular")) return "▯";
  if (normalized.includes("reloj")) return "◫";
  if (normalized.includes("audio")) return "◉";
  if (normalized.includes("carga")) return "⚡";
  if (normalized.includes("accesorio")) return "＋";
  return "✦";
}

async function loadCategories() {
  try {
    const response = await fetch("/Pizzería Padua/api/v1/Categories");
    if (!response.ok) throw new Error("HTTP " + response.status);
    const categories = await response.json();
    renderCategoryNav(categories || []);
    saveCache("categories", categories || []);
  } catch (err) {
    const cached = loadCache("categories");
    if (cached) renderCategoryNav(cached);
  }
}

function renderCategoryNav(categories) {
  const nav = document.getElementById("catFloat");
  if (!nav) return;
  if (currentCategoryId && !categories.some(c => String(c.categoryId) === String(currentCategoryId))) {
    currentCategoryId = "";
    const title = document.querySelector("#menu h2");
    if (title) title.textContent = "Todos los productos";
  }
  const activeCategory = currentCategoryId || "";
  nav.innerHTML = `
    <div class="${!activeCategory ? "cat-active" : ""}" data-category=""><span>✦</span><p>Todo</p></div>
    ${categories.map(c => `
      <div class="${String(activeCategory) === String(c.categoryId) ? "cat-active" : ""}" data-category="${c.categoryId}">
        <span>${getCategoryIcon(c.name)}</span><p>${escapeHtml(c.name || "Categoría")}</p>
      </div>
    `).join("")}
  `;
  bindCategoryNav();
}

function bindCategoryNav() {
  document.querySelectorAll(".cat-float div").forEach(el => {
    el.addEventListener("click", () => {
      document.querySelectorAll(".cat-float div").forEach(d => d.classList.remove("cat-active"));
      el.classList.add("cat-active");

      currentCategoryId = el.dataset.category;
      const title = document.querySelector("#menu h2");
      const catName = el.querySelector("p").textContent;
      title.textContent = !currentCategoryId ? "Todos los productos" : catName;
      loadProducts(currentCategoryId || null);

      document.querySelector("#menu").scrollIntoView({ behavior: "smooth", block: "start" });
    });
  });
}

function renderProducts(products) {
  const grid = document.getElementById("productGrid");
  grid.innerHTML = "";
  if (!products || products.length === 0) {
    grid.innerHTML = "<p style='text-align:center;'>No hay productos disponibles</p>";
    return;
  }
  const cacheBust = Date.now();
  productsById = {};
  products.forEach((p, index) => {
    productsById[p.productId] = p;
    const available = p.isAvailable !== false;
    let rawSrc = resolveImageUrl(p.imageUrl);
    if (supportsWebP && rawSrc.endsWith(".png")) rawSrc = rawSrc.slice(0, -4) + ".webp";
    const imgSrc = rawSrc + (rawSrc.includes("?") ? "&" : "?") + "_t=" + cacheBust;
    grid.innerHTML += `
      <div class="product ${available ? "" : "unavailable"}" style="animation-delay:${Math.min(index * 0.04, 0.28)}s">
        ${p.badge ? `<span class="badge">${p.badge}</span>` : ""}
        ${!available ? `<span class="badge badge-unavail">No disponible</span>` : ""}
        <img src="${imgSrc}" alt="${escapeHtml(p.name)}" onerror="this.src='img/hero-tech.jpg'" />
        <h3>${escapeHtml(p.name)}</h3>
        <p class="product-description">${escapeHtml(p.description || "")}</p>
        <div class="product-buy-row">
          <p class="price">$${Number(p.price).toLocaleString("es-MX", { minimumFractionDigits: 0, maximumFractionDigits: 0 })}</p>
          ${available
            ? `<button aria-label="Agregar ${escapeHtml(p.name)} a la bolsa" onclick="openProductCustomization(${p.productId})" ${!licenseValid ? 'style="display:none;"' : ''}>Comprar</button>`
            : `<button class="btn-disabled" disabled>&times;</button>`}
        </div>
    `;
  });
}

let currentCategoryId = null;

var daysNames = {0:"Dom",1:"Lun",2:"Mar",3:"Mi\u00e9",4:"Jue",5:"Vie",6:"S\u00e1b"};
var mapsUrlSaved = "";
function defaultPromotions() {
  return [
    { eyebrow: "Estrena hoy", title: "Hasta 12 MSI", text: "En celulares y relojes seleccionados.", style: "dark", mediaUrl: "", mediaType: "" },
    { eyebrow: "Kit esencial", title: "15% menos", text: "Funda, cargador y cable para empezar bien.", style: "pink", mediaUrl: "", mediaType: "" },
    { eyebrow: "Compra tranquila", title: "1 año", text: "Garantía en productos participantes.", style: "light", mediaUrl: "", mediaType: "" }
  ];
}

function parsePromotionsFromParts(parts) {
  try {
    if (!parts[5]) return defaultPromotions();
    var parsed = JSON.parse(decodeURIComponent(parts[5]));
    return Array.isArray(parsed) && parsed.length ? parsed : defaultPromotions();
  } catch {
    return defaultPromotions();
  }
}

function escapeHtml(value) {
  return String(value || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function defaultPageTheme() {
  return {
    backgroundColor: "#f5f5f7",
    backgroundSoftColor: "#ffffff",
    headerColor: "#ffffff",
    buttonStartColor: "#0071e3",
    buttonEndColor: "#0071e3",
    accentColor: "#0066cc",
    textColor: "#1d1d1f",
    cardColor: "#ffffff",
    iconColor: "#e2e2e5"
  };
}

function parsePageThemeFromParts(parts) {
  try {
    if (!parts[6]) return defaultPageTheme();
    return Object.assign(defaultPageTheme(), JSON.parse(decodeURIComponent(parts[6])));
  } catch {
    return defaultPageTheme();
  }
}

function applyPageTheme(theme) {
  var t = Object.assign(defaultPageTheme(), theme || {});
  var root = document.documentElement;
  root.style.setProperty("--ink", t.textColor);
  root.style.setProperty("--pink", t.accentColor);
  root.style.setProperty("--coral", t.buttonEndColor);
  root.style.setProperty("--page-bg", `radial-gradient(circle at top left, ${t.accentColor}33, transparent 32rem), radial-gradient(circle at 84% 12%, ${t.buttonEndColor}30, transparent 26rem), linear-gradient(180deg, ${t.backgroundColor} 0%, ${t.backgroundSoftColor} 48%, ${t.backgroundColor} 100%)`);
  root.style.setProperty("--page-grid-color", t.accentColor + "18");
  root.style.setProperty("--header-bg", t.headerColor);
  root.style.setProperty("--button-bg", `linear-gradient(135deg, ${t.buttonStartColor}, ${t.buttonEndColor})`);
  root.style.setProperty("--secondary-button-bg", t.cardColor);
  root.style.setProperty("--card-bg", t.cardColor);
  root.style.setProperty("--soft-panel-bg", t.cardColor);
  root.style.setProperty("--icon-bg", t.iconColor);
}

function renderPromotions(promos) {
  var track = document.getElementById("promoTrack");
  if (!track) return;
  var visiblePromos = promos && promos.length ? promos : defaultPromotions();
  var doubled = visiblePromos.concat(visiblePromos);
  var allowedStyles = ["dark", "pink", "light", "sunset", "berry", "ocean", "mint", "purple", "caramel", "choco", "peach", "sky", "fire"];
  track.innerHTML = doubled.map(function(promo, index) {
    var style = allowedStyles.indexOf(promo.style) >= 0 ? promo.style : "dark";
    var mediaLayout = promo.mediaLayout === "floating" ? "floating" : "full";
    var mediaSize = Math.min(150, Math.max(64, parseInt(promo.mediaSize, 10) || 94));
    var media = "";
    if (promo.mediaUrl) {
      var src = resolveImageUrl(promo.mediaUrl);
      media = promo.mediaType === "video"
        ? `<video class="promo-media" src="${src}" muted autoplay loop playsinline></video>`
        : `<img class="promo-media" src="${src}" alt="${escapeHtml(promo.title)}" onerror="this.style.display='none'" />`;
    }
    return `
      <div class="promo ${style} ${promo.mediaUrl ? "with-media media-" + mediaLayout : ""}" style="--promo-media-size:${mediaSize}px;" ${index >= visiblePromos.length ? 'aria-hidden="true"' : ""}>
        ${media}
        <div class="promo-content">
          <h3>${escapeHtml(promo.eyebrow)}</h3>
          <h2>${escapeHtml(promo.title)}</h2>
          <p>${escapeHtml(promo.text)}</p>
        </div>
      </div>
    `;
  }).join("");
}

function loadContactInfo() {
  fetch("/Pizzería Padua/api/v1/Settings?_=" + Date.now()).then(function(r) { return r.json(); }).then(function(s) {
    var parts = (s.address || "").split("|");
    var addr = parts[0] || "";
    var phone = parts[1] || "";
    var days = (parts[3] || "1,2,3,4,5,6").split(",").map(Number);
    mapsUrlSaved = parts[4] || "";
    var daysStr = days.sort(function(a,b){return a-b;}).map(function(d){return daysNames[d] || "";}).filter(Boolean).join(", ");
    document.getElementById("contactAddress").innerHTML = "&#128205; " + addr;
    if (phone) document.getElementById("contactPhone").innerHTML = "&#128222; " + phone;
    document.getElementById("contactHours").innerHTML = "&#9200; " + s.openingTime + " - " + s.closingTime + " | " + daysStr;
    renderPromotions(parsePromotionsFromParts(parts));
    applyPageTheme(parsePageThemeFromParts(parts));
  }).catch(function() {});
}

document.addEventListener("click", function(e) {
  if (e.target && e.target.id === "directionsBtn") {
    e.preventDefault();
    if (mapsUrlSaved) { window.open(mapsUrlSaved, "_blank"); }
    else {
      var addr = (document.getElementById("contactAddress") || {}).textContent || "";
      window.open("https://www.google.com/maps/dir/?api=1&destination=" + encodeURIComponent(addr.replace(/[^\w\s,.#-]/g, "")), "_blank");
    }
  }
});

async function loadReviews() {
  try {
    const res = await fetch("/Pizzería Padua/api/v1/Reviews");
    const reviews = await res.json();
    const list = document.getElementById("reviewsList");
    
    if (!reviews || reviews.length === 0) {
      list.innerHTML = "<p style='text-align:center;color:#766d86;'>Sé el primero en dejar una reseña</p>";
    } else {
      list.innerHTML = reviews.map(r => `
        <div class="review-card">
          <div class="review-header">
            <span class="review-username">${r.username}</span>
            <span class="review-date">${r.createdAt}</span>
          </div>
          <div class="review-stars">${"★".repeat(r.rating)}${"☆".repeat(5 - r.rating)}</div>
          <p class="review-comment">${r.comment}</p>
        </div>
      `).join("");
    }
    
    const avgRes = await fetch("/Pizzería Padua/api/v1/Reviews/average");
    const avg = await avgRes.json();
    document.getElementById("averageRating").textContent = avg.average;
    document.getElementById("averageStars").textContent = "★".repeat(Math.round(avg.average)) + "☆".repeat(5 - Math.round(avg.average));
    document.getElementById("reviewCount").textContent = avg.count + " reseña" + (avg.count !== 1 ? "s" : "");
  } catch (err) {
    console.error("Error al cargar reseñas:", err);
  }
}

function showReviewForm() {
  if (currentUser) {
    document.getElementById("reviewFormContainer").style.display = "block";
    document.getElementById("loginToReview").style.display = "none";
  } else {
    document.getElementById("reviewFormContainer").style.display = "none";
    document.getElementById("loginToReview").style.display = "block";
  }
}

function openReviewsModal() {
  document.getElementById("reviewsModal").classList.add("open");
  loadReviews();
  showReviewForm();
  var menu = document.getElementById("userMenu");
  if (menu) menu.classList.remove("show");
}

function closeReviewsModal() {
  document.getElementById("reviewsModal").classList.remove("open");
}

document.querySelectorAll(".star-rating .star").forEach(star => {
  star.addEventListener("click", function() {
    const rating = parseInt(this.dataset.rating);
    document.getElementById("reviewRating").value = rating;
    document.querySelectorAll(".star-rating .star").forEach(s => {
      s.classList.toggle("active", parseInt(s.dataset.rating) <= rating);
    });
  });
});

async function submitReview(e) {
  e.preventDefault();
  const rating = parseInt(document.getElementById("reviewRating").value);
  const comment = document.getElementById("reviewComment").value.trim();
  const errorEl = document.getElementById("reviewError");
  
  if (rating === 0) {
    errorEl.textContent = "Selecciona una calificación";
    errorEl.style.color = "#e74c3c";
    return;
  }
  
  if (!comment) {
    errorEl.textContent = "Escribe un comentario";
    errorEl.style.color = "#e74c3c";
    return;
  }
  
  try {
    const res = await fetch("/Pizzería Padua/api/v1/Reviews", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: currentUser.id,
        comment: comment,
        rating: rating
      })
    });
    const data = await res.json();
    
    if (data.success) {
      errorEl.textContent = "";
      document.getElementById("reviewComment").value = "";
      document.getElementById("reviewRating").value = "0";
      document.querySelectorAll(".star-rating .star").forEach(s => s.classList.remove("active"));
      loadReviews();
      alert("¡Gracias por tu reseña!");
    } else {
      errorEl.textContent = data.message || "Error al enviar la reseña";
      errorEl.style.color = "#e74c3c";
    }
  } catch (err) {
    errorEl.textContent = "Error de conexión";
    errorEl.style.color = "#e74c3c";
  }
}

document.addEventListener("DOMContentLoaded", () => {
  initializeGoogleSignIn();
  if (currentUser && currentUser.sessionToken) {
    setUser(currentUser);
  } else if (currentUser) {
    localStorage.removeItem("mt_user");
    currentUser = null;
  }
  checkLicense().then(function() { loadProducts(); });
  loadCategories();
  checkMercadoPagoConfiguration();
  loadCart();
  handleMercadoPagoReturn();
  checkStoreHours();
  loadContactInfo();

  bindCategoryNav();

  loadActiveOrders();
  setInterval(() => {
    loadActiveOrders();
  }, 5000);
  setTimeout(loadActiveOrders, 100);

  if (window.EventSource) {
    const evtSource = new EventSource("/Pizzería Padua/api/v1/Events/stream");
    evtSource.addEventListener("product-changed", async () => {
      await loadCategories();
      loadProducts(currentCategoryId || null);
      setServerOnline(true);
    });
    evtSource.addEventListener("settings-changed", () => {
      loadContactInfo();
      setServerOnline(true);
    });
    evtSource.addEventListener("order-updated", (e) => {
      try {
        const data = JSON.parse(e.data);
        const belongsToCurrentUser = currentUser && Number(data.userId) === Number(currentUser.id);
        if (belongsToCurrentUser && data.status === 1) {
          showOrderNotification(data.customerName || "Cliente", data.orderId, 1);
        } else if (belongsToCurrentUser && data.status === 4) {
          showOrderNotification(data.customerName || "Cliente", data.orderId, 2);
        }
        if (window.ReactNativeWebView && window.ReactNativeWebView.postMessage) {
          window.ReactNativeWebView.postMessage(JSON.stringify({
            type: "order-updated",
            store: "Pizzería Padua",
            orderId: data.orderId,
            customerName: data.customerName || "Cliente",
            status: data.status,
            statusLabel: data.status === 1 ? "preparando" : data.status === 4 ? "listo" : data.status === 2 ? "entregado" : "actualizado",
            date: new Date().toISOString()
          }));
        }
      } catch (err) {}
      setTimeout(loadActiveOrders, 800);
      if (currentUser && document.getElementById("myOrdersSidebar")?.classList.contains("open")) {
        setTimeout(loadMyOrders, 800);
      }
      setServerOnline(true);
    });
    evtSource.onerror = () => { setServerOnline(false); };
    evtSource.onopen = () => { setServerOnline(true); loadActiveOrders(); };
  }
});
