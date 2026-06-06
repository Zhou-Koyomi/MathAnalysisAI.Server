window.UI = {
  qs(selector) { return document.querySelector(selector); },
  qsa(selector) { return Array.from(document.querySelectorAll(selector)); },
  setText(el, text) { if (el) el.textContent = text == null ? "" : String(text); },
  showStatus(el, text, isError) {
    if (!el) return;
    el.className = isError ? "status error" : "status";
    el.textContent = text || "";
  },
  escapeHtml(str) {
    return String(str ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  },
  safeList(v) {
    if (!v) return [];
    return Array.isArray(v) ? v : [String(v)];
  },
  renderList(items) {
    const arr = this.safeList(items);
    if (!arr.length) return "<div class='status'>暂无</div>";
    return "<ul class='list'>" + arr.map(x => "<li>" + this.escapeHtml(x) + "</li>").join("") + "</ul>";
  },
  formatRateLimitMessage(err) {
    var msg = (err && err.rateLimitMessage) || "请求过于频繁，请稍后重试。";
    var ra = (err && err.retryAfter) ? Number(err.retryAfter) : null;
    if (ra && Number.isFinite(ra) && ra > 0) {
      return msg + " 请约 " + ra + " 秒后重试。";
    }
    return msg;
  },
  toJudgementText(v) {
    if (v === true) return "基本正确";
    if (v === false) return "存在问题";
    return "暂无法确定";
  }
};

document.addEventListener("DOMContentLoaded", function () {
  if (window.loadLeaderboard && UI.qs("#leaderboardContainer")) {
    window.loadLeaderboard();
  }
});
