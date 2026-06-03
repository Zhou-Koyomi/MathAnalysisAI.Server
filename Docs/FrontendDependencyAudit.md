# 前端第三方依赖审计

> **R38-a** | 日期：2026-06-03 | 状态：审计完成（不替换依赖）

---

## 1. 审计范围

HTML 文件：`wwwroot/index.html`、`wwwroot/dev.html`、`wwwroot/login.html`、`wwwroot/materials.html`、`wwwroot/stats.html`

JS 文件：`wwwroot/js/*.js`

CSS 文件：`wwwroot/css/*.css`

---

## 2. CDN 依赖清单

### 2.1 来自 CDN 的依赖

| 资源 | CDN URL | 使用页面 | 有 SRI | 风险 |
|------|---------|---------|--------|------|
| MathJax 3 | `https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js` | `index.html:7`, `dev.html:7` | ❌ 无 | HIGH |

### 2.2 本地 vendor 清单

| 资源 | 路径 | 大小 | 使用页面 |
|------|------|------|---------|
| MathLive（主库） | `wwwroot/vendor/mathlive/mathlive.min.js` | 844 KB | `index.html`, `dev.html` |
| MathLive 样式 | `wwwroot/vendor/mathlive/mathlive-static.css` | 13 KB | `index.html`, `dev.html` |
| MathLive 字体样式 | `wwwroot/vendor/mathlive/mathlive-fonts.css` | 3 KB | `index.html`, `dev.html` |
| MathLive 字体目录 | `wwwroot/vendor/mathlive/fonts/` | ~1.3 MB total | 自动加载 |
| MathLive 音效目录 | `wwwroot/vendor/mathlive/sounds/` | 小 | 自动加载 |

---

## 3. 风险分析

### 3.1 MathJax CDN 依赖（当前唯一 CDN 依赖）

**风险**：
- jsdelivr CDN 被劫持 → 攻击者可注入恶意 JS
- 无 SRI hash → 浏览器无法验证文件完整性
- 国内网络问题 → jsdelivr 可能访问缓慢或不可用

**当前缓解**：
- MathJax 仅做 LaTeX 渲染展示，不访问 DOM 中的用户数据
- 即使 MathJax 加载失败，页面核心功能（OCR、分析、结果展示）仍可用
- 失败时仅公式渲染回退到原始 LaTeX 文本

**影响范围**：
- `index.html`：分析结果中的数学公式渲染
- `dev.html`：开发工具页中的公式渲染
- 不影响：OCR、analyze、登录、统计等核心功能

### 3.2 MathLive 本地 vendor（当前安全）

MathLive 已完全本地化（`wwwroot/vendor/mathlive/`），包含所有所需文件：
- 不依赖 CDN
- 不依赖外部字体服务
- 离线可用

**验证**：`DemoRunbook.md:133` 确认 "MathLive 默认从本地 `/vendor/mathlive/*` 加载，不依赖 CDN"。

### 3.3 其他前端资源

所有 JS 文件（`api.js`, `auth.js`, `analysis.js`, `nav.js` 等）均为本地文件。所有 CSS（`app.css`）均为本地文件。无其他 CDN 依赖。

---

## 4. SRI 检查结果

**当前所有 script/link 标签均无 `integrity` 属性。**

- MathJax CDN `<script>`：无 `integrity`
- MathLive 本地 `<script>`：本地文件不需要 SRI

---

## 5. 生产建议

### 5.1 短期（演示期间，不执行）

- 当前 MathJax CDN 风险**可接受**（仅影响公式渲染的视觉效果，不影响核心功能）
- 不建议在演示冻结期间替换

### 5.2 中期（演示后，R38-b）

推荐以下任一方案：

**方案 A：MathJax 本地 vendor（推荐）**

将 MathJax 下载到 `wwwroot/vendor/mathjax/` 并在 HTML 中引用本地文件：

```html
<!-- 替换 CDN 引用 -->
<script src="/vendor/mathjax/tex-mml-chtml.js"></script>
```

优点：
- 完全离线可用
- 消除 jsdelivr CDN 依赖
- 与 MathLive 保持一致的 vendor 策略

缺点：
- 增加仓库大小（MathJax 约 5-8MB，含字体和扩展）
- 需手动管理版本更新

**方案 B：CDN + SRI**

保留 CDN，但添加 SRI integrity hash：

```html
<script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"
        integrity="sha384-<hash>"
        crossorigin="anonymous"></script>
```

优点：
- 改动最小
- 利用 CDN 缓存加速

缺点：
- 仍依赖 jsdelivr 可用性（国内网络问题）
- SRI hash 需在版本升级时更新

### 5.3 推荐结论

**推荐方案 A（本地 vendor）**，与当前 MathLive 的 vendor 策略一致：
- MathLive 已走本地 vendor，MathJax 应保持一致
- 消除所有 CDN 依赖，实现完全离线可用
- 中国国内网络环境下 CDN 可用性不可靠

---

## 6. 本地 Vendor 清单汇总

| 资源 | 本地 vendor | CDN | SRI | 风险等级 |
|------|-----------|-----|-----|---------|
| MathLive（公式编辑器） | ✅ `/vendor/mathlive/` | ❌ | N/A | 🟢 安全 |
| MathJax 3（公式渲染） | ❌ | ✅ jsdelivr | ❌ | 🔴 HIGH |
| 所有 JS 业务逻辑 | ✅ `/js/*.js` | ❌ | N/A | 🟢 安全 |
| 应用样式 | ✅ `/css/app.css` | ❌ | N/A | 🟢 安全 |

---

## 7. 后续实施拆分

| 阶段 | 内容 | 时机 |
|------|------|------|
| R38-a | 本文档（审计） | ✅ 当前 |
| R38-b | MathJax 本地 vendor（下载 + HTML 引用修改） | 演示冻结解除后 |
| R38-c | 移除 CDN 引用，验证公式渲染无损 | R38-b 完成后 |
| R38-d | `.dockerignore` 确认 vendor 目录不被排除 | R38-b 完成后 |

---

## 8. 决策结论

- ✅ MathLive 已本地 vendor（安全）
- ❌ MathJax 仍走 CDN，无 SRI（**当前唯一 CDN 高风险依赖**）
- ✅ 核心功能不依赖 MathJax（仅公式渲染视觉效果）
- ⚠️ 演示期间不替换（符合冻结规则）
- ✅ 演示后建议 MathJax 本地 vendor（方案 A）
- ⚠️ 当前不下载、不替换、不写 SRI hash
