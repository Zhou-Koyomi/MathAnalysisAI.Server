(function () {
  function renderUserInfo(user) {
    const info = UI.qs("#loginUserInfo");
    if (!info) return;

    if (!user) {
      UI.setText(info, "");
      return;
    }

    const displayName = user.realName || user.username || "未知用户";
    const role = user.role || "student";
    UI.setText(info, "当前用户：" + displayName + "（" + role + "）");
  }

  async function doLogin(username) {
    const status = UI.qs("#loginStatus");
    const loginBtn = UI.qs("#loginSubmitBtn");
    const quickBtn = UI.qs("#loginQuickBtn");

    const value = (username || "").trim();
    if (!value) {
      UI.showStatus(status, "请输入用户名。", true);
      return;
    }

    loginBtn.disabled = true;
    quickBtn.disabled = true;
    UI.showStatus(status, "正在登录……", false);

    try {
      await Api.postJson("/api/auth/login", { username: value });
      if (window.Auth && window.Auth.loadCurrentUser) {
        await window.Auth.loadCurrentUser(true);
      }
      const user = window.Auth && window.Auth.getCurrentUser ? window.Auth.getCurrentUser() : null;
      renderUserInfo(user);
      UI.showStatus(status, "登录成功，正在进入首页……", false);
      setTimeout(function () {
        window.location.href = "/index.html";
      }, 300);
    } catch (err) {
      let message = "登录失败，请检查用户名。";
      if (err && err.isRateLimited) {
        message = UI.formatRateLimitMessage(err);
      } else if (err && err.status === 401) {
        message = "用户名不存在或不可用。";
      }
      UI.showStatus(status, message, true);
    } finally {
      loginBtn.disabled = false;
      quickBtn.disabled = false;
    }
  }

  function loginWithUsername() {
    const input = UI.qs("#loginUsernameInput");
    doLogin(input ? input.value : "");
  }

  function quickLoginTestStudent() {
    doLogin("test_student");
  }

  async function initLoginPage() {
    if (!UI.qs("#loginPageRoot")) return;

    if (window.Auth && window.Auth.loadCurrentUser) {
      await window.Auth.loadCurrentUser();
      renderUserInfo(window.Auth.getCurrentUser());
    }

    const input = UI.qs("#loginUsernameInput");
    if (input) {
      input.addEventListener("keydown", function (evt) {
        if (evt.key === "Enter") {
          evt.preventDefault();
          loginWithUsername();
        }
      });
    }
  }

  window.loginWithUsername = loginWithUsername;
  window.quickLoginTestStudent = quickLoginTestStudent;

  document.addEventListener("DOMContentLoaded", function () {
    initLoginPage();
  });
})();
