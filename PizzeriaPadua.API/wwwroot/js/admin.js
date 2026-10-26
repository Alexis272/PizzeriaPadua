const API = { products: "/Pizzería Padua/api/v1/Products", orders: "/Pizzería Padua/api/v1/Orders", categories: "/Pizzería Padua/api/v1/Categories" };

function resolveImageUrl(url) {
  if (!url) return "img/hero-tech.jpg";
  if (/^(https?:|data:|blob:)/i.test(url)) return url;
  if (url.startsWith("/img/")) return url.substring(1);
  if (url.startsWith("/promo/")) return url.substring(1);
  return url;
}

function getAdminAuthToken() {
  return sessionStorage.getItem("adminAuthToken") || "";
}

let historyFolderHandle = null;
let storedHistoryFolderHandle = null;
let lastHistoryData = null;
let historyFolderReadyPromise = null;
let historySyncInProgress = false;
let historyDownloadFallback = false;
let ordersRefreshInProgress = false;
let usersRefreshInProgress = false;
const realtimeRefreshTimers = {};

const HISTORY_DB_NAME = "pizzeriaPaduaHistory";
const HISTORY_DB_STORE = "settings";
const HISTORY_FOLDER_KEY = "historyFolder";

document.addEventListener("DOMContentLoaded", () => {
  historyFolderReadyPromise = restoreHistoryFolder();
  historyFolderReadyPromise.then(saveHistoryToFile);
  
  document.querySelectorAll(".tab-link").forEach(link => {
    link.addEventListener("click", async e => {
      e.preventDefault();
      const tab = link.dataset.tab;
      if (tab !== "license") {
        const gate = document.getElementById("licenseGate");
        try {
          const res = await fetch("/Pizzería Padua/api/v1/License/status");
          const data = await res.json();
          if (!data.licensed) {
            gate.classList.add("show");
            return;
          }
        } catch {
          gate.classList.add("show");
          return;
        }
      }
      document.getElementById("licenseGate").classList.remove("show");
      document.querySelectorAll(".tab-link").forEach(l => l.classList.remove("active"));
      link.classList.add("active");
      document.querySelectorAll(".admin-section").forEach(s => s.classList.remove("active"));
      document.getElementById(tab).classList.add("active");
      if (tab === "orders") loadOrders();
      if (tab === "history") loadSalesHistory();
      if (tab === "products") setProductSubtab("products");
      if (tab === "license") loadLicenseStatus();
      if (tab === "settings") loadSettings();
      if (tab === "page") loadSettings();
      if (tab === "users") loadUsers();
    });
  });
  checkLicenseOnLoad();
  initCalendar();
  initializeAdminRealtime();
  setInterval(() => {
    refreshActiveAdminSection();
  }, 5000);
  setInterval(saveHistoryToFile, 15000);
  document.querySelectorAll(".filter-quick-btns .filter-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".filter-quick-btns .filter-btn").forEach(b => b.classList.remove("active"));
      btn.classList.add("active");
      selectedDate = null;
      document.getElementById("dateInput").value = "";
      historyPeriod = btn.dataset.period;
      loadSalesHistory();
    });
  });
});

function refreshActiveAdminSection() {
  const active = document.querySelector(".admin-section.active");
  if (active && active.id === "orders") loadOrders();
  if (active && active.id === "users") loadUsers();
}

function setProductSubtab(tab) {
  document.querySelectorAll(".product-subtab").forEach(btn => {
    btn.classList.toggle("active", btn.dataset.productSubtab === tab);
  });

  document.getElementById("productsSubpanel")?.classList.toggle("active", tab === "products");
  document.getElementById("promosSubpanel")?.classList.toggle("active", tab === "promos");

  if (tab === "products") loadAdminProducts();
  if (tab === "promos") loadSettings();
}

function scheduleRealtimeRefresh(section, delay) {
  clearTimeout(realtimeRefreshTimers[section]);
  realtimeRefreshTimers[section] = setTimeout(() => {
    const active = document.querySelector(".admin-section.active");
    if (!active || active.id !== section) return;
    if (section === "orders") loadOrders();
    if (section === "users") loadUsers();
  }, delay || 150);
}

function initializeAdminRealtime() {
  document.addEventListener("visibilitychange", () => {
    if (!document.hidden) refreshActiveAdminSection();
  });
  window.addEventListener("focus", refreshActiveAdminSection);

  if (!window.EventSource) return;
  const eventSource = new EventSource("/Pizzería Padua/api/v1/Events/stream");
  eventSource.addEventListener("connected", refreshActiveAdminSection);
  eventSource.addEventListener("order-updated", () => scheduleRealtimeRefresh("orders", 250));
  eventSource.addEventListener("session-changed", () => scheduleRealtimeRefresh("users", 100));
  eventSource.onopen = refreshActiveAdminSection;
}

function goToLicenseTab() {
  document.getElementById("licenseGate").classList.remove("show");
  document.querySelectorAll(".tab-link").forEach(l => l.classList.remove("active"));
  document.querySelector('[data-tab="license"]').classList.add("active");
  document.querySelectorAll(".admin-section").forEach(s => s.classList.remove("active"));
  document.getElementById("license").classList.add("active");
  loadLicenseStatus();
}

async function checkLicenseOnLoad() {
  const gate = document.getElementById("licenseGate");
  try {
    const res = await fetch("/Pizzería Padua/api/v1/License/status");
    const data = await res.json();
    if (data.licensed) {
      gate.classList.remove("show");
    } else {
      gate.classList.add("show");
    }
  } catch {
    gate.classList.add("show");
  }
}

async function apiFetch(url, options) {
  const res = await fetch(url, options);
  if (!res.ok) throw new Error("HTTP " + res.status);
  return res.json();
}

// --- ORDERS ---
async function loadOrders() {
  if (ordersRefreshInProgress) return;
  ordersRefreshInProgress = true;
  const container = document.getElementById("ordersList");
  if (!container.dataset.loaded) container.innerHTML = "<p>Cargando pedidos...</p>";
  try {
    const orders = await apiFetch(API.orders);
    sessionStorage.setItem("adminOrders", JSON.stringify(orders));
    const filter = document.getElementById("orderFilter").value;
    let active = orders.filter(o => o.status === 0 || o.status === 1 || o.status === 4);
    let filtered = filter === "all" ? active : active.filter(o => o.status == filter);
    renderOrders(filtered);
    container.dataset.loaded = "true";
    await saveHistoryToFile();
  } catch (err) {
    if (!container.dataset.loaded) container.innerHTML = "<p>No hay conexión con el servidor o no hay pedidos.</p>";
  } finally {
    ordersRefreshInProgress = false;
  }
}

function renderOrders(orders) {
  const container = document.getElementById("ordersList");
  if (!orders || orders.length === 0) {
    container.innerHTML = "<p style='text-align:center;color:#999;'>No hay pedidos</p>";
    return;
  }
  const statusMap = { 0: "Pendiente", 1: "En preparación", 2: "Entregado", 3: "Cancelado", 4: "Pendiente de entrega" };
  const classMap = { 0: "status-pending", 1: "status-progress", 2: "status-completed", 3: "status-cancelled", 4: "status-completed" };
  container.innerHTML = orders.map(o => `
    <div class="order-card">
      <div class="order-info">
        <h3>Pedido #${o.orderId} ${o.customerName ? `- ${o.customerName}` : ""}</h3>
        <p>${new Date(o.orderDate).toLocaleString("es-MX")} | ${o.orderItems ? o.orderItems.length : 0} artículo(s)</p>
        <p><strong>Total: $${o.totalAmount.toFixed(2)}</strong></p>
        ${renderOrderPaymentFee(o)}
        ${o.table ? `<p>Entrega: ${o.table.tableNumber}</p>` : ""}
        ${o.orderItems ? o.orderItems.map(oi => renderOrderItemLine(oi)).join("") : ""}
      </div>
      <span class="order-status ${classMap[o.status] || "status-pending"}">${statusMap[o.status] || "Desconocido"}</span>
      <div class="order-actions">
        ${o.status === 0 ? `<button class="btn-progress" onclick="updateOrderStatus(${o.orderId}, 1)">En preparación</button>` : ""}
        ${o.status === 0 || o.status === 1 ? (getOrderPaymentFee(o) > 0
          ? `<button class="btn-complete" onclick="updateOrderStatus(${o.orderId}, 4)">✔ Listo para entregar</button>`
          : `<button class="btn-complete" onclick="openCashModal(${o.orderId})">✔ Completar</button>`) : ""}
        ${o.status === 0 || o.status === 1 ? `<button class="btn-cancel" onclick="updateOrderStatus(${o.orderId}, 3)">✕ Cancelar</button>` : ""}
        ${o.status === 4 ? `<button class="btn-progress" onclick="renotifyOrder(${o.orderId})">🔔 Volver a notificar</button>` : ""}
        ${o.status === 4 ? `<button class="btn-complete" onclick="updateOrderStatus(${o.orderId}, 2)">✔ Marcar como entregado</button>` : ""}
      </div>
    </div>
  `).join("");
}

