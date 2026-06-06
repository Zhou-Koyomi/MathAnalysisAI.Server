(function () {
  function renderLeaderboard(rows) {
    const data = Array.isArray(rows) ? rows : [];
    if (!data.length) return "<div class='status'>暂无排行榜数据，完成一次分析后会显示在这里。</div>";

    let html = "<table class='rank-table'><thead><tr>" +
      "<th>排名</th><th>用户名</th><th>练习次数</th><th>正确数</th><th>错误数</th><th>正确率</th><th>积分</th>" +
      "</tr></thead><tbody>";

    data.forEach((x, idx) => {
      const rank = x.rank || (idx + 1);
      const accuracy = x.accuracyRate == null ? "-" : String(x.accuracyRate);
      const score = x.rankingScore == null ? "-" : String(x.rankingScore);
      html += "<tr>" +
        "<td>" + UI.escapeHtml(rank) + "</td>" +
        "<td>" + UI.escapeHtml(x.username || "") + "</td>" +
        "<td>" + UI.escapeHtml(x.attemptCount || 0) + "</td>" +
        "<td>" + UI.escapeHtml(x.correctCount || 0) + "</td>" +
        "<td>" + UI.escapeHtml(x.wrongCount || 0) + "</td>" +
        "<td>" + UI.escapeHtml(accuracy) + "</td>" +
        "<td>" + UI.escapeHtml(score) + "</td>" +
        "</tr>";
    });

    return html + "</tbody></table>";
  }

  async function loadLeaderboard() {
    const box = UI.qs("#leaderboardContainer");
    if (!box) return;

    UI.showStatus(box, "加载中...", false);
    try {
      const rows = await Api.getJson("/api/leaderboard/public?courseId=" + AppConfig.defaultCourseId + "&take=" + AppConfig.leaderboardTake);
      box.className = "";
      box.innerHTML = renderLeaderboard(rows);
    } catch (_) {
      UI.showStatus(box, "排行榜加载失败，请稍后重试。", true);
    }
  }

  window.loadLeaderboard = loadLeaderboard;
})();
