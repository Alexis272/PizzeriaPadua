function applyTheme(theme) {
  const safeTheme = theme === "night" ? "night" : "day";
  document.body.dataset.theme = safeTheme;
  localStorage.setItem("pizzeriaPaduaTheme", safeTheme);

  const icon = document.getElementById("themeIcon");
  const toggle = document.getElementById("themeToggle");
  if (icon) icon.textContent = safeTheme === "night" ? "\u263e" : "\u2600";
  if (toggle) {
    toggle.setAttribute(
      "aria-label",
      safeTheme === "night" ? "Cambiar a modo dia" : "Cambiar a modo noche"
    );
  }
}

document.addEventListener("DOMContentLoaded", () => {
  const savedTheme = localStorage.getItem("pizzeriaPaduaTheme");
  const prefersDark = window.matchMedia &&
    window.matchMedia("(prefers-color-scheme: dark)").matches;
  applyTheme(savedTheme || (prefersDark ? "night" : "day"));

  const toggle = document.getElementById("themeToggle");
  if (!toggle) return;

  toggle.addEventListener("click", () => {
    const nextTheme = document.body.dataset.theme === "night" ? "day" : "night";
    applyTheme(nextTheme);
  });
});