function renderOrderPaymentFee(order) {
  const fee = getOrderPaymentFee(order);
  const subtotal = Number(order.totalAmount || 0) - fee;
  return fee > 0.009
    ? `<p style="color:#a34d14;font-size:12px;">Subtotal $${subtotal.toFixed(2)} + cargo Mercado Pago $${fee.toFixed(2)}</p>`
    : "";
}

function renderOrderItemLine(oi, className) {
  const name = oi.product ? oi.product.name : "Producto #" + oi.productId;
  const custom = oi.customizations ? `<small class="order-item-customizations">${escapeHtml(oi.customizations)}</small>` : "";
  const cls = className ? ` class="${className}"` : "";
  return `<span${cls}>${oi.quantity}x ${escapeHtml(name)} - $${Number(oi.subtotal || 0).toFixed(2)}${custom}</span><br>`;
}

function getOrderPaymentFee(order) {
  const subtotal = order.orderItems
    ? order.orderItems.reduce((sum, item) => sum + Number(item.subtotal || 0), 0)
    : Number(order.totalAmount || 0);
  return Math.max(0, Number(order.totalAmount || 0) - subtotal);
}

async function updateOrderStatus(orderId, status) {
  try {
    await apiFetch(API.orders + "/update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ orderId, status, tableId: null, employeeId: null })
    });
    loadOrders();
  } catch (err) {
    alert("Error al actualizar pedido: " + err.message);
  }
}

let pendingOrderId = null;
let pendingTotal = 0;

function openCashModal(orderId) {
  pendingOrderId = orderId;
  const orders = JSON.parse(sessionStorage.getItem("adminOrders") || "[]");
  const order = orders.find(o => o.orderId === orderId);
  pendingTotal = order ? order.totalAmount : 0;
  document.getElementById("modalOrderId").textContent = "#" + orderId;
  document.getElementById("modalTotal").textContent = "$" + pendingTotal.toFixed(2);
  document.getElementById("cashInput").value = "";
  document.getElementById("changeDisplay").textContent = "";
  document.getElementById("confirmCashBtn").disabled = true;
  document.getElementById("cashModal").classList.add("show");
  document.getElementById("cashInput").focus();
}

function closeCashModal() {
  document.getElementById("cashModal").classList.remove("show");
  pendingOrderId = null;
  pendingTotal = 0;
}

document.getElementById("cashInput")?.addEventListener("input", function () {
  const cash = parseFloat(this.value);
  const confirmBtn = document.getElementById("confirmCashBtn");
  const changeDisplay = document.getElementById("changeDisplay");
  if (isNaN(cash) || cash < pendingTotal) {
    confirmBtn.disabled = true;
    if (isNaN(cash)) {
      changeDisplay.textContent = "";
    } else {
      changeDisplay.textContent = "Faltan $" + (pendingTotal - cash).toFixed(2);
      changeDisplay.style.color = "#e74c3c";
    }
    return;
  }
  confirmBtn.disabled = false;
  const change = cash - pendingTotal;
  changeDisplay.textContent = "Cambio: $" + change.toFixed(2);
  changeDisplay.style.color = "#155724";
});

function confirmCash() {
  if (pendingOrderId) {
    updateOrderStatus(pendingOrderId, 4);
    closeCashModal();
  }
}

let adminOrderProducts = [];
let adminOrderQuantities = {};
let adminOrderCustomizations = {};

function escapeHtml(value) {
  return String(value || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

async function openAdminOrderModal() {
  adminOrderQuantities = {};
  adminOrderCustomizations = {};
  document.getElementById("adminOrderCustomer").value = "";
  document.getElementById("adminOrderMsg").textContent = "";
  document.getElementById("adminOrderProducts").innerHTML = "<p>Cargando productos...</p>";
  document.getElementById("adminOrderModal").classList.add("show");
  document.getElementById("adminOrderCustomer").focus();

  try {
    const products = await apiFetch(API.products);
    adminOrderProducts = products.filter(p => p.isAvailable);
    renderAdminOrderProducts();
    updateAdminOrderSummary();
  } catch (err) {
    document.getElementById("adminOrderProducts").innerHTML = "<p>No se pudieron cargar los productos.</p>";
  }
}

function closeAdminOrderModal() {
  document.getElementById("adminOrderModal").classList.remove("show");
  adminOrderProducts = [];
  adminOrderQuantities = {};
  adminOrderCustomizations = {};
}

function parseAdminCustomizationOptions(value) {
  return String(value || "")
    .split(/\r?\n|,/)
    .map(option => option.trim())
    .filter(Boolean);
}

function renderAdminOrderProducts() {
  const container = document.getElementById("adminOrderProducts");
  if (!adminOrderProducts.length) {
    container.innerHTML = "<p style='color:#999;'>No hay productos disponibles</p>";
    return;
  }

  const cacheBust = Date.now();
  container.innerHTML = adminOrderProducts.map((product, index) => {
    const quantity = adminOrderQuantities[product.productId] || 0;
    const rawSrc = resolveImageUrl(product.imageUrl);
    const imgSrc = rawSrc + (rawSrc.includes("?") ? "&" : "?") + "_t=" + cacheBust;
    const safeName = escapeHtml(product.name);
    const description = escapeHtml(product.description || "");
    const price = Number(product.price || 0).toFixed(2);
    const customizationOptions = parseAdminCustomizationOptions(product.customizationOptions);
    const customizationState = adminOrderCustomizations[product.productId] || { selected: [], notes: "" };
    const customizationPanel = quantity > 0
      ? `
          <div class="admin-order-customizations">
            ${customizationOptions.length ? `
              <div class="admin-order-customization-title">Personalizar</div>
              <div class="admin-order-customization-options">
                ${customizationOptions.map(option => `
                  <label>
                    <input type="checkbox" value="${escapeHtml(option)}" ${customizationState.selected.includes(option) ? "checked" : ""} onchange="toggleAdminOrderCustomization(${product.productId}, this.value, this.checked)">
                    ${escapeHtml(option)}
                  </label>
                `).join("")}
              </div>
            ` : ""}
            <textarea placeholder="Notas extra para este producto" oninput="setAdminOrderCustomizationNotes(${product.productId}, this.value)">${escapeHtml(customizationState.notes || "")}</textarea>
          </div>
        `
      : "";
    return `
      <div class="admin-order-product ${quantity > 0 ? "selected" : ""}" style="animation-delay:${Math.min(index * 0.03, 0.24)}s">
        ${product.badge ? `<span class="admin-order-badge">${escapeHtml(product.badge)}</span>` : ""}
        <img src="${imgSrc}" alt="${safeName}" onerror="this.src='img/hero-tech.jpg'" />
        <div class="admin-order-product-body">
          <h3>${safeName}</h3>
          ${description ? `<p>${description}</p>` : ""}
          <div class="admin-order-product-footer">
            <span class="admin-order-price">$${price}</span>
            <div class="admin-order-qty">
              <button type="button" class="qty-minus" aria-label="Quitar ${safeName}" onclick="changeAdminOrderQuantity(${product.productId}, -1)">−</button>
              <span>${quantity}</span>
              <button type="button" class="qty-plus" aria-label="Agregar ${safeName}" onclick="changeAdminOrderQuantity(${product.productId}, 1)">+</button>
            </div>
          </div>
          ${customizationPanel}
        </div>
      </div>
    `;
  }).join("");
}

function toggleAdminOrderCustomization(productId, option, checked) {
  const state = adminOrderCustomizations[productId] || { selected: [], notes: "" };
  const selected = new Set(state.selected || []);
  if (checked) selected.add(option);
  else selected.delete(option);
  adminOrderCustomizations[productId] = { ...state, selected: Array.from(selected) };
  updateAdminOrderSummary();
}

function setAdminOrderCustomizationNotes(productId, notes) {
  const state = adminOrderCustomizations[productId] || { selected: [], notes: "" };
  adminOrderCustomizations[productId] = { ...state, notes: notes || "" };
  updateAdminOrderSummary();
}

function getAdminOrderCustomizations(productId) {
  const state = adminOrderCustomizations[productId] || { selected: [], notes: "" };
  const parts = [...(state.selected || [])];
  const notes = String(state.notes || "").trim();
  if (notes) parts.push("Notas: " + notes);
  return parts.join(", ");
}

function changeAdminOrderQuantity(productId, delta) {
  const current = adminOrderQuantities[productId] || 0;
  const next = Math.max(0, current + delta);
  if (next === 0) {
    delete adminOrderQuantities[productId];
  } else {
    adminOrderQuantities[productId] = next;
  }
  renderAdminOrderProducts();
  updateAdminOrderSummary();
}

function updateAdminOrderSummary() {
  const selected = Object.entries(adminOrderQuantities)
    .map(([productId, quantity]) => {
      const product = adminOrderProducts.find(p => p.productId === Number(productId));
      return product ? { product, quantity } : null;
    })
    .filter(Boolean);

  const total = selected.reduce((sum, item) => sum + Number(item.product.price || 0) * item.quantity, 0);
  document.getElementById("adminOrderTotal").textContent = "$" + total.toFixed(2);
  document.getElementById("adminOrderSummary").innerHTML = selected.length
    ? selected.map(item => {
        const custom = getAdminOrderCustomizations(item.product.productId);
        return `${item.quantity}x ${escapeHtml(item.product.name)}${custom ? `<small class="order-item-customizations">${escapeHtml(custom)}</small>` : ""}`;
      }).join("<br>")
    : "Sin productos";
}

async function createAdminOrder() {
  const customerName = document.getElementById("adminOrderCustomer").value.trim();
  const msg = document.getElementById("adminOrderMsg");
  const button = document.getElementById("adminOrderCreateBtn");
  const items = Object.entries(adminOrderQuantities).map(([productId, quantity]) => ({
    productId: Number(productId),
    quantity,
    customizations: getAdminOrderCustomizations(Number(productId))
  }));

  if (!customerName) {
    msg.style.color = "#e74c3c";
    msg.textContent = "Ingresa el nombre del cliente.";
    return;
  }

  if (!items.length) {
    msg.style.color = "#e74c3c";
    msg.textContent = "Agrega al menos un producto.";
    return;
  }

  button.disabled = true;
  button.textContent = "Creando...";
  msg.style.color = "#555";
  msg.textContent = "Creando pedido...";

  try {
    const response = await fetch(API.orders + "/admin/add", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ customerName, items })
    });
    const data = await response.json();
    if (!response.ok || !data.success) {
      throw new Error(data.message || "No se pudo crear el pedido");
    }

    msg.style.color = "green";
    msg.textContent = "Pedido creado correctamente.";
    closeAdminOrderModal();
    await loadOrders();
  } catch (err) {
    msg.style.color = "#e74c3c";
    msg.textContent = err.message || "No se pudo crear el pedido";
  } finally {
    button.disabled = false;
    button.textContent = "Crear pedido";
  }
}

