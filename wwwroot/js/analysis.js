(function () {
  function hasAnalysisDom() {
    return !!(
      UI.qs("#analyzeBtn") &&
      UI.qs("#analyzeStatus") &&
      UI.qs("#resultContainer") &&
      UI.qs("#problemTextInput") &&
      UI.qs("#studentSolutionTextInput") &&
      UI.qs("#chapterSelect") &&
      UI.qs("#modeSelect")
    );
  }

  function normalizeText(value, fallback) {
    if (value == null) return fallback || "";
    return String(value);
  }

  function normalizeList(list) {
    return UI.safeList(list).map(item => normalizeText(item, "").trim()).filter(Boolean);
  }

  function resolveStatus(isCorrect) {
    if (isCorrect === true) {
      return { text: "正确", className: "result-status-correct" };
    }
    if (isCorrect === false) {
      return { text: "需要修正", className: "result-status-wrong" };
    }
    return { text: "待判断", className: "result-status-unknown" };
  }

  function renderHeaderMeta(data) {
    const course = normalizeText(data.course, "未标注课程");
    const chapter = normalizeText(data.chapter, "未标注章节");
    const problemType = normalizeText(data.problemType, "unknown");
    const difficulty = normalizeText(data.difficulty, "unknown");

    return "<div class='result-summary-meta'>" +
      "<span>课程：" + UI.escapeHtml(course) + "</span>" +
      "<span>章节：" + UI.escapeHtml(chapter) + "</span>" +
      "<span>题型：" + UI.escapeHtml(problemType) + "</span>" +
      "<span>难度：" + UI.escapeHtml(difficulty) + "</span>" +
      "</div>";
  }

  function renderMistakeTags(tags) {
    const arr = normalizeList(tags);
    if (!arr.length) return "<div class='status'>暂未发现明确问题</div>";
    return "<div class='pill-row'>" + arr.map(tag =>
      "<span class='mistake-tag-pill'>" + UI.escapeHtml(tag) + "</span>"
    ).join("") + "</div>";
  }

  function renderKnowledgePoints(codes) {
    const arr = normalizeList(codes);
    if (!arr.length) return "<div class='status'>暂无关联知识点</div>";

    return "<div class='pill-row'>" + arr.map(code => {
      const kp = window.KnowledgePoints && window.KnowledgePoints.formatKnowledgePoint
        ? window.KnowledgePoints.formatKnowledgePoint(code)
        : { name: code, code: code, known: false };

      const name = UI.escapeHtml(kp.name || kp.code || code);
      const rawCode = UI.escapeHtml(kp.code || code);
      return "<span class='knowledge-pill'><span class='knowledge-name'>" + name +
        "</span><span class='knowledge-code'>" + rawCode + "</span></span>";
    }).join("") + "</div>";
  }

  function renderSolutionSteps(steps) {
    const arr = UI.safeList(steps);
    if (!arr.length) return "<div class='status'>暂无标准解答步骤</div>";

    let html = "";
    arr.forEach((item, index) => {
      const step = typeof item === "object" && item ? item : null;
      const stepNo = normalizeText(step && step.step, index + 1);
      const title = normalizeText(step && step.title, "标准解答");
      const content = normalizeText(step && step.content, typeof item === "string" ? item : "");

      html += "<div class='solution-step-card'>" +
        "<div class='solution-step-head'>步骤 " + UI.escapeHtml(stepNo) + "：" + UI.escapeHtml(title) + "</div>" +
        "<div class='solution-step-content'>" + UI.escapeHtml(content).replace(/\n/g, "<br>") + "</div>" +
      "</div>";
    });

    return html;
  }

  function renderSuggestionList(list) {
    const arr = normalizeList(list);
    if (!arr.length) return "";
    return UI.renderList(arr);
  }

  function mergeSuggestions(reviewSuggestions, studentSuggestions) {
    const merged = [];
    const seen = new Set();
    const pushIfNew = (text) => {
      const normalized = normalizeText(text, "").trim();
      if (!normalized) return;
      const key = normalized.toLowerCase();
      if (seen.has(key)) return;
      seen.add(key);
      merged.push(normalized);
    };

    normalizeList(reviewSuggestions).forEach(pushIfNew);
    normalizeList(studentSuggestions).forEach(pushIfNew);
    return merged;
  }

  function renderVisualization(visualization) {
    const v = visualization && typeof visualization === "object" ? visualization : null;
    if (!v || v.shouldUse !== true) {
      return "";
    }

    const reason = normalizeText(v.reason, "");
    const caption = normalizeText(v.caption, "");
    const commands = normalizeList(v.geoGebraCommands);

    let html = "<div class='result-section'><h3>可视化建议</h3>";
    if (caption) {
      html += "<div><strong>图示标题：</strong>" + UI.escapeHtml(caption) + "</div>";
    }
    if (reason) {
      html += "<div><strong>说明：</strong>" + UI.escapeHtml(reason) + "</div>";
    }

    if (commands.length) {
      html += "<div style='margin-top:8px;'><strong>GeoGebra 命令：</strong></div>";
      html += UI.renderList(commands);
    } else {
      html += "<div class='status'>未返回可执行命令。</div>";
    }

    html += "</div>";
    return html;
  }

  function renderAnalysisReport(data) {
    const review = data && data.studentSolutionReview && typeof data.studentSolutionReview === "object"
      ? data.studentSolutionReview
      : {};
    const status = resolveStatus(review.isCorrect);
    const mainIssue = normalizeText(review.mainIssue, "").trim();
    const logicGaps = normalizeList(review.logicGaps);
    const mistakeTags = normalizeList(data.mistakeTags);
    const mergedSuggestions = mergeSuggestions(data.reviewSuggestions, review.suggestions);

    let html = "";

    html += "<div class='result-summary-card'>" +
      "<div class='result-summary-header'>" +
      "<span class='result-status-pill " + status.className + "'>" + UI.escapeHtml(status.text) + "</span>" +
      "</div>" +
      "<div class='result-summary-main'>" + UI.escapeHtml(mainIssue || "已完成分析，请查看下方详细反馈。") + "</div>" +
      renderHeaderMeta(data) +
      "</div>";

    html += "<div class='result-section'><h3>我的解答问题</h3>";
    html += "<div><strong>主要问题：</strong>" + UI.escapeHtml(mainIssue || "暂未发现明确问题") + "</div>";
    if (logicGaps.length) {
      html += "<div style='margin-top:8px;'><strong>逻辑漏洞：</strong></div>" + UI.renderList(logicGaps);
    }
    html += "<div style='margin-top:8px;'><strong>错误标签：</strong></div>" + renderMistakeTags(mistakeTags);
    html += "</div>";

    html += "<div class='result-section'><h3>标准解答</h3>" + renderSolutionSteps(data.standardSolution) + "</div>";

    const solutionOverview = normalizeText(data.solutionOverview, "").trim();
    if (solutionOverview) {
      html += "<div class='result-section'><h3>解题思路概览</h3><div>" +
        UI.escapeHtml(solutionOverview).replace(/\n/g, "<br>") +
      "</div></div>";
    }

    html += "<div class='result-section'><h3>关联知识点</h3>" + renderKnowledgePoints(data.knowledgePoints) + "</div>";

    if (mergedSuggestions.length) {
      html += "<div class='result-section'><h3>下一步建议</h3>" + renderSuggestionList(mergedSuggestions) + "</div>";
    }

    html += renderVisualization(data.visualization);
    return html;
  }

  async function analyzeText() {
    if (!hasAnalysisDom()) {
      return;
    }

    const btn = UI.qs("#analyzeBtn");
    const status = UI.qs("#analyzeStatus");
    const box = UI.qs("#resultContainer");
    const problemText = UI.qs("#problemTextInput").value.trim();
    const studentSolutionText = UI.qs("#studentSolutionTextInput").value.trim();
    const chapterId = parseInt(UI.qs("#chapterSelect").value, 10);
    const mode = UI.qs("#modeSelect").value;

    if (!problemText) {
      UI.showStatus(status, "请先输入题目。", true);
      return;
    }

    let currentUserId = null;
    if (window.Auth && window.Auth.getCurrentUserId) {
      currentUserId = window.Auth.getCurrentUserId();
      if (currentUserId == null && window.Auth.loadCurrentUser) {
        await window.Auth.loadCurrentUser();
        currentUserId = window.Auth.getCurrentUserId();
      }
    }
    if (currentUserId == null && window.AppConfig && AppConfig.developmentFallbackUser) {
      currentUserId = Number(AppConfig.developmentFallbackUser.userId) || null;
    }

    const payload = {
      courseId: AppConfig.defaultCourseId,
      chapterId: Number.isNaN(chapterId) ? AppConfig.defaultChapterId : chapterId,
      problemText,
      studentSolutionText,
      analysisMode: mode || "review_solution",
      userId: currentUserId
    };

    btn.disabled = true;
    UI.showStatus(status, "正在分析，请稍候……", false);

    try {
      const data = await Api.postJson("/api/learning-analysis/analyze", payload);
      box.innerHTML = renderAnalysisReport(data || {});
      UI.showStatus(status, "分析完成。", false);
    } catch (err) {
      let message;
      if (err && err.isRateLimited) {
        message = UI.formatRateLimitMessage(err);
      } else {
        message = "分析失败，请稍后重试。";
        const data = err && err.data ? err.data : null;
        const msg = data && (data.message || data.title) ? (data.message || data.title) : "";
        if (msg && (location.hostname === "localhost" || location.hostname === "127.0.0.1")) {
          message += "（开发信息：" + msg + "）";
        }
      }
      UI.showStatus(status, message, true);
    } finally {
      btn.disabled = false;
      if (window.MathJax) MathJax.typeset();
    }
  }

  window.analyzeText = analyzeText;
})();
