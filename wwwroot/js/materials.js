(function () {
  function toDisplayStatus(parseStatus) {
    const normalized = String(parseStatus || "").toLowerCase();
    if (normalized === "success") return "解析成功";
    if (normalized === "processing") return "解析中";
    if (normalized === "pending") return "等待解析";
    if (normalized === "failed") return "解析失败";
    if (normalized === "ocr_pending") return "需要 OCR";
    return parseStatus || "未知";
  }

  function toDisplayKind(kind) {
    const normalized = String(kind || "").toLowerCase();
    if (normalized === "textbook") return "教材";
    if (normalized === "lecture_note") return "讲义";
    if (normalized === "exercise_book") return "习题册";
    if (normalized === "handout") return "资料";
    if (normalized === "user_note") return "个人笔记";
    if (normalized === "other") return "其他";
    return kind || "未知";
  }

  function toDisplayChunkType(chunkType) {
    const normalized = String(chunkType || "").toLowerCase();
    if (normalized === "definition") return "定义";
    if (normalized === "theorem") return "定理";
    if (normalized === "proof") return "证明";
    if (normalized === "example") return "例题";
    if (normalized === "exercise") return "习题";
    if (normalized === "method") return "方法";
    if (normalized === "remark") return "备注";
    if (normalized === "explanation") return "讲解";
    if (normalized === "unknown") return "未分类";
    return chunkType || "未知";
  }

  function formatDateTime(value) {
    if (!value) return "-";
    const dt = new Date(value);
    if (Number.isNaN(dt.getTime())) return "-";
    return dt.toLocaleString("zh-CN", { hour12: false });
  }

  function formatFileSize(bytes) {
    const value = Number(bytes || 0);
    if (!Number.isFinite(value) || value <= 0) return "0 B";
    if (value < 1024) return value + " B";
    if (value < 1024 * 1024) return (value / 1024).toFixed(1) + " KB";
    return (value / 1024 / 1024).toFixed(2) + " MB";
  }

  function renderList(items) {
    const container = UI.qs("#materialsListContainer");
    if (!container) return;

    const arr = Array.isArray(items) ? items : [];
    if (!arr.length) {
      container.className = "status";
      container.textContent = "暂无课程资料，上传 PDF 后会显示在这里。";
      return;
    }

    let html = "";
    arr.forEach((item) => {
      const statusText = toDisplayStatus(item.parseStatus);
      const kindText = toDisplayKind(item.materialKind);
      const parseMessage = item.parseMessage ? item.parseMessage : "无";
      const chunkCount = item.chunkCount == null ? 0 : item.chunkCount;
      const showOcrHint = String(item.parseStatus || "").toLowerCase() === "ocr_pending";

      html += "<div class='result-section'>";
      html += "<div><strong>标题：</strong>" + UI.escapeHtml(item.title || "-") + "</div>";
      html += "<div><strong>类型：</strong>" + UI.escapeHtml(kindText) + "</div>";
      html += "<div><strong>文件名：</strong>" + UI.escapeHtml(item.originalFileName || "-") + "（" + UI.escapeHtml(formatFileSize(item.fileSizeBytes)) + "）</div>";
      html += "<div><strong>解析状态：</strong><span class='status-chip'>" + UI.escapeHtml(statusText) + "</span></div>";
      html += "<div><strong>分块数量：</strong>" + UI.escapeHtml(chunkCount) + "</div>";
      html += "<div><strong>上传时间：</strong>" + UI.escapeHtml(formatDateTime(item.uploadedAt)) + "</div>";
      html += "<div><strong>解析说明：</strong>" + UI.escapeHtml(parseMessage) + "</div>";
      if (showOcrHint) {
        html += "<div class='hint warning-hint'>该 PDF 可能是扫描版，后续需要 OCR 处理。</div>";
      }
      html += "</div>";
    });

    container.className = "";
    container.innerHTML = html;
  }

  function renderSearchResults(items) {
    const container = UI.qs("#materialSearchResultContainer");
    if (!container) return;

    const arr = Array.isArray(items) ? items : [];
    if (!arr.length) {
      container.className = "status";
      container.textContent = "未检索到相关资料片段。若资料为扫描版 PDF，需要先完成 OCR。";
      return;
    }

    let html = "";
    arr.forEach((item) => {
      const score = typeof item.score === "number" ? item.score.toFixed(3) : String(item.score || "0");
      const matched = Array.isArray(item.matchedKnowledgePoints) ? item.matchedKnowledgePoints : [];

      html += "<div class='result-section'>";
      html += "<div><strong>标题：</strong>" + UI.escapeHtml(item.title || "-") + "</div>";
      html += "<div><strong>类型：</strong>" + UI.escapeHtml(toDisplayKind(item.materialKind)) + "</div>";
      html += "<div><strong>小节：</strong>" + UI.escapeHtml(item.sectionTitle || "-") + "</div>";
      html += "<div><strong>路径：</strong>" + UI.escapeHtml(item.sectionPath || "-") + "</div>";
      html += "<div><strong>页码：</strong>" + UI.escapeHtml((item.pageStart ?? "-") + " - " + (item.pageEnd ?? "-")) + "</div>";
      html += "<div><strong>片段类型：</strong>" + UI.escapeHtml(toDisplayChunkType(item.chunkType)) + "</div>";
      html += "<div><strong>相关度：</strong><span class='status-chip'>" + UI.escapeHtml(score) + "</span></div>";
      html += "<div><strong>匹配知识点：</strong>" + (matched.length ? UI.renderList(matched) : "<span class='status'>暂无</span>") + "</div>";
      html += "<div><strong>片段预览：</strong></div>";
      html += "<div class='material-preview-box'>" + UI.escapeHtml(item.contentPreview || "") + "</div>";
      html += "</div>";
    });

    container.className = "";
    container.innerHTML = html;
  }

  async function loadCourseMaterials() {
    const statusEl = UI.qs("#materialsListStatus");
    const chapterSelect = UI.qs("#materialsListChapterSelect");
    const parseStatusSelect = UI.qs("#materialsListStatusSelect");
    const container = UI.qs("#materialsListContainer");

    if (!statusEl || !chapterSelect || !parseStatusSelect || !container) {
      return;
    }

    UI.showStatus(statusEl, "加载中...", false);

    const params = new URLSearchParams();
    params.set("courseId", String(AppConfig.defaultCourseId));
    params.set("take", "50");

    const chapterValue = chapterSelect.value;
    if (chapterValue) {
      params.set("chapterId", chapterValue);
    }

    const parseStatusValue = parseStatusSelect.value;
    if (parseStatusValue) {
      params.set("parseStatus", parseStatusValue);
    }

    try {
      const data = await Api.getJson("/api/course-materials?" + params.toString());
      renderList(data);
      UI.showStatus(statusEl, "已刷新。", false);
    } catch (_) {
      container.className = "status error";
      container.textContent = "资料列表加载失败，请稍后重试。";
      UI.showStatus(statusEl, "加载失败。", true);
    }
  }

  function renderMaterialResult(data) {
    const materialId = data && data.materialId != null ? data.materialId : "-";
    const title = data && data.title ? data.title : "-";
    const originalFileName = data && data.originalFileName ? data.originalFileName : "-";
    const parseStatus = data && data.parseStatus ? data.parseStatus : "-";
    const parseMessage = data && data.parseMessage ? data.parseMessage : "";
    const chunkCount = data && data.chunkCount != null ? data.chunkCount : 0;

    let html = "";
    html += "<div class='result-section'>";
    html += "<div><strong>资料编号：</strong>" + UI.escapeHtml(materialId) + "</div>";
    html += "<div><strong>资料标题：</strong>" + UI.escapeHtml(title) + "</div>";
    html += "<div><strong>原始文件：</strong>" + UI.escapeHtml(originalFileName) + "</div>";
    html += "<div><strong>解析状态：</strong><span class='status-chip'>" + UI.escapeHtml(toDisplayStatus(parseStatus)) + "</span></div>";
    html += "<div><strong>解析说明：</strong>" + UI.escapeHtml(parseMessage || "无") + "</div>";
    html += "<div><strong>分块数量：</strong>" + UI.escapeHtml(chunkCount) + "</div>";
    if (String(parseStatus).toLowerCase() === "ocr_pending") {
      html += "<div class='hint warning-hint'>该 PDF 可能是扫描版，后续需要 OCR 处理。</div>";
    }
    html += "</div>";
    return html;
  }

  async function uploadCourseMaterial() {
    const uploadBtn = UI.qs("#uploadMaterialBtn");
    const statusEl = UI.qs("#uploadMaterialStatus");
    const resultBox = UI.qs("#materialResultBox");

    if (!uploadBtn || !statusEl || !resultBox) return;

    const titleInput = UI.qs("#materialTitleInput");
    const chapterSelect = UI.qs("#materialChapterSelect");
    const kindSelect = UI.qs("#materialKindSelect");
    const fileInput = UI.qs("#materialPdfFile");

    const file = fileInput && fileInput.files && fileInput.files[0] ? fileInput.files[0] : null;
    if (!file) {
      UI.showStatus(statusEl, "请先选择 PDF 文件。", true);
      return;
    }

    const name = (file.name || "").toLowerCase();
    if (!name.endsWith(".pdf")) {
      UI.showStatus(statusEl, "当前只支持 PDF 文件上传。", true);
      return;
    }

    const chapterId = parseInt(chapterSelect.value, 10);
    const formData = new FormData();
    formData.append("courseId", String(AppConfig.defaultCourseId));
    if (!Number.isNaN(chapterId)) {
      formData.append("chapterId", String(chapterId));
    }

    const title = titleInput && titleInput.value ? titleInput.value.trim() : "";
    if (title) {
      formData.append("title", title);
    }

    const kind = kindSelect && kindSelect.value ? kindSelect.value : "textbook";
    formData.append("materialKind", kind);
    formData.append("visibility", "course_internal");
    formData.append("file", file);

    uploadBtn.disabled = true;
    UI.showStatus(statusEl, "正在上传并解析，请稍候……", false);

    try {
      const data = await Api.postFormData("/api/course-materials/upload", formData);
      resultBox.className = "";
      resultBox.innerHTML = renderMaterialResult(data || {});
      UI.showStatus(statusEl, "上传完成。", false);
      await loadCourseMaterials();
    } catch (err) {
      let message = "上传失败，请稍后重试。";
      if (err && err.status === 400) {
        message = "上传失败，请检查文件格式或填写内容。";
      }
      UI.showStatus(statusEl, message, true);
    } finally {
      uploadBtn.disabled = false;
    }
  }

  async function searchCourseMaterials() {
    const button = UI.qs("#materialSearchBtn");
    const statusEl = UI.qs("#materialSearchStatus");
    const resultContainer = UI.qs("#materialSearchResultContainer");
    const qInput = UI.qs("#materialSearchQInput");
    const chapterSelect = UI.qs("#materialSearchChapterSelect");
    const topKInput = UI.qs("#materialSearchTopKInput");
    const studentSolutionInput = UI.qs("#materialSearchStudentSolutionInput");

    if (!button || !statusEl || !resultContainer || !qInput || !chapterSelect || !topKInput || !studentSolutionInput) {
      return;
    }

    const q = (qInput.value || "").trim();
    if (!q) {
      UI.showStatus(statusEl, "请输入检索关键词或题目文本。", true);
      return;
    }

    let topK = parseInt(topKInput.value, 10);
    if (Number.isNaN(topK)) {
      topK = 3;
    }
    topK = Math.max(1, Math.min(8, topK));
    topKInput.value = String(topK);

    const params = new URLSearchParams();
    params.set("courseId", String(AppConfig.defaultCourseId));
    params.set("q", q);
    params.set("topK", String(topK));

    const chapterValue = chapterSelect.value;
    if (chapterValue) {
      params.set("chapterId", chapterValue);
    }

    const studentSolutionText = (studentSolutionInput.value || "").trim();
    if (studentSolutionText) {
      params.set("studentSolutionText", studentSolutionText);
    }

    button.disabled = true;
    UI.showStatus(statusEl, "检索中...", false);

    try {
      const data = await Api.getJson("/api/course-materials/search?" + params.toString());
      renderSearchResults(data);
      UI.showStatus(statusEl, "检索完成。", false);
    } catch (_) {
      resultContainer.className = "status error";
      resultContainer.textContent = "资料检索失败，请稍后重试。";
      UI.showStatus(statusEl, "检索失败。", true);
    } finally {
      button.disabled = false;
    }
  }

  function wireMaterialListFilters() {
    const chapterSelect = UI.qs("#materialsListChapterSelect");
    const parseStatusSelect = UI.qs("#materialsListStatusSelect");
    if (chapterSelect) {
      chapterSelect.addEventListener("change", function () {
        loadCourseMaterials();
      });
    }
    if (parseStatusSelect) {
      parseStatusSelect.addEventListener("change", function () {
        loadCourseMaterials();
      });
    }
  }

  function refreshCourseMaterials() {
    loadCourseMaterials();
  }

  function initMaterialsPage() {
    if (!UI.qs("#uploadMaterialBtn")) {
      return;
    }
    wireMaterialListFilters();
    loadCourseMaterials();
  }

  window.uploadCourseMaterial = uploadCourseMaterial;
  window.refreshCourseMaterials = refreshCourseMaterials;
  window.searchCourseMaterials = searchCourseMaterials;

  document.addEventListener("DOMContentLoaded", function () {
    initMaterialsPage();
  });
})();