let historyPeriod = "all";
let selectedDate = null;

function initCalendar() {
  flatpickr("#dateInput", {
    locale: flatpickr.l10ns.es || "es",
    dateFormat: "Y-m-d",
    inline: true,
    onChange: function(selectedDates, dateStr) {
      selectedDate = selectedDates[0] || null;
      document.querySelectorAll(".filter-quick-btns .filter-btn").forEach(b => b.classList.remove("active"));
      historyPeriod = "";
      loadSalesHistory();
    }
  });
}

async function loadSalesHistory() {
  const container = document.getElementById("historyList");
  const totalEl = document.getElementById("totalSales");
  const countEl = document.getElementById("completedCount");
  container.innerHTML = "<p>Cargando historial...</p>";
  try {
    const orders = await apiFetch(API.orders);
    const completed = orders.filter(o => o.status === 2);
    const filtered = filterHistoryByPeriod(completed);
    renderHistory(filtered, totalEl, countEl, container);
    await saveHistoryToFile();
  } catch (err) {
    container.innerHTML = "<p>No hay conexión con el servidor.</p>";
  }
}

function filterHistoryByPeriod(orders) {
  const now = new Date();
  if (selectedDate) {
    return orders.filter(o => {
      const d = new Date(o.orderDate);
      return d.getFullYear() === selectedDate.getFullYear() &&
             d.getMonth() === selectedDate.getMonth() &&
             d.getDate() === selectedDate.getDate();
    });
  }
  switch (historyPeriod) {
    case "today":
      return orders.filter(o => {
        const d = new Date(o.orderDate);
        return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate();
      });
    case "month":
      return orders.filter(o => {
        const d = new Date(o.orderDate);
        return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
      });
    case "year":
      return orders.filter(o => new Date(o.orderDate).getFullYear() === now.getFullYear());
    default:
      return orders;
  }
}



function renderHistory(orders, totalEl, countEl, container) {
  if (!orders || orders.length === 0) {
    container.innerHTML = "<p style='text-align:center;color:#999;'>No hay ventas registradas</p>";
    totalEl.textContent = "$0.00";
    countEl.textContent = "0";
    return;
  }
  const total = orders.reduce((sum, o) => sum + o.totalAmount, 0);
  totalEl.textContent = "$" + total.toFixed(2);
  countEl.textContent = orders.length;
  container.innerHTML = orders.map(o => `
    <div class="order-card history-card">
      <div class="order-info">
        <h3>Pedido #${o.orderId} ${o.customerName ? `- ${o.customerName}` : ""}</h3>
        <p>${new Date(o.orderDate).toLocaleString("es-MX")}</p>
        <p><strong>Total: $${o.totalAmount.toFixed(2)}</strong></p>
        ${renderOrderPaymentFee(o)}
        ${o.orderItems ? o.orderItems.map(oi => renderOrderItemLine(oi, "items")).join("") : ""}
      </div>
      <span class="order-status status-completed">Entregado</span>
    </div>
  `).join("");
}

document.getElementById("orderFilter")?.addEventListener("change", loadOrders);

// --- PRODUCTS ---
let adminCategories = [];

async function loadAdminProducts() {
  const grid = document.getElementById("adminProductGrid");
  grid.innerHTML = "<p>Cargando productos...</p>";
  try {
    const [products, categories] = await Promise.all([
      apiFetch(API.products),
      apiFetch(API.categories)
    ]);
    adminCategories = categories || [];
    renderAdminCategories();
    populateCategorySelects();
    renderAdminProducts(products);
  } catch (err) {
    grid.innerHTML = "<p>No se pudieron cargar los productos.</p>";
  }
}

function populateCategorySelects() {
  const options = `<option value="">Sin categoría</option>` + adminCategories
    .map(c => `<option value="${c.categoryId}">${escapeHtml(c.name || "")}</option>`)
    .join("");
  const prodCategory = document.getElementById("prodCategory");
  const editCategory = document.getElementById("editCategory");
  if (prodCategory) prodCategory.innerHTML = options;
  if (editCategory) editCategory.innerHTML = options;
}

function renderAdminCategories() {
  const container = document.getElementById("categoryAdminList");
  if (!container) return;
  if (!adminCategories.length) {
    container.innerHTML = `<p class="category-empty">No hay categorías todavía.</p>`;
    return;
  }
  container.innerHTML = adminCategories.map(c => {
    const productCount = Array.isArray(c.products) ? c.products.length : 0;
    return `
      <div class="category-admin-item">
        <div>
          <strong>${escapeHtml(c.name || "Sin nombre")}</strong>
          <span>${productCount} producto(s)</span>
          ${c.description ? `<p>${escapeHtml(c.description)}</p>` : ""}
        </div>
        <div class="category-admin-actions">
          <button type="button" class="btn-edit" onclick="editCategory(${c.categoryId})">Editar</button>
          <button type="button" class="btn-delete" onclick="deleteCategory(${c.categoryId})">Eliminar</button>
        </div>
      </div>
    `;
  }).join("");
}

async function addCategory(event) {
  event.preventDefault();
  const name = document.getElementById("catName").value.trim();
  const description = document.getElementById("catDesc").value.trim();
  if (!name) return;
  try {
    await apiFetch(API.categories + "/add", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, description })
    });
    document.getElementById("categoryForm").reset();
    await loadAdminProducts();
  } catch (err) {
    alert("Error al agregar categoría: " + err.message);
  }
}

async function editCategory(categoryId) {
  const category = adminCategories.find(c => c.categoryId === categoryId);
  if (!category) return;
  const name = prompt("Nuevo nombre de la categoría:", category.name || "");
  if (name === null) return;
  const cleanName = name.trim();
  if (!cleanName) {
    alert("El nombre no puede quedar vacío.");
    return;
  }
  const description = prompt("Descripción de la categoría:", category.description || "");
  if (description === null) return;
  try {
    await apiFetch(API.categories + "/update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ categoryId, name: cleanName, description: description.trim() })
    });
    await loadAdminProducts();
  } catch (err) {
    alert("Error al editar categoría: " + err.message);
  }
}

async function deleteCategory(categoryId) {
  const category = adminCategories.find(c => c.categoryId === categoryId);
  const productCount = category && Array.isArray(category.products) ? category.products.length : 0;
  const message = productCount > 0
    ? `¿Eliminar "${category.name}"? Sus ${productCount} producto(s) NO se eliminarán; quedarán como "Sin categoría".`
    : `¿Eliminar "${category ? category.name : "esta categoría"}"?`;
  if (!confirm(message)) return;
  try {
    await apiFetch(API.categories + "/delete/" + categoryId, { method: "DELETE" });
    await loadAdminProducts();
  } catch (err) {
    alert("Error al eliminar categoría: " + err.message);
  }
}

