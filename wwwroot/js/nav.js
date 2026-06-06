(function () {
  const routeMap = {
    "/": "/index.html",
    "/index.html": "/index.html",
    "/analysis.html": "/analysis.html",
    "/ocr.html": "/ocr.html",
    "/materials.html": "/materials.html",
    "/stats.html": "/stats.html",
    "/dev.html": "/dev.html",
    "/login.html": "/login.html"
  };

  function normalizePath(pathname) {
    if (!pathname || pathname === "/") return "/index.html";
    return routeMap[pathname] || pathname;
  }

  function setActiveNav() {
    const current = normalizePath(window.location.pathname);
    const links = document.querySelectorAll(".top-nav-link");
    links.forEach((link) => {
      const href = link.getAttribute("href") || "";
      if (href === current) {
        link.classList.add("active");
      } else {
        link.classList.remove("active");
      }
    });
  }

  function getUserText(user, fallbackApplied) {
    if (!user) return "当前未登录";
    const displayName = user.realName || user.username || "未知用户";
    const role = user.role || "student";
    if (fallbackApplied) {
      return "当前用户：" + displayName + "（" + role + "，开发模式）";
    }
    return "当前用户：" + displayName + "（" + role + "）";
  }

  function renderCurrentUserHint() {
    const hint = document.getElementById("currentUserHint");
    if (!hint) return;

    const auth = window.Auth;
    const user = auth ? auth.getCurrentUser() : null;
    const fallbackApplied = auth && auth.isDevelopmentFallbackApplied && auth.isDevelopmentFallbackApplied();
    hint.textContent = getUserText(user, !!fallbackApplied);
  }

  function applyRoleBasedNavVisibility() {
    const auth = window.Auth;
    const user = auth ? auth.getCurrentUser() : null;
    const role = user && user.role ? String(user.role).toLowerCase() : "";

    const materialsLinks = document.querySelectorAll('.top-nav-link[href="/materials.html"]');
    const devLinks = document.querySelectorAll('.top-nav-link[href="/dev.html"]');

    const canSeeMaterials = role === "teacher" || role === "admin";
    const canSeeDev = role === "admin";

    materialsLinks.forEach((link) => {
      link.style.display = canSeeMaterials ? "" : "none";
    });

    devLinks.forEach((link) => {
      link.style.display = canSeeDev ? "" : "none";
    });
  }

  function showAccessDenied(containerId, message) {
    const denied = document.getElementById(containerId);
    if (!denied) return;
    denied.innerHTML =
      "<div class='card access-denied-card'>" +
      "<div class='section-title'>无权访问</div>" +
      "<div class='status error'>" + UI.escapeHtml(message) + "</div>" +
      "<a class='jump-link' href='/index.html'>返回首页 →</a>" +
      "</div>";
    denied.style.display = "block";
  }

  function guardMaterialsPage() {
    if (normalizePath(window.location.pathname) !== "/materials.html") return;
    const auth = window.Auth;
    const allowed = auth && auth.hasAnyRole && auth.hasAnyRole(["teacher", "admin"]);
    if (allowed) return;

    const managerArea = document.getElementById("materialsManagerArea");
    if (managerArea) {
      managerArea.remove();
    }
    showAccessDenied("materialsAccessDenied", "无权访问课程资料管理");
  }

  function guardDevPage() {
    if (normalizePath(window.location.pathname) !== "/dev.html") return;
    const auth = window.Auth;
    const allowed = auth && auth.hasRole && auth.hasRole("admin");
    if (allowed) {
      return;
    }

    const managerArea = document.getElementById("devToolsArea");
    if (managerArea) {
      managerArea.remove();
    }
    showAccessDenied("devAccessDenied", "无权访问开发工具");
  }

  async function logout() {
    try {
      await Api.postJson("/api/auth/logout", {});
    } catch (_) {
      // Keep UX stable in development mode.
    }

    if (window.Auth && window.Auth.loadCurrentUser) {
      await window.Auth.loadCurrentUser(true);
    }

    const current = normalizePath(window.location.pathname);
    if (current === "/login.html") {
      renderTopNavUserArea();
      return;
    }

    window.location.href = "/login.html";
  }

  function renderTopNavUserArea() {
    const nav = document.querySelector(".top-nav");
    if (!nav) return;

    let area = nav.querySelector(".top-nav-user");
    if (!area) {
      area = document.createElement("div");
      area.className = "top-nav-user";
      nav.appendChild(area);
    }

    const auth = window.Auth;
    const user = auth ? auth.getCurrentUser() : null;
    const fallbackApplied = auth && auth.isDevelopmentFallbackApplied && auth.isDevelopmentFallbackApplied();

    const text = document.createElement("span");
    text.className = "top-nav-user-text";
    text.textContent = getUserText(user, !!fallbackApplied);

    const action = document.createElement("a");
    action.className = "top-nav-auth-link";

    if (user) {
      action.href = "#";
      action.textContent = "退出";
      action.addEventListener("click", function (e) {
        e.preventDefault();
        logout();
      });
    } else {
      action.href = "/login.html";
      action.textContent = "登录";
    }

    area.innerHTML = "";
    area.appendChild(text);
    area.appendChild(action);
  }

  document.addEventListener("DOMContentLoaded", async function () {
    if (window.Auth && window.Auth.loadCurrentUser) {
      await window.Auth.loadCurrentUser();
    }
    applyRoleBasedNavVisibility();
    setActiveNav();
    renderCurrentUserHint();
    renderTopNavUserArea();
    guardMaterialsPage();
    guardDevPage();
  });
})();
