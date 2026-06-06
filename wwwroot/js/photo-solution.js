(function () {
  function hasPhotoOcrDom() {
    return !!(
      UI.qs("#photoOcrBtn") &&
      UI.qs("#photoOcrStatus") &&
      UI.qs("#photoOcrSummary") &&
      UI.qs("#photoSolutionFile") &&
      UI.qs("#photoSolutionHint") &&
      UI.qs("#chapterSelect") &&
      UI.qs("#problemTextInput") &&
      UI.qs("#studentSolutionTextInput")
    );
  }

  async function recognizePhotoSolution() {
    if (!hasPhotoOcrDom()) {
      return;
    }

    const btn = UI.qs("#photoOcrBtn");
    const status = UI.qs("#photoOcrStatus");
    const summary = UI.qs("#photoOcrSummary");
    const fileInput = UI.qs("#photoSolutionFile");
    const hintInput = UI.qs("#photoSolutionHint");

    const file = (fileInput.files && fileInput.files[0]) ? fileInput.files[0] : null;
    if (!file) {
      UI.showStatus(status, "请先选择作业图片。", true);
      return;
    }

    const chapterId = parseInt(UI.qs("#chapterSelect").value, 10);
    const form = new FormData();
    form.append("courseId", String(AppConfig.defaultCourseId));
    form.append("chapterId", Number.isNaN(chapterId) ? String(AppConfig.defaultChapterId) : String(chapterId));
    if (hintInput.value && hintInput.value.trim()) {
      form.append("userHint", hintInput.value.trim());
    }
    form.append("file", file);

    btn.disabled = true;
    UI.showStatus(status, "正在识别，请稍候……", false);
    UI.setText(summary, "");

    try {
      const data = await Api.postFormData("/api/photo-solutions/ocr", form);
      UI.qs("#problemTextInput").value = (data.problemText || "").trim();
      UI.qs("#studentSolutionTextInput").value = (data.studentSolutionText || "").trim();

      const warningCount = Array.isArray(data.warnings) ? data.warnings.length : 0;
      const formulaCount = Array.isArray(data.formulas) ? data.formulas.length : 0;
      const sectionCount = Array.isArray(data.detectedSections) ? data.detectedSections.length : 0;
      UI.setText(summary, "识别完成：已回填题目与解答。分段 " + sectionCount + " 条，公式 " + formulaCount + " 条，警告 " + warningCount + " 条。");
      UI.showStatus(status, "识别完成。", false);

      if (window.MathLiveOcr && window.MathLiveOcr.renderFormulas) {
        window.MathLiveOcr.renderFormulas(Array.isArray(data.formulas) ? data.formulas : [], data);
      }
    } catch (err) {
      if (err && err.isRateLimited) {
        UI.showStatus(status, UI.formatRateLimitMessage(err), true);
      } else {
        UI.showStatus(status, "视觉 OCR 服务暂未配置或调用失败，请手动输入题目和解答。", true);
      }
      if (window.MathLiveOcr && window.MathLiveOcr.renderFormulas) {
        window.MathLiveOcr.renderFormulas([], null);
      }
    } finally {
      btn.disabled = false;
    }
  }

  window.recognizePhotoSolution = recognizePhotoSolution;
})();