function renderAdminProducts(products) {
  const grid = document.getElementById("adminProductGrid");
  if (!products || products.length === 0) {
    grid.innerHTML = "<p style='text-align:center;color:#999;'>No hay productos</p>";
    return;
  }
  grid.style.display = "grid";
  grid.style.gridTemplateColumns = "repeat(auto-fill, minmax(260px, 1fr))";
  grid.style.gap = "15px";
  grid.innerHTML = products.map(p => {
    const imgSrc = resolveImageUrl(p.imageUrl);
    return `
    <div class="admin-product-card">
      <img src="${imgSrc}" alt="${p.name}" style="width:100%;height:140px;object-fit:cover;border-radius:14px;background:#f5f0ff;"
        onerror="this.src='img/hero-tech.jpg'" />
      <div class="prod-header">
        <h3>${p.name}</h3>
        <span class="price">$${p.price.toFixed(2)}</span>
      </div>
      <div>
        <span class="category-tag">${p.category ? escapeHtml(p.category.name) : "Sin categoría"}</span>
        <span class="available-badge ${p.isAvailable ? "avail-yes" : "avail-no"}">${p.isAvailable ? "Disponible" : "Agotado"}</span>
        ${p.badge ? `<span class="badge-tag">${p.badge}</span>` : ""}
      </div>
      <div class="prod-actions">
        <button class="btn-edit" onclick="openEditModal(${p.productId})">✏️ Editar</button>
        <button class="${p.isAvailable ? "btn-toggle-off" : "btn-toggle-on"}" onclick="toggleAvailability(${p.productId}, ${!p.isAvailable})">
          ${p.isAvailable ? "Desactivar" : "Activar"}
        </button>
        <button class="btn-delete" onclick="deleteProduct(${p.productId})">Eliminar</button>
      </div>
    </div>`;
  }).join("");
}

async function toggleAvailability(productId, newState) {
  try {
    const products = await apiFetch(API.products);
    const product = products.find(p => p.productId === productId);
    if (!product) return;
    await apiFetch(API.products + "/update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        productId, name: product.name, description: product.description || "",
        price: product.price, categoryId: product.categoryId || null,
        stock: product.stock, isAvailable: newState,
        imageUrl: product.imageUrl, isFeatured: product.isFeatured, badge: product.badge,
        customizationOptions: product.customizationOptions || ""
      })
    });
    loadAdminProducts();
  } catch (err) {
    alert("Error al actualizar producto: " + err.message);
  }
}

let pendingDeleteId = null;

function deleteProduct(productId) {
  pendingDeleteId = productId;
  document.getElementById("authUser").value = "";
  document.getElementById("authPass").value = "";
  document.getElementById("authError").style.display = "none";
  document.getElementById("authModal").classList.add("show");
  document.getElementById("authUser").focus();
}

function closeAuthModal() {
  document.getElementById("authModal").classList.remove("show");
  pendingDeleteId = null;
}

async function confirmDeleteProduct() {
  const user = document.getElementById("authUser").value.replace(/\s/g, "");
  const pass = document.getElementById("authPass").value.replace(/\s/g, "");
  const authResponse = await fetch("/Pizzería Padua/api/v1/License/auth", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: user, password: pass })
  });
  if (!authResponse.ok) {
    document.getElementById("authError").style.display = "block";
    return;
  }
  sessionStorage.setItem("adminAuthToken", btoa(user + ":" + pass));
  if (!pendingDeleteId) return;
  document.getElementById("authError").style.display = "none";
  closeAuthModal();
  try {
    await apiFetch(API.products + "/delete/" + pendingDeleteId, { method: "DELETE" });
    loadAdminProducts();
  } catch (err) {
    alert("Error al eliminar: " + err.message);
  }
  pendingDeleteId = null;
}

// --- LICENSE ---
let licenseAuthToken = null;

function handleLicenseFile(event) {
  const file = event.target.files[0];
  if (!file) return;
  document.getElementById("fileNameDisplay").textContent = file.name;
  const reader = new FileReader();
  reader.onload = function (e) {
    document.getElementById("licenseContent").value = e.target.result;
    uploadLicense();
  };
  reader.readAsText(file);
  event.target.value = "";
}

async function loadLicenseStatus() {
  const container = document.getElementById("licenseStatus");
  const revokeBtn = document.getElementById("btnRevokeLicense");
  if (!container) return;
  try {
    const res = await fetch("/Pizzería Padua/api/v1/License/status");
    const data = await res.json();
    if (data.licensed) {
      container.innerHTML = `
        <p style="color:#15724b;font-weight:900;font-size:16px;">✅ License active</p>
        <p style="color:#766d86;font-size:13px;">Expires: ${data.expires || "N/A"}</p>
      `;
      if (revokeBtn) revokeBtn.style.display = "";
    } else if (data.expired) {
      container.innerHTML = `
        <p style="color:#a62744;font-weight:900;font-size:16px;">❌ License expired</p>
        <p style="color:#766d86;font-size:13px;">Expired: ${data.expires || "N/A"}</p>
      `;
      if (revokeBtn) revokeBtn.style.display = "none";
    } else {
      container.innerHTML = `
        <p style="color:#8b6500;font-weight:900;font-size:16px;">⚠️ No license installed</p>
        <p style="color:#766d86;font-size:13px;">Upload a license file to activate the system.</p>
      `;
      if (revokeBtn) revokeBtn.style.display = "none";
    }
  } catch (err) {
    container.innerHTML = `<p style="color:#a62744;">Error loading license status</p>`;
  }
}

function openRevokeConfirm() {
  document.getElementById("revokeUser").value = "";
  document.getElementById("revokePass").value = "";
  document.getElementById("revokeError").style.display = "none";
  document.getElementById("revokeConfirmModal").classList.add("show");
  document.getElementById("revokeUser").focus();
}

function closeRevokeConfirm() {
  document.getElementById("revokeConfirmModal").classList.remove("show");
}

async function confirmRevoke() {
  const user = document.getElementById("revokeUser").value.trim();
  const pass = document.getElementById("revokePass").value.trim();
  const token = btoa(user + ":" + pass);
  try {
    const res = await fetch("/Pizzería Padua/api/v1/License/revoke", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-License-Auth": token
      }
    });
    const data = await res.json();
    if (data.success) {
      closeRevokeConfirm();
      document.getElementById("licenseGate").classList.add("show");
      document.querySelector("[data-tab='license']").click();
    } else {
      document.getElementById("revokeError").style.display = "block";
    }
  } catch {
    document.getElementById("revokeError").textContent = "Connection error";
    document.getElementById("revokeError").style.display = "block";
  }
}

function openLicenseAuth() {
  document.getElementById("licenseUser").value = "";
  document.getElementById("licensePass").value = "";
  document.getElementById("licenseAuthError").style.display = "none";
  document.getElementById("licenseAuthModal").classList.add("show");
  document.getElementById("licenseUser").focus();
}

function closeLicenseAuthModal() {
  document.getElementById("licenseAuthModal").classList.remove("show");
}

function closeLicenseUploadModal() {
  document.getElementById("licenseUploadModal").classList.remove("show");
}

async function confirmLicenseAuth() {
  const user = document.getElementById("licenseUser").value.trim();
  const pass = document.getElementById("licensePass").value.trim();
  try {
    const res = await fetch("/Pizzería Padua/api/v1/License/auth", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: user, password: pass })
    });
    const data = await res.json();
    if (data.success) {
      licenseAuthToken = btoa(user + ":" + pass);
      closeLicenseAuthModal();
      openLicenseUpload();
    } else {
      document.getElementById("licenseAuthError").style.display = "block";
    }
  } catch (err) {
    document.getElementById("licenseAuthError").textContent = "Connection error";
    document.getElementById("licenseAuthError").style.display = "block";
  }
}

function openLicenseUpload() {
  document.getElementById("licenseContent").value = "";
  document.getElementById("licenseUploadResult").textContent = "";
  document.getElementById("licenseUploadResult").style.color = "";
  document.getElementById("licenseUploadModal").classList.add("show");
}

async function uploadLicense() {
  const content = document.getElementById("licenseContent").value.trim();
  if (!content) {
    document.getElementById("licenseUploadResult").textContent = "Paste the license JSON content";
    document.getElementById("licenseUploadResult").style.color = "#a62744";
    return;
  }
  try {
    const res = await fetch("/Pizzería Padua/api/v1/License/upload", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-License-Auth": licenseAuthToken
      },
      body: JSON.stringify({ licenseContent: content })
    });
    const data = await res.json();
    if (data.success) {
      document.getElementById("licenseUploadResult").textContent = "✅ " + data.message + " (expires: " + data.expires + ")";
      document.getElementById("licenseUploadResult").style.color = "#15724b";
      setTimeout(() => {
        closeLicenseUploadModal();
        document.getElementById("licenseGate").classList.remove("show");
        document.querySelector("[data-tab='orders']").click();
      }, 1500);
      loadLicenseStatus();
    } else {
      document.getElementById("licenseUploadResult").textContent = "❌ " + data.message;
      document.getElementById("licenseUploadResult").style.color = "#a62744";
    }
  } catch (err) {
    document.getElementById("licenseUploadResult").textContent = "❌ Connection error";
    document.getElementById("licenseUploadResult").style.color = "#a62744";
  }
}

