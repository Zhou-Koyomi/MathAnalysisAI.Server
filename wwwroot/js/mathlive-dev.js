(function () {
  const DEFAULT_LATEX = "\\int_1^\\infty \\frac{1}{x^2}\\,dx";
  const EXAMPLES = {
    limit: "\\lim_{x\\to0}\\frac{\\sin x}{x}",
    matrix: "\\begin{pmatrix}1&2\\\\3&4\\end{pmatrix}",
    integral: "\\int_1^\\infty \\frac{1}{x^2}\\,dx"
  };

  function hasDom() {
    return !!(
      UI.qs("#devToolsArea") &&
      UI.qs("#mathliveDevCard") &&
      UI.qs("#mathliveDevField") &&
      UI.qs("#mathliveCurrentLatex")
    );
  }

  function isMathLiveLoaded() {
    return !!(window.MathfieldElement || customElements.get("math-field"));
  }

  function setLatexOutput(value) {
    const output = UI.qs("#mathliveCurrentLatex");
    if (!output) return;
    output.textContent = value || "";
  }

  function setFieldLatex(field, value) {
    field.value = value || "";
    setLatexOutput(field.value || "");
  }

  async function copyLatex(value) {
    if (!navigator.clipboard || !value) return false;
    try {
      await navigator.clipboard.writeText(value);
      return true;
    } catch (_) {
      return false;
    }
  }

  function bindButtons(field) {
    const limitBtn = UI.qs("#mathliveExampleLimitBtn");
    const matrixBtn = UI.qs("#mathliveExampleMatrixBtn");
    const integralBtn = UI.qs("#mathliveExampleIntegralBtn");
    const clearBtn = UI.qs("#mathliveClearBtn");
    const copyBtn = UI.qs("#mathliveCopyBtn");
    const hint = UI.qs("#mathliveLoadHint");

    if (limitBtn) limitBtn.addEventListener("click", () => setFieldLatex(field, EXAMPLES.limit));
    if (matrixBtn) matrixBtn.addEventListener("click", () => setFieldLatex(field, EXAMPLES.matrix));
    if (integralBtn) integralBtn.addEventListener("click", () => setFieldLatex(field, EXAMPLES.integral));
    if (clearBtn) clearBtn.addEventListener("click", () => setFieldLatex(field, ""));
    if (copyBtn) {
      copyBtn.addEventListener("click", async () => {
        const ok = await copyLatex(field.value || "");
        if (hint) {
          hint.style.display = "block";
          hint.textContent = ok ? "LaTeX 已复制。" : "复制失败，请手动复制。";
        }
      });
    }
  }

  function init() {
    if (!hasDom()) return;

    const hint = UI.qs("#mathliveLoadHint");
    const field = UI.qs("#mathliveDevField");
    if (!field) return;

    if (!isMathLiveLoaded()) {
      if (hint) {
        hint.style.display = "block";
        hint.textContent = "MathLive 未加载，请检查 /vendor/mathlive 文件是否存在。";
      }
      return;
    }

    if (hint) hint.style.display = "none";
    setFieldLatex(field, DEFAULT_LATEX);
    field.addEventListener("input", () => setLatexOutput(field.value || ""));
    bindButtons(field);
  }

  document.addEventListener("DOMContentLoaded", init);
})();
