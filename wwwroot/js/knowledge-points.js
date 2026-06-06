(function () {
  const map = {
    "ma.improper_integral.convergence_criteria": "收敛与发散判定",
    "ma.improper_integral.comparison_test": "比较判别法",
    "ma.improper_integral.infinite_interval": "无穷限反常积分",
    "ma.improper_integral.unbounded_function": "无界函数反常积分"
  };

  function formatKnowledgePoint(code) {
    const c = String(code || "").trim();
    if (!c) return { name: "", code: "", known: false };
    return { name: map[c] || c, code: c, known: !!map[c] };
  }

  function renderKnowledgePoints(items) {
    const arr = UI.safeList(items);
    if (!arr.length) return "<div class='status'>暂无</div>";
    return "<ul class='list'>" + arr.map(code => {
      const fp = formatKnowledgePoint(code);
      if (!fp.known) return "<li>" + UI.escapeHtml(fp.code) + "</li>";
      return "<li>" + UI.escapeHtml(fp.name) + "<span class='code-weak'>" + UI.escapeHtml(fp.code) + "</span></li>";
    }).join("") + "</ul>";
  }

  window.KnowledgePoints = { map, formatKnowledgePoint, renderKnowledgePoints };
})();
