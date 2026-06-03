(function () {
  async function initStatsPage() {
    const userHint = UI.qs("#statsCurrentUserHint");
    if (window.Auth && window.Auth.loadCurrentUser) {
      await window.Auth.loadCurrentUser();
    }

    if (userHint) {
      const user = window.Auth && window.Auth.getCurrentUser ? window.Auth.getCurrentUser() : null;
      if (!user) {
        userHint.textContent = "当前未登录";
      } else {
        const displayName = user.realName || user.username || "未知用户";
        const role = user.role || "student";
        if (window.Auth && window.Auth.isDevelopmentFallbackApplied && window.Auth.isDevelopmentFallbackApplied()) {
          userHint.textContent = "当前用户：" + displayName + "（开发模式）";
        } else {
          userHint.textContent = "当前用户：" + displayName + "（" + role + "）";
        }
      }
    }

    if (window.loadLeaderboard) {
      window.loadLeaderboard();
    }
  }

  document.addEventListener("DOMContentLoaded", function () {
    if (!UI.qs("#statsPageRoot")) return;
    initStatsPage();
  });
})();