async function addProduct(e) {
  e.preventDefault();
  const name = document.getElementById("prodName").value.trim();
  const price = parseFloat(document.getElementById("prodPrice").value);
  const desc = document.getElementById("prodDesc").value.trim();
  const image = document.getElementById("prodImage").value.trim();
  const categoryValue = document.getElementById("prodCategory").value;
  const categoryId = categoryValue ? parseInt(categoryValue) : null;
  const badge = document.getElementById("prodBadge").value.trim();
  const customizationOptions = document.getElementById("prodCustomizations").value.trim();
  const featured = document.getElementById("prodFeatured").checked;

  if (!name || isNaN(price)) {
    alert("Completa los campos obligatorios.");
    return;
  }

  try {
    await apiFetch(API.products + "/add", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name, description: desc, price, categoryId,
        stock: 50, isAvailable: true,
        imageUrl: image || null, isFeatured: featured, badge: badge || null,
        customizationOptions: customizationOptions || null
      })
    });
    document.getElementById("productForm").reset();
    loadAdminProducts();
    alert("Producto agregado.");
  } catch (err) {
    alert("Error al agregar: " + err.message);
  }
}

// --- EDIT PRODUCT ---
let pendingEditId = null;
let pendingEditImageUrl = null;

function openEditModal(productId) {
  pendingEditId = productId;
  pendingEditImageUrl = null;
  document.getElementById("editError").style.display = "none";
  document.getElementById("editForm").reset();
  document.getElementById("editImageFile").value = "";
  document.getElementById("editImageName").textContent = "";

  apiFetch(API.products).then(products => {
    const p = products.find(x => x.productId === productId);
    if (!p) return;
    pendingEditImageUrl = p.imageUrl || null;
    document.getElementById("editProductTitle").textContent = `Editando: ${p.name}`;
    document.getElementById("editName").value = p.name || "";
    document.getElementById("editPrice").value = p.price || "";
    populateCategorySelects();
    document.getElementById("editCategory").value = p.categoryId || "";
    document.getElementById("editDesc").value = p.description || "";
    document.getElementById("editBadge").value = p.badge || "";
    document.getElementById("editCustomizations").value = p.customizationOptions || "";
    document.getElementById("editFeatured").checked = !!p.isFeatured;
    document.getElementById("editAvailable").checked = p.isAvailable !== false;
    document.getElementById("editStock").value = p.stock ?? 50;
    const preview = document.getElementById("editImagePreview");
    if (p.imageUrl) {
      preview.src = p.imageUrl;
      preview.style.display = "block";
    } else {
      preview.style.display = "none";
    }
    document.getElementById("editModal").classList.add("show");
  }).catch(() => {
    document.getElementById("editError").textContent = "Error al cargar producto";
    document.getElementById("editError").style.display = "block";
  });
}

function closeEditModal() {
  document.getElementById("editModal").classList.remove("show");
  pendingEditId = null;
  pendingEditImageUrl = null;
}

async function saveEditProduct(e) {
  e.preventDefault();
  if (!pendingEditId) return;

  const name = document.getElementById("editName").value.trim();
  const price = parseFloat(document.getElementById("editPrice").value);
  const categoryValue = document.getElementById("editCategory").value;
  const categoryId = categoryValue ? parseInt(categoryValue) : null;
  const desc = document.getElementById("editDesc").value.trim();
  const badge = document.getElementById("editBadge").value.trim();
  const customizationOptions = document.getElementById("editCustomizations").value.trim();
  const featured = document.getElementById("editFeatured").checked;
  const available = document.getElementById("editAvailable").checked;
  const stock = parseInt(document.getElementById("editStock").value) || 0;

  if (!name || isNaN(price)) {
    document.getElementById("editError").textContent = "Completa los campos obligatorios.";
    document.getElementById("editError").style.display = "block";
    return;
  }

  try {
    let imageUrl = pendingEditImageUrl;
    const fileInput = document.getElementById("editImageFile");
    if (fileInput.files.length > 0) {
      const formData = new FormData();
      formData.append("productId", pendingEditId);
      formData.append("file", fileInput.files[0]);
      const uploadRes = await fetch("/Pizzería Padua/api/v1/Products/upload-image", { method: "POST", body: formData });
      const uploadData = await uploadRes.json();
      if (uploadData.success) {
        imageUrl = uploadData.imageUrl;
      } else {
        document.getElementById("editError").textContent = "Error al subir imagen: " + (uploadData.message || "desconocido");
        document.getElementById("editError").style.display = "block";
        return;
      }
    }

    await apiFetch(API.products + "/update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        productId: pendingEditId,
        name, description: desc, price, categoryId,
        stock, isAvailable: available,
        imageUrl, isFeatured: featured, badge: badge || null,
        customizationOptions: customizationOptions || null
      })
    });
    closeEditModal();
    loadAdminProducts();
  } catch (err) {
    document.getElementById("editError").textContent = "Error al guardar: " + err.message;
    document.getElementById("editError").style.display = "block";
  }
}

document.getElementById("editImageFile")?.addEventListener("change", function () {
  const file = this.files[0];
  if (!file) return;
  document.getElementById("editImageName").textContent = file.name;
  const reader = new FileReader();
  reader.onload = function (e) {
    const preview = document.getElementById("editImagePreview");
    preview.src = e.target.result;
    preview.style.display = "block";
  };
  reader.readAsDataURL(file);
});

// --- IMAGE UPLOAD ---
let imageUploadProductId = null;

function openImageUpload(productId) {
  imageUploadProductId = productId;
  document.getElementById("imageFileInput").value = "";
  document.getElementById("imageFileName").textContent = "";
  document.getElementById("imagePreview").style.display = "none";
  document.getElementById("imagePreview").src = "";
  document.getElementById("imageUploadResult").textContent = "";
  document.getElementById("btnUploadImage").disabled = false;

  apiFetch(API.products).then(products => {
    const p = products.find(x => x.productId === productId);
    if (p) {
      document.getElementById("imageUploadProductName").textContent = p.name;
      if (p.imageUrl) {
        document.getElementById("imagePreview").src = p.imageUrl;
        document.getElementById("imagePreview").style.display = "block";
      }
    }
  }).catch(() => {});

  document.getElementById("imageUploadModal").classList.add("show");
}

function closeImageUpload() {
  document.getElementById("imageUploadModal").classList.remove("show");
  imageUploadProductId = null;
}

document.getElementById("imageFileInput")?.addEventListener("change", function () {
  const file = this.files[0];
  if (!file) return;
  document.getElementById("imageFileName").textContent = file.name;
  const reader = new FileReader();
  reader.onload = function (e) {
    document.getElementById("imagePreview").src = e.target.result;
    document.getElementById("imagePreview").style.display = "block";
  };
  reader.readAsDataURL(file);
});

async function uploadImage() {
  const file = document.getElementById("imageFileInput").files[0];
  if (!file) {
    document.getElementById("imageUploadResult").textContent = "Selecciona una imagen";
    document.getElementById("imageUploadResult").style.color = "#a62744";
    return;
  }
  if (!imageUploadProductId) return;

  document.getElementById("btnUploadImage").disabled = true;
  document.getElementById("imageUploadResult").textContent = "Subiendo...";
  document.getElementById("imageUploadResult").style.color = "#766d86";

  const formData = new FormData();
  formData.append("productId", imageUploadProductId);
  formData.append("file", file);

  try {
    const res = await fetch("/Pizzería Padua/api/v1/Products/upload-image", {
      method: "POST",
      body: formData
    });
    const data = await res.json();
    if (data.success) {
      document.getElementById("imageUploadResult").textContent = "✅ Imagen actualizada";
      document.getElementById("imageUploadResult").style.color = "#15724b";
      setTimeout(() => {
        closeImageUpload();
        loadAdminProducts();
      }, 1200);
    } else {
      document.getElementById("imageUploadResult").textContent = "❌ " + (data.message || "Error");
      document.getElementById("imageUploadResult").style.color = "#a62744";
      document.getElementById("btnUploadImage").disabled = false;
    }
  } catch (err) {
    document.getElementById("imageUploadResult").textContent = "❌ Error de conexión";
    document.getElementById("imageUploadResult").style.color = "#a62744";
    document.getElementById("btnUploadImage").disabled = false;
  }
}

var daysInitialized = false;
var promoEditorsData = [];
var pageThemeData = defaultPageTheme();
var promoStyleOptions = [
  { value: "dark", label: "Oscura" },
  { value: "pink", label: "Rosa" },
  { value: "light", label: "Clara" },
  { value: "sunset", label: "Atardecer" },
  { value: "berry", label: "Frutos rojos" },
  { value: "ocean", label: "Océano" },
  { value: "mint", label: "Menta" },
  { value: "purple", label: "Morado" },
  { value: "caramel", label: "Caramelo" },
  { value: "choco", label: "Chocolate" },
  { value: "peach", label: "Durazno" },
  { value: "sky", label: "Cielo" },
  { value: "fire", label: "Fuego" }
];

