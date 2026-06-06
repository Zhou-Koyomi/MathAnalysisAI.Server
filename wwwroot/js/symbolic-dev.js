(function () {
  function getEl(id) {
    return document.getElementById(id);
  }

  function hasSymbolicDom() {
    return !!(
      getEl("devToolsArea") &&
      getEl("symbolicComputeBtn") &&
      getEl("symbolicOperation") &&
      getEl("symbolicExpression") &&
      getEl("symbolicResult") &&
      getEl("symbolicStatus")
    );
  }

  function buildPayload() {
    const operation = (getEl("symbolicOperation")?.value || "").trim();
    const expression = (getEl("symbolicExpression")?.value || "").trim();
    const variable = (getEl("symbolicVariable")?.value || "").trim();
    const lower = (getEl("symbolicLower")?.value || "").trim();
    const upper = (getEl("symbolicUpper")?.value || "").trim();
    const point = (getEl("symbolicPoint")?.value || "").trim();
    const orderRaw = (getEl("symbolicOrder")?.value || "").trim();

    const payload = {
      operation: operation,
      expression: expression,
      inputFormat: "plain",
      outputFormat: "text"
    };

    if (variable) payload.variable = variable;
    if (lower) payload.lower = lower;
    if (upper) payload.upper = upper;
    if (point) payload.point = point;

    if (orderRaw) {
      const parsed = Number.parseInt(orderRaw, 10);
      if (Number.isFinite(parsed) && !Number.isNaN(parsed)) {
        payload.order = parsed;
      }
    }

    return payload;
  }

  function renderWarnings(warnings) {
    const list = UI.safeList(warnings).map(x => String(x || "").trim()).filter(Boolean);
    if (!list.length) return "<div><strong>warnings：</strong>无</div>";
    return "<div><strong>warnings：</strong>" + UI.renderList(list) + "</div>";
  }

  function renderResult(data) {
    const box = getEl("symbolicResult");
    if (!box) return;

    const success = !!(data && data.success === true);
    const resultText = UI.escapeHtml(data?.resultText || "");
    const resultLatex = UI.escapeHtml(data?.resultLatex || "");
    const engine = UI.escapeHtml(data?.engine || "");
    const version = UI.escapeHtml(data?.engineVersion || "");
    const elapsedMs = UI.escapeHtml(data?.elapsedMs ?? "");
    const errorCode = UI.escapeHtml(data?.errorCode || "");
    const errorMessage = UI.escapeHtml(data?.errorMessage || "");

    let html = "";
    html += "<div><strong>success：</strong>" + (success ? "true" : "false") + "</div>";
    html += "<div><strong>resultText：</strong>" + (resultText || "(空)") + "</div>";
    html += "<div><strong>resultLatex：</strong>" + (resultLatex || "(空)") + "</div>";
    html += "<div><strong>engine：</strong>" + engine + "</div>";
    html += "<div><strong>engineVersion：</strong>" + (version || "(未知)") + "</div>";
    html += "<div><strong>elapsedMs：</strong>" + (elapsedMs || "0") + "</div>";
    html += "<div><strong>errorCode：</strong>" + (errorCode || "(无)") + "</div>";
    html += "<div><strong>errorMessage：</strong>" + (errorMessage || "(无)") + "</div>";
    html += renderWarnings(data?.warnings);

    box.innerHTML = html;
    box.style.display = "block";
  }

  async function computeSymbolic() {
    if (!hasSymbolicDom()) return;

    const btn = getEl("symbolicComputeBtn");
    const status = getEl("symbolicStatus");

    const payload = buildPayload();
    if (!payload.expression) {
      UI.showStatus(status, "请先输入表达式。", true);
      return;
    }

    if (!document.getElementById("devToolsArea")) {
      UI.showStatus(status, "无权访问开发工具。", true);
      return;
    }

    btn.disabled = true;
    UI.showStatus(status, "计算中，请稍候……", false);

    try {
      const data = await Api.postJson("/api/symbolic/compute", payload);
      renderResult(data || {});
      UI.showStatus(status, "计算完成。", false);
    } catch (err) {
      let message = "计算失败，请稍后重试。";
      const data = err && err.data ? err.data : null;
      if (data && (data.message || data.title)) {
        message = UI.escapeHtml(data.message || data.title);
      }
      UI.showStatus(status, message, true);
    } finally {
      btn.disabled = false;
      if (window.MathJax) MathJax.typeset();
    }
  }

  document.addEventListener("DOMContentLoaded", function () {
    if (!hasSymbolicDom()) return;

    const btn = getEl("symbolicComputeBtn");
    if (!btn) return;

    btn.addEventListener("click", function () {
      computeSymbolic();
    });
  });
})();