function defaultPromotions() {
  return [
    { eyebrow: "Estrena hoy", title: "Hasta 12 MSI", text: "En celulares y relojes seleccionados.", style: "dark", mediaUrl: "", mediaType: "" },
    { eyebrow: "Kit esencial", title: "15% menos", text: "Funda, cargador y cable para empezar bien.", style: "pink", mediaUrl: "", mediaType: "" },
    { eyebrow: "Cumpleañeros", title: "15% OFF", text: "Presentando tu INE.", style: "light", mediaUrl: "", mediaType: "" }
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

function encodePromotions(promos) {
  return encodeURIComponent(JSON.stringify(promos));
}

function defaultPageTheme() {
  return {
    backgroundColor: "#fff9f1",
    backgroundSoftColor: "#fff2f7",
    headerColor: "#ffffff",
    buttonStartColor: "#ff4f93",
    buttonEndColor: "#ff8157",
    accentColor: "#ff4f93",
    textColor: "#2a1714",
    cardColor: "#ffffff",
    iconColor: "#fff2f7"
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

function encodePageTheme(theme) {
  return encodeURIComponent(JSON.stringify(theme || defaultPageTheme()));
}

function applyPageThemePreview(theme) {
  var preview = document.getElementById("pageThemePreview");
  if (!preview) return;
  preview.style.setProperty("--preview-bg", `linear-gradient(135deg, ${theme.backgroundColor}, ${theme.backgroundSoftColor})`);
  preview.style.setProperty("--preview-header", theme.headerColor);
  preview.style.setProperty("--preview-button", `linear-gradient(135deg, ${theme.buttonStartColor}, ${theme.buttonEndColor})`);
  preview.style.setProperty("--preview-card", theme.cardColor);
  preview.style.setProperty("--preview-text", theme.textColor);
  preview.style.setProperty("--preview-accent", theme.accentColor);
  preview.style.setProperty("--preview-icon", theme.iconColor);
}

function setPageThemeForm(theme) {
  pageThemeData = Object.assign(defaultPageTheme(), theme || {});
  Object.keys(pageThemeData).forEach(function(key) {
    var input = document.getElementById("theme" + key.charAt(0).toUpperCase() + key.slice(1));
    if (input) input.value = pageThemeData[key];
  });
  applyPageThemePreview(pageThemeData);
}

function readPageThemeForm() {
  var theme = defaultPageTheme();
  Object.keys(theme).forEach(function(key) {
    var input = document.getElementById("theme" + key.charAt(0).toUpperCase() + key.slice(1));
    if (input) theme[key] = input.value || theme[key];
  });
  pageThemeData = theme;
  applyPageThemePreview(theme);
  return theme;
}

function resetPageTheme() {
  setPageThemeForm(defaultPageTheme());
}

function renderPromoEditors() {
  var container = document.getElementById("promoEditors");
  if (!container) return;
  container.innerHTML = promoEditorsData.map(function(promo, index) {
    var style = promoStyleOptions.some(function(option) { return option.value === promo.style; }) ? promo.style : "dark";
    var mediaLayout = promo.mediaLayout === "floating" ? "floating" : "full";
    var mediaSize = Math.min(150, Math.max(64, parseInt(promo.mediaSize, 10) || 94));
    var styleOptions = promoStyleOptions.map(function(option) {
      return `<option value="${option.value}" ${style === option.value ? "selected" : ""}>${option.label}</option>`;
    }).join("");
    var mediaPreview = "";
    if (promo.mediaUrl) {
      var src = resolveImageUrl(promo.mediaUrl);
      mediaPreview = promo.mediaType === "video"
        ? `<video src="${src}" muted playsinline controls></video>`
        : `<img src="${src}" alt="Promo ${index + 1}" onerror="this.style.display='none'" />`;
    }
    return `
      <div class="promo-editor-card" data-index="${index}">
        <div class="promo-editor-preview ${style} ${promo.mediaUrl ? "with-media media-" + mediaLayout : ""}" style="--promo-media-size:${mediaSize}px;">
          ${mediaPreview}
          <div>
            <span>${escapeHtml(promo.eyebrow || "Promo")}</span>
            <strong>${escapeHtml(promo.title || "Título")}</strong>
            <p>${escapeHtml(promo.text || "Descripción")}</p>
          </div>
        </div>
        <div class="promo-editor-fields">
          <input type="text" value="${escapeHtml(promo.eyebrow || "")}" placeholder="Etiqueta, ej. Happy Hour" oninput="updatePromoEditor(${index}, 'eyebrow', this.value)" />
          <input type="text" value="${escapeHtml(promo.title || "")}" placeholder="Título grande" oninput="updatePromoEditor(${index}, 'title', this.value)" />
          <textarea placeholder="Descripción" oninput="updatePromoEditor(${index}, 'text', this.value)">${escapeHtml(promo.text || "")}</textarea>
          <select onchange="updatePromoEditor(${index}, 'mediaLayout', this.value)">
            <option value="full" ${mediaLayout === "full" ? "selected" : ""}>Imagen en recuadro completo</option>
            <option value="floating" ${mediaLayout === "floating" ? "selected" : ""}>Imagen como recuadro flotante</option>
          </select>
          <label class="promo-range-field ${mediaLayout === "floating" ? "" : "disabled"}">
            Tamaño de imagen flotante
            <span id="promoSizeLabel${index}">${mediaSize}px</span>
            <input type="range" min="64" max="150" value="${mediaSize}" ${mediaLayout === "floating" ? "" : "disabled"} oninput="updatePromoEditor(${index}, 'mediaSize', this.value)" />
          </label>
          <select onchange="updatePromoEditor(${index}, 'style', this.value)">
            ${styleOptions}
          </select>
          <input type="url" value="${escapeHtml(promo.mediaUrl || "")}" placeholder="URL de imagen o video" oninput="updatePromoEditor(${index}, 'mediaUrl', this.value)" />
          <div class="promo-editor-actions">
            <label class="btn-license promo-upload-btn">
              Subir imagen/video
              <input type="file" accept="image/*,video/mp4,video/webm,video/quicktime" onchange="uploadPromoMedia(${index}, this)" />
            </label>
            <button type="button" class="btn-license btn-license-cancel" onclick="removePromoEditor(${index})">Eliminar</button>
          </div>
        </div>
      </div>
    `;
  }).join("");
}

function updatePromoEditor(index, field, value) {
  if (!promoEditorsData[index]) return;
  promoEditorsData[index][field] = value;
  if (field === "mediaUrl") {
    promoEditorsData[index].mediaType = /\.(mp4|webm|mov)(\?|$)/i.test(value) ? "video" : value ? "image" : "";
  }
  if (field === "mediaSize") {
    var size = Math.min(150, Math.max(64, parseInt(value, 10) || 94));
    promoEditorsData[index].mediaSize = size;
    var preview = document.querySelector(`.promo-editor-card[data-index="${index}"] .promo-editor-preview`);
    var label = document.getElementById("promoSizeLabel" + index);
    if (preview) preview.style.setProperty("--promo-media-size", size + "px");
    if (label) label.textContent = size + "px";
    return;
  }
  if (field === "style" || field === "mediaUrl" || field === "mediaLayout") renderPromoEditors();
}

function addPromoEditor() {
  promoEditorsData.push({ eyebrow: "Nueva promo", title: "Título", text: "Descripción de la promoción", style: "pink", mediaUrl: "", mediaType: "", mediaLayout: "floating", mediaSize: 94 });
  renderPromoEditors();
}

function removePromoEditor(index) {
  promoEditorsData.splice(index, 1);
  renderPromoEditors();
}

async function uploadPromoMedia(index, input) {
  var file = input.files && input.files[0];
  if (!file || !promoEditorsData[index]) return;
  var auth = getAdminAuthToken();
  if (!auth) {
    alert("Vuelve a iniciar sesión de administrador.");
    return;
  }
  var formData = new FormData();
  formData.append("file", file);
  try {
    var res = await fetch("/Pizzería Padua/api/v1/Settings/upload-promo-media", {
      method: "POST",
      headers: { "X-License-Auth": auth },
      body: formData
    });
    var data = await res.json();
    if (!res.ok || !data.success) throw new Error(data.message || "No se pudo subir");
    promoEditorsData[index].mediaUrl = data.mediaUrl;
    promoEditorsData[index].mediaType = data.mediaType;
    renderPromoEditors();
  } catch (err) {
    alert("Error al subir archivo: " + err.message);
  }
}

function loadSettings() {
  if (!daysInitialized) {
    daysInitialized = true;
    document.querySelectorAll(".day-btn").forEach(function(b) {
      b.addEventListener("click", function() { this.classList.toggle("active"); });
    });
  }
  fetch("/Pizzería Padua/api/v1/Settings").then(r => r.json()).then(s => {
    var parts = (s.address || "").split("|");
    document.getElementById("setAddress").value = parts[0] || "";
    document.getElementById("setPhone").value = parts[1] || "";
    document.getElementById("setMapsUrl").value = parts[4] || "";
    document.getElementById("setOpening").value = s.openingTime || "09:00";
    document.getElementById("setClosing").value = s.closingTime || "22:00";
    promoEditorsData = parsePromotionsFromParts(parts);
    setPageThemeForm(parsePageThemeFromParts(parts));
    renderPromoEditors();
    var days = (parts[3] || "1,2,3,4,5,6,0").split(",").map(Number);
    document.querySelectorAll(".day-btn").forEach(function(b) {
      b.classList.toggle("active", days.indexOf(parseInt(b.dataset.day)) !== -1);
    });
  }).catch(() => {});
}

function saveSettings(context) {
  var msg = document.getElementById(context === "promos" ? "promoSettingsMsg" : context === "page" ? "pageSettingsMsg" : "settingsMsg") || document.getElementById("settingsMsg");
  var auth = getAdminAuthToken();
  if (!auth) {
    msg.style.color = "red";
    msg.textContent = "✖ Vuelve a iniciar sesión de administrador.";
    return;
  }
  var addr = document.getElementById("setAddress").value.trim();
  var phone = document.getElementById("setPhone").value.trim();
  var mapsUrl = document.getElementById("setMapsUrl").value.trim();
  var days = Array.from(document.querySelectorAll(".day-btn.active")).map(function(b) { return b.dataset.day; }).join(",");
  var cleanPromos = promoEditorsData
    .map(function(p) {
      return {
        eyebrow: (p.eyebrow || "").trim(),
        title: (p.title || "").trim(),
        text: (p.text || "").trim(),
        style: p.style || "dark",
        mediaUrl: (p.mediaUrl || "").trim(),
        mediaType: p.mediaType || (/\.(mp4|webm|mov)(\?|$)/i.test(p.mediaUrl || "") ? "video" : (p.mediaUrl ? "image" : "")),
        mediaLayout: p.mediaLayout === "floating" ? "floating" : "full",
        mediaSize: Math.min(150, Math.max(64, parseInt(p.mediaSize, 10) || 94))
      };
    })
    .filter(function(p) { return p.eyebrow || p.title || p.text || p.mediaUrl; });
  var theme = readPageThemeForm();
  var fullAddr = addr + "|" + phone + "||" + days + "|" + mapsUrl + "|" + encodePromotions(cleanPromos.length ? cleanPromos : defaultPromotions()) + "|" + encodePageTheme(theme);
  var data = {
    address: fullAddr,
    openingTime: document.getElementById("setOpening").value,
    closingTime: document.getElementById("setClosing").value
  };
  fetch("/Pizzería Padua/api/v1/Settings", {
    method: "PUT",
    headers: { "Content-Type": "application/json", "X-License-Auth": auth },
    body: JSON.stringify(data)
  }).then(r => r.json()).then(res => {
    msg.style.color = res.success ? "green" : "red";
    msg.textContent = res.success
      ? (context === "promos" ? "✔ Promociones guardadas" : context === "page" ? "✔ Colores guardados" : "✔ Configuración guardada")
      : "✖ " + (res.message || "Error");
  }).catch(() => {
    msg.style.color = "red";
    msg.textContent = "✖ Error de conexión";
  });
}

async function selectHistoryFolder() {
  try {
    if (!window.showDirectoryPicker || !window.indexedDB) {
      historyDownloadFallback = true;
      lastHistoryData = null;
      await saveHistoryToFile(true);
      return;
    }

    if (window.showDirectoryPicker) {
      if (storedHistoryFolderHandle) {
        const restoredPermission = await storedHistoryFolderHandle.requestPermission({ mode: "readwrite" });
        if (restoredPermission === "granted") {
          historyFolderHandle = storedHistoryFolderHandle;
        }
      }

      if (!historyFolderHandle) {
        historyFolderHandle = await window.showDirectoryPicker({
          id: "morelos-tech-history",
          mode: "readwrite"
        });
        storedHistoryFolderHandle = historyFolderHandle;
        await saveHistoryFolderHandle(historyFolderHandle);
      }
      
      localStorage.setItem("historyFolderName", historyFolderHandle.name);
      
      // Actualizar UI
      document.getElementById("historyPathMsg").textContent = "✓ Carpeta: " + historyFolderHandle.name;
      document.getElementById("historyPathMsg").style.color = "green";
      updateCurrentHistoryPath(historyFolderHandle.name);
      
      // Ocultar toast después de 2 segundos
      setTimeout(() => {
        document.getElementById("historyPathToast").style.display = "none";
      }, 2000);
      
      lastHistoryData = null;
      await saveHistoryToFile();
    } else {
      document.getElementById("historyPathMsg").textContent = "Este navegador no permite reemplazar archivos locales. Abre el panel en Chrome, Edge o Brave desde una computadora.";
      document.getElementById("historyPathMsg").style.color = "#c0392b";
      updateCurrentHistoryPath("Guardado automático no disponible");
    }
  } catch (err) {
    if (err.name !== "AbortError") {
      alert("Error al seleccionar carpeta: " + err.message);
    }
  }
}

function updateCurrentHistoryPath(folderName) {
  const element = document.getElementById("currentHistoryPath");
  if (element) {
    element.textContent = "📁 " + folderName;
  }
}

async function changeHistoryFolder() {
  historyFolderHandle = null;
  storedHistoryFolderHandle = null;
  await selectHistoryFolder();
}

function openHistoryDatabase() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(HISTORY_DB_NAME, 1);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(HISTORY_DB_STORE)) {
        db.createObjectStore(HISTORY_DB_STORE);
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

async function saveHistoryFolderHandle(handle) {
  const db = await openHistoryDatabase();
  await new Promise((resolve, reject) => {
    const transaction = db.transaction(HISTORY_DB_STORE, "readwrite");
    transaction.objectStore(HISTORY_DB_STORE).put(handle, HISTORY_FOLDER_KEY);
    transaction.oncomplete = resolve;
    transaction.onerror = () => reject(transaction.error);
  });
  db.close();
}

async function loadHistoryFolderHandle() {
  const db = await openHistoryDatabase();
  const handle = await new Promise((resolve, reject) => {
    const request = db.transaction(HISTORY_DB_STORE, "readonly")
      .objectStore(HISTORY_DB_STORE)
      .get(HISTORY_FOLDER_KEY);
    request.onsuccess = () => resolve(request.result || null);
    request.onerror = () => reject(request.error);
  });
  db.close();
  return handle;
}

async function restoreHistoryFolder() {
  const toast = document.getElementById("historyPathToast");
  const message = document.getElementById("historyPathMsg");

  if (!window.showDirectoryPicker || !window.indexedDB) {
    historyDownloadFallback = true;
    updateCurrentHistoryPath("📥 Celular: usa el botón para descargar a Descargas");
    toast.style.display = "block";
    message.textContent = "En celular el navegador no permite reemplazar archivos locales. Desde gestión puedes descargar los históricos a Descargas con el botón.";
    return;
  }

  try {
    storedHistoryFolderHandle = await loadHistoryFolderHandle();
    if (!storedHistoryFolderHandle) {
      toast.style.display = "block";
      return;
    }

    const permission = await storedHistoryFolderHandle.queryPermission({ mode: "readwrite" });
    updateCurrentHistoryPath(storedHistoryFolderHandle.name);
    localStorage.setItem("historyFolderName", storedHistoryFolderHandle.name);

    if (permission === "granted") {
      historyFolderHandle = storedHistoryFolderHandle;
      toast.style.display = "none";
      message.textContent = "";
    } else {
      toast.style.display = "block";
      message.textContent = "Confirma una vez el acceso para seguir actualizando la carpeta " + storedHistoryFolderHandle.name + ".";
    }
  } catch (err) {
    console.error("No se pudo restaurar la carpeta del historial:", err);
    toast.style.display = "block";
  }
}

async function renotifyOrder(orderId) {
  try {
    const result = await apiFetch(`${API.orders}/${orderId}/notify`, { method: "POST" });
    alert(result.message || "Cliente notificado nuevamente");
    loadOrders();
  } catch (err) {
    alert("No se pudo volver a notificar: " + err.message);
  }
}

function getHistoryDateStamp() {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: "America/Mexico_City",
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  }).formatToParts(new Date());
  const values = Object.fromEntries(parts.map(part => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

function getHistoryDateTimeStamp() {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: "America/Mexico_City",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  }).formatToParts(new Date());
  const values = Object.fromEntries(parts.map(part => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}_${values.hour}-${values.minute}-${values.second}`;
}

function downloadHistoryCsv(fileName, content) {
  const blob = new Blob(["\uFEFF" + content], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

async function saveHistoryToFile(forceDownload = false) {
  if (historySyncInProgress) return;

  if (historyFolderReadyPromise) {
    await historyFolderReadyPromise;
  }

  if (historyDownloadFallback && !forceDownload) return;
  if (!historyDownloadFallback && !historyFolderHandle) return;
  if (historySyncInProgress) return;
  historySyncInProgress = true;

  try {
    // Obtener datos actuales
    const orders = await apiFetch(API.orders);
    const completed = orders.filter(o => o.status === 2);
    const users = await fetch("/Pizzería Padua/api/v1/Accounts").then(r => r.json());
    
    // Verificar si hay cambios
    const dateStamp = getHistoryDateStamp();
    const currentData = JSON.stringify({ dateStamp, orders: completed, users });
    if (currentData === lastHistoryData) return;
    
    // Crear contenido CSV para historial
    let csvContent = "Pedido,Cliente,Fecha,Total,Productos\n";
    completed.forEach(o => {
      const products = o.orderItems ? o.orderItems.map(oi => {
        const name = oi.product ? oi.product.name : "Producto #" + oi.productId;
        const custom = oi.customizations ? ` [${oi.customizations}]` : "";
        return `${oi.quantity}x ${name}${custom} ($${oi.subtotal.toFixed(2)})`;
      }).join("; ") : "";
      
      const customerName = o.customerName || "Sin nombre";
      const orderDate = new Date(o.orderDate).toLocaleString("es-MX");
      
      csvContent += `${o.orderId},"${customerName}","${orderDate}",${o.totalAmount.toFixed(2)},"${products}"\n`;
    });
    
    // Crear contenido CSV para usuarios
    let usersContent = "ID,Usuario,Correo,Teléfono,Fecha Registro,Estado,Conectado,Plataforma,Ultima Actividad\n";
    users.forEach(u => {
      const status = u.isBlocked ? "Bloqueado" : "Activo";
      usersContent += `${u.id},"${u.username}","${u.email}","${u.phoneNumber || ''}","${u.createdAt}","${status}","${u.isOnline ? 'En línea' : 'Desconectado'}","${u.loginProvider || ''}","${u.lastSeenAt || ''}"\n`;
    });
    
    const historyFileName = `historico_compras_${dateStamp}.csv`;
    const usersFileName = `historico_usuarios_${dateStamp}.csv`;

    if (historyDownloadFallback) {
      const fileStamp = getHistoryDateTimeStamp();
      downloadHistoryCsv(`historico_compras_${fileStamp}.csv`, csvContent);
      setTimeout(() => downloadHistoryCsv(`historico_usuarios_${fileStamp}.csv`, usersContent), 300);
      lastHistoryData = currentData;
      updateCurrentHistoryPath("📥 Descargado a Descargas · " + new Date().toLocaleTimeString("es-MX"));
      document.getElementById("historyPathMsg").textContent = "Históricos descargados. En celular no se pueden reemplazar archivos automáticamente.";
      document.getElementById("historyPathMsg").style.color = "green";
      return;
    }

    const historyFile = await historyFolderHandle.getFileHandle(historyFileName, { create: true });
    const historyWritable = await historyFile.createWritable();
    await historyWritable.write("\uFEFF" + csvContent);
    await historyWritable.close();

    const usersFile = await historyFolderHandle.getFileHandle(usersFileName, { create: true });
    const usersWritable = await usersFile.createWritable();
    await usersWritable.write("\uFEFF" + usersContent);
    await usersWritable.close();

    lastHistoryData = currentData;
    updateCurrentHistoryPath(historyFolderHandle.name + " · Actualizado " + new Date().toLocaleTimeString("es-MX"));
  } catch (err) {
    console.error("Error al guardar historial:", err);
    if (err.name === "NotAllowedError") {
      historyFolderHandle = null;
      document.getElementById("historyPathToast").style.display = "block";
      document.getElementById("historyPathMsg").textContent = "Reactiva el permiso de la carpeta para continuar actualizando los archivos.";
    }
  } finally {
    historySyncInProgress = false;
  }
}

async function loadUsers() {
  if (usersRefreshInProgress) return;
  usersRefreshInProgress = true;
  try {
    const res = await fetch("/Pizzería Padua/api/v1/Accounts");
    if (!res.ok) throw new Error("HTTP " + res.status);
    const users = await res.json();
    const tbody = document.getElementById("usersTableBody");
    tbody.innerHTML = users.map(u => `
      <tr style="${u.isBlocked ? 'background:#ffe5e5;' : ''}">
        <td>${u.id}</td>
        <td>${u.username} ${u.isBlocked ? '<span style="color:#e74c3c;font-size:11px;">(Bloqueado)</span>' : ''}</td>
        <td>${u.email}</td>
        <td><span class="session-status ${u.isOnline ? 'session-online' : 'session-offline'}">${u.isOnline ? 'En línea' : 'Desconectado'}</span></td>
        <td>${renderLoginProvider(u.loginProvider)}</td>
        <td>${u.lastSeenAt || 'Sin actividad reciente'}</td>
        <td>${u.createdAt}</td>
        <td><div class="user-actions">
          <button class="user-action-btn btn-session" onclick="forceLogoutUser(${u.id})" ${u.isOnline ? '' : 'disabled'}>Cerrar sesión</button>
          <button class="user-action-btn ${u.isBlocked ? 'btn-user-unblock' : 'btn-user-block'}" onclick="blockUser(${u.id}, '${u.username}', ${!u.isBlocked})">${u.isBlocked ? 'Desbloquear' : 'Bloquear'}</button>
          <button class="user-action-btn btn-user-delete" onclick="deleteUser(${u.id}, '${u.username}')">Eliminar</button>
        </div>
        </td>
      </tr>
    `).join("");
    await saveHistoryToFile();
  } catch (err) {
    console.error("Error al cargar usuarios:", err);
  } finally {
    usersRefreshInProgress = false;
  }
}

function renderLoginProvider(provider) {
  if (provider === "Google") {
    return '<span class="platform-badge platform-google"><span class="platform-icon">G</span>Google</span>';
  }
  if (provider === "Facebook") {
    return '<span class="platform-badge platform-facebook"><span class="platform-icon">f</span>Facebook</span>';
  }
  if (provider === "Correo") {
    return '<span class="platform-badge platform-email"><span class="platform-icon">✉</span>Correo</span>';
  }
  return '<span class="platform-badge platform-unknown"><span class="platform-icon">?</span>Sin sesión</span>';
}

async function forceLogoutUser(userId) {
  if (!confirm("¿Cerrar la sesión activa de este usuario?")) return;

  try {
    const response = await fetch(`/Pizzería Padua/api/v1/Accounts/${userId}/force-logout`, { method: "POST" });
    const data = await response.json();
    if (!response.ok || !data.success) {
      alert(data.message || "No se pudo cerrar la sesión");
      return;
    }
    await loadUsers();
  } catch (err) {
    alert("Error de conexión al cerrar la sesión");
  }
}

async function deleteUser(userId, username) {
  if (!confirm(`¿Estás seguro de eliminar al usuario "${username}"? Esta acción no se puede deshacer.`)) {
    return;
  }
  
  try {
    const res = await fetch(`/Pizzería Padua/api/v1/Accounts/${userId}`, {
      method: "DELETE"
    });
    const data = await res.json();
    
    if (data.success) {
      alert(`Usuario "${username}" eliminado correctamente`);
      loadUsers();
    } else {
      alert(`Error: ${data.message || "No se pudo eliminar el usuario"}`);
    }
  } catch (err) {
    alert("Error de conexión al eliminar el usuario");
  }
}

async function blockUser(userId, username, block) {
  const action = block ? 'bloquear' : 'desbloquear';
  if (!confirm(`¿Estás seguro de ${action} al usuario "${username}"?`)) {
    return;
  }
  
  try {
    const res = await fetch(`/Pizzería Padua/api/v1/Accounts/block`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId: userId, isBlocked: block })
    });
    const data = await res.json();
    
    if (data.success) {
      alert(`Usuario "${username}" ${block ? 'bloqueado' : 'desbloqueado'} correctamente`);
      loadUsers();
    } else {
      alert(`Error: ${data.message || "No se pudo ${action} el usuario"}`);
    }
  } catch (err) {
    alert(`Error de conexión al ${action} el usuario`);
  }
}

function openDeleteHistoryModal() {
  document.getElementById("deleteHistoryModal").classList.add("show");
}

function closeDeleteHistoryModal() {
  document.getElementById("deleteHistoryModal").classList.remove("show");
}

async function confirmDeleteHistory() {
  try {
    const auth = getAdminAuthToken();
    if (!auth) {
      alert("Vuelve a iniciar sesión de administrador.");
      return;
    }
    const res = await fetch("/Pizzería Padua/api/v1/Orders/history", {
      method: "DELETE",
      headers: { "X-License-Auth": auth }
    });
    const data = await res.json();
    
    if (data.success) {
      alert(data.message || "Historial eliminado correctamente");
      closeDeleteHistoryModal();
      loadSalesHistory();
    } else {
      alert("Error: " + (data.message || "No se pudo eliminar el historial"));
    }
  } catch (err) {
    alert("Error de conexión al eliminar el historial");
  }
}
