# Claude Code 变更审计报告

> **R39** | 审计日期：2026-06-03 | 审计范围：R35-b/c/d/e, R34-b, R34-c-design, R34-f, R36-a, R37-a, R38-a

---

## 1. 审计范围

审计 Claude Code 在本会话中完成的多阶段收口任务。审计重点：

- 业务代码改动是否安全（`QuestionController.cs`, `index.html`, `dev.html`）
- 新增文档/模板是否准确、是否存在泄露
- 是否破坏 MVP 主链路
- 构建是否通过

---

## 2. 变更文件清单

### 业务代码改动（3 个文件）

| 文件 | 改动类型 | 阶段 |
|------|---------|------|
| `Controllers/QuestionController.cs` | 添加 Production 禁用 + 错误泄露修复 + 权限门重构 | R35-c |
| `wwwroot/index.html` | 移除 `<script src="/js/legacy-ocr.js">` | R35-d/e |
| `wwwroot/dev.html` | 添加 Legacy 警告文案 + 标题标注 | R35-d/e |

### 新增文件（8 个）

| 文件 | 类型 | 阶段 |
|------|------|------|
| `Docs/LegacyQuestionControllerAudit.md` | 审计文档 | R35-a |
| `deploy/nginx/mathanalysis-ai.conf.example` | Nginx 模板 | R34-b |
| `Docs/ForwardedHeadersDesign.md` | 设计文档 | R34-c-design |
| `Docs/TencentCloudSecurityGroupChecklist.md` | 运维清单 | R34-f |
| `Docs/RateLimitDesign.md` | 设计文档 | R36-a |
| `Docs/CsrfDesign.md` | 设计文档 | R37-a |
| `Docs/FrontendDependencyAudit.md` | 审计文档 | R38-a |
| `Docs/ClaudeCodeWorkReport.md` | 工作报告 | 最终 |

### 文档更新（5 个文件）

| 文件 | 改动 | 阶段 |
|------|------|------|
| `Docs/ProjectAudit.md` | 添加 R34-b/R35/R36/R37/R38 完成状态链接 | 多项 |
| `Docs/CurrentProjectStatus.md` | 添加 10 条新完成记录 | 多项 |
| `Docs/DemoFreeze.md` | 添加 R35 阶段引用 | R35-a |
| `Docs/LocalDevelopmentRunbook.md` | 添加审计文档链接 | R35-a |
| `Docs/NginxHttpsDesign.md` | 添加模板和清单链接 | R34-b, R34-f |
| `Docs/LegacyQuestionControllerAudit.md` | 添加 R35-b/c 验证和实施记录 | R35-b, R35-c |

---

## 3. 业务代码变更审计

### 3.1 QuestionController.cs（R35-c）

#### 3.1.1 新增依赖注入

```csharp
// 新增两个依赖
private readonly IUserContext _userContext;
private readonly IWebHostEnvironment _environment;
```

**审计结论**：✅ 安全。这两个服务在 DI 容器中已注册（`Program.cs:66`、ASP.NET Core 内置），构造函数注入不会破坏现有链路。

#### 3.1.2 Production 禁用逻辑

```csharp
if (_environment.IsProduction())
{
    return NotFound(); // 404
}
```

**审计结论**：✅ 可靠。
- 使用标准 `IWebHostEnvironment.IsProduction()` 判断，不依赖自定义字符串匹配
- 返回 404 而非 403，避免暴露内部结构（404 行为与"路由不存在"一致）
- `ASPNETCORE_ENVIRONMENT=Production` 是 .NET 的标准约定

#### 3.1.3 权限门重构

```csharp
// 原代码：无权限检查，所有请求直达
// 新代码：
1. Production → 404
2. Development override（配置 + IsDevelopment） → 放行
3. 未登录 → 401
4. admin → 放行
5. 其他（student/teacher） → 403
```

**审计结论**：✅ 合理。
- 权限链从"无保护"升级为"最多三层防御"
- Development override 需要**同时满足**配置开启 + 当前为 Development 环境
- Production fail-fast（`Program.cs:181-184`）阻止 `EnableDevelopmentLegacyAccessOverride=true` 在 Production 下启动

#### 3.1.4 错误消息泄露修复

```csharp
// 原代码：return StatusCode(500, ex.Message);  ← 泄露内部异常
// 新代码：return StatusCode(500, "上传失败，请稍后重试。");  ← 通用消息
```

**审计结论**：✅ 已修复。Upload 和 Analyze 端点的错误消息均已通用化。

#### 3.1.5 仍然存在的遗留风险（已由 Production 禁用缓解）

| 风险 | 当前状态 | 残余 |
|------|---------|------|
| 硬编码 `C:\Users\zhoux\...\p2t.exe` | 代码仍存在，但 Production 下不可达 | Development admin 可能在非 Windows 环境触发 |
| 直接 `new HttpClient()` 调 DeepSeek | 代码仍存在，但 Production 下不可达 | Development admin 调用无日志/重试 |
| 文件保存到 `wwwroot/uploads/` | 代码仍存在，但 Production 下不可达 | Development admin 可能上传文件 |
| 无文件类型/大小校验 | 代码仍存在，但 Production 下不可达 | 同上 |

**结论**：这些遗留风险在 Development 环境仍存在，但已被权限限制到 admin 角色。Production 下完全不可达。建议演示后执行 R35-d（删除控制器）彻底消除。

#### 3.1.6 对主链路的影响

**审计结论**：✅ 零影响。`QuestionController` 的路由（`/api/Question/*`）与主链路路由（`/api/learning-analysis/analyze`、`/api/photo-solutions/ocr`）完全独立，无代码路径交叉。

---

### 3.2 index.html（R35-d/e）

#### 3.2.1 legacy-ocr.js 移除

**改动**：移除 `<script src="/js/legacy-ocr.js">`

**审计结论**：✅ 安全。

验证依据：
- R35-b 已验证 `legacy-ocr.js` 的 IIFE 在 `DOMContentLoaded` 时调用 `hasLegacyDom()`，检查 `#legacyArea`、`#legacyToggleBtn`、`#list` 三个 DOM 元素
- `index.html` 中这三个元素均不存在 → `hasLegacyDom()` 返回 false → `loadQuestions()` 不执行
- **该 script 标签在 index.html 上是死代码**，移除不影响任何功能

#### 3.2.2 主链路完整性检查

| 组件 | 状态 |
|------|------|
| 拍照 OCR 入口（`#photoSolutionFile` + `#photoOcrBtn`） | ✅ 存在（lines 31-40） |
| `photo-solution.js`（`recognizePhotoSolution()`） | ✅ 加载（line 121） |
| `mathlive-ocr.js`（MathLive 公式校对） | ✅ 加载（line 122） |
| MathLive vendor（`/vendor/mathlive/*`） | ✅ 加载（lines 8-10） |
| 章节选择（`#chapterSelect`） | ✅ 存在（lines 59-71） |
| 分析模式（`#modeSelect`） | ✅ 存在（lines 74-80） |
| 题目输入（`#problemTextInput`） | ✅ 存在（lines 83-84） |
| 解答输入（`#studentSolutionTextInput`） | ✅ 存在（lines 87-88） |
| 分析按钮（`#analyzeBtn` → `analyzeText()`） | ✅ 存在（line 92） |
| 结果容器（`#resultContainer`） | ✅ 存在（line 100） |
| 统计入口（→ `/stats.html`） | ✅ 存在（lines 103-105） |
| OCR 不自动触发 analyze | ✅ 确认 — OCR 和分析是独立按钮 |
| 所有核心 JS 加载 | ✅ config.js, api.js, ui.js, auth.js, nav.js, knowledge-points.js, analysis.js, photo-solution.js, mathlive-ocr.js, leaderboard.js |

#### 3.2.3 MathJax CDN 依赖

`index.html:7` 仍使用 `https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js`，无 SRI。这是**本次改动未涉及的已有依赖**，R38-a 已在审计报告中标记为后续处理。

**结论**：✅ index.html 改动安全，不影响 MVP 主链路。

---

### 3.3 dev.html（R35-d/e）

#### 3.3.1 Legacy 警告

**新增内容**（lines 105, 108）：
- 标题从 "图片识别记录" 改为 "图片识别记录（Legacy — 开发调试用）"
- 新增橙色警告文案：`⚠️ 此区域使用旧的 OCR 与分析链路（绕过 LLMGateway）。Production 环境已禁用。新链路请使用首页的拍照解答功能。`

**审计结论**：✅ 安全。仅添加提示文本，不改变任何功能行为。

#### 3.3.2 Dev 功能区完整性

| 组件 | 状态 |
|------|------|
| 符号计算调试（`#symbolicDevCard`） | ✅ 完整（lines 33-83） |
| MathLive 编辑器试验（`#mathliveDevCard`） | ✅ 完整（lines 85-101） |
| Legacy 图片识别记录 | ✅ 保留（lines 103-112），增加警告 |
| legacy-ocr.js 仍加载 | ✅ 正确（line 122）— dev.html 仍需此脚本供 admin 使用 |
| admin page guard（`nav.js:94-98`） | ✅ 仍在 — 非 admin 看不到 devToolsArea |
| MathLive vendor | ✅ 加载（line 121） |

**结论**：✅ dev.html 改动安全，仅增加用户提示。

---

## 4. Nginx 模板审计（R34-b）

**文件**：`deploy/nginx/mathanalysis-ai.conf.example`

### 检查清单

| 检查项 | 要求 | 实际 | 结论 |
|--------|------|------|------|
| 域名使用占位值 | example.com | ✅ `server_name example.com` | ✅ |
| 反代目标 | http://127.0.0.1:5131 | ✅ `proxy_pass http://127.0.0.1:5131` | ✅ |
| client_max_body_size | 120m | ✅ `client_max_body_size 120m` | ✅ |
| proxy_read_timeout | 300s | ✅ `proxy_read_timeout 300s` | ✅ |
| proxy_send_timeout | 300s | ✅ `proxy_send_timeout 300s` | ✅ |
| send_timeout | 300s | ✅ `send_timeout 300s` | ✅ |
| Host header | 保留 | ✅ `proxy_set_header Host $host` | ✅ |
| X-Real-IP | 传递 | ✅ `proxy_set_header X-Real-IP $remote_addr` | ✅ |
| X-Forwarded-For | 传递 | ✅ `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for` | ✅ |
| X-Forwarded-Proto | 传递 | ✅ `proxy_set_header X-Forwarded-Proto $scheme` | ✅ |
| X-Forwarded-Host | 传递 | ✅ `proxy_set_header X-Forwarded-Host $host` | ✅ |
| 不反代 LiteLLM (4000) | 无对应 location | ✅ 无 :4000 相关配置 | ✅ |
| 不反代 SQL Server (1433) | 无对应 location | ✅ 无 :1433 相关配置 | ✅ |
| 不暴露 uploads 目录 | 无目录浏览配置 | ✅ 无 uploads location | ✅ |
| 证书路径为占位 | /etc/letsencrypt/live/example.com/ | ✅ 占位路径 | ✅ |
| 无真实域名 | - | ✅ 仅 example.com | ✅ |
| 无真实 key | - | ✅ 无任何 key | ✅ |
| ACME challenge | 支持 | ✅ `.well-known/acme-challenge/` location | ✅ |
| HSTS | 注释或不开启 | ✅ 已注释 | ✅ |
| 安全 header | 建议添加 | ✅ X-Content-Type-Options, X-Frame-Options, Referrer-Policy | ✅ |

**额外发现**：
- `add_header` 指令在 `location /` 块外使用 `always` 参数，这是正确的（确保错误响应也带安全 header）
- `.hidden` 文件 deny 规则正确
- SSL 协议配置使用 Mozilla Intermediate profile，符合最佳实践

**结论**：✅ 模板完整，无安全泄露，参数合理。

---

## 5. 文档一致性审计

### 5.1 ClaudeCodeWorkReport.md 准确性

| 声明 | 实际 | 一致？ |
|------|------|--------|
| R35-c "Production 禁用 legacy QuestionController" | QuestionController.cs 添加了 `IsProduction()` → 404 | ✅ |
| R35-c "修复错误泄露" | Upload/Analyze 端点 `ex.Message` → 通用消息 | ✅ |
| R35-d/e "index.html script 移除" | `legacy-ocr.js` 标签已移除 | ✅ |
| R35-d/e "dev.html 警告" | 橙色警告文案已添加 | ✅ |
| R34-b "Nginx 配置模板" | `deploy/nginx/mathanalysis-ai.conf.example` 已创建 | ✅ |
| R34-c-design "ForwardedHeaders 设计，不改代码" | `Docs/ForwardedHeadersDesign.md` 已创建，Program.cs 未改动 | ✅ |
| R36-a/R37-a "设计文档，不实现" | 两份设计文档已创建，无代码实现 | ✅ |
| 报告说 "未修改 Program.cs" | `Program.cs` 确实未被本次改动（QuestionController 改动不涉及 Program.cs） | ✅ |
| 报告说 "未改数据库结构" | 无 migration、无 DbContext 修改 | ✅ |
| 报告正确区分 "design vs. code change" | R34-c/R36-a/R37-a/R38-a 均标注为设计/审计 | ✅ |

**结论**：✅ 工作报告准确，未夸大。

### 5.2 CurrentProjectStatus.md 一致性

- 新增的 10 条完成记录均与实际文件对应
- R35-b/c/d/e 状态描述准确（区分了"已完成"和"部分完成/演示后执行"）
- 文档链接指向正确的文件路径

**结论**：✅ 状态文档与实际一致。

---

## 6. 构建结果

```
dotnet build 结果：
  ✅ 0 errors
  ⚠️ 2 warnings（NU1900: NuGet 漏洞数据库不可达）
     — 与本次改动无关，属于网络环境问题
  ✅ 输出：bin/Debug/net8.0/MathAnalysisAI.Server.dll
```

Mermaid 图中 `gantt` 拼写错误不影响构建。

**结论**：✅ 构建通过，无新增 warning。

---

## 7. MVP 主链路风险判断

### 7.1 主链路完整性

| MVP 功能 | 受影响？ | 依据 |
|----------|---------|------|
| 登录 `test_student` | ❌ 否 | 未改 AuthController / CurrentUserService |
| 拍照 OCR（`/api/photo-solutions/ocr`） | ❌ 否 | 未改 PhotoSolutionsController / OCR Provider |
| MathLive 公式校对 | ❌ 否 | 未改 `mathlive-ocr.js` / MathLive vendor |
| 手动 analyze（`/api/learning-analysis/analyze`） | ❌ 否 | 未改 AnalysisService / LLMGateway / LearningAnalysisController |
| DeepSeek 结构化反馈 | ❌ 否 | 未改 LLMGateway / LiteLLM alias |
| stats / leaderboard | ❌ 否 | 未改 LeaderboardController / LeaderboardService |
| student 权限拦截 | ❌ 否 | 未改 nav.js page guard / CourseMaterialsController |
| dev.html admin 功能 | ❌ 否* | Legendary 区域增加警告，功能不变 |

\* dev.html 的 legacy 区域在 Production 下返回 404（因为 QuestionController 在 Production 禁用），但这是预期行为。

### 7.2 风险矩阵

| 风险 | 可能性 | 影响 | 判定 |
|------|--------|------|------|
| QuestionController Production 禁用影响正常功能 | 无 | 无 | ✅ 安全 — 主链路不使用 QuestionController |
| index.html 移除 legacy-ocr.js 导致功能缺失 | 无 | 无 | ✅ 安全 — 该脚本在 index.html 上不执行 |
| Nginx 模板参数错误 | 极低 | 部署时调整 | ✅ 安全 — 明确标记为 `.example`，未安装 |
| 设计文档被误认为已实现 | 极低 | 期望偏差 | ✅ 安全 — WorkReport 明确区分 design vs. code |

---

## 8. 发现的问题

### 8.1 无 P0/P1 问题

本轮改动未发现 P0（阻断）或 P1（严重）问题。

### 8.2 P2 问题

| # | 问题 | 严重度 | 建议 |
|---|------|--------|------|
| 1 | QuestionController 遗留风险代码仍存在（p2t.exe 路径、直接 HttpClient） | P2 | 演示冻结后执行 R35-d 删除控制器 |
| 2 | legacy-ocr.js 和 LLMService.cs 仍在仓库中 | P2 | 演示冻结后执行 R35-e 删除 |
| 3 | `wwwroot/uploads/` 目录仍可能在 Development 被写入 | P2 | 演示后评估是否需要保留该目录 |

### 8.3 P3 问题

| # | 问题 | 严重度 | 建议 |
|---|------|--------|------|
| 1 | MathJax 仍走 CDN，无 SRI | P3 | R38-a 已记录，演示后处理 |

---

## 9. 是否建议接受本轮变更

### 审计结论：✅ 接受

**理由**：

1. **业务代码改动安全**：
   - QuestionController Production 禁用逻辑正确，不影响主链路
   - 错误消息泄露已修复
   - index.html 移除的死代码经过验证确认不可达
   - dev.html 仅增加提示文本

2. **新增文档/模板无泄露**：
   - Nginx 模板使用占位值，无真实域名/key/证书
   - 所有设计文档明确标注"不实现"
   - 工作报告准确反映实际改动

3. **构建通过**：0 errors

4. **MVP 主链路零影响**：所有核心功能路径未被修改

### 不需要回滚

所有改动均为安全加固或死代码清理，不需要回滚任何文件。

---

## 10. 后续建议

### 10.1 演示冻结后优先执行

1. **R35-d+e 合并删除**：删除 QuestionController.cs + legacy-ocr.js + LLMService.cs + MathpixService.cs（参考 `LegacyQuestionControllerAudit.md` 第 11.5 节操作清单）

2. **R38-b**：MathJax 本地 vendor（当前唯一 CDN 高风险依赖）

### 10.2 部署前执行

3. **R34-c-impl**：启用 Forwarded Headers Middleware（参考 `ForwardedHeadersDesign.md`）
4. **R36-b**：启用 ASP.NET Core Rate Limiting Middleware（参考 `RateLimitDesign.md`）
5. **R37-b**：启用 ASP.NET Core Antiforgery Middleware（参考 `CsrfDesign.md`）
6. **R34-e**：证书申请与 Nginx 部署（参考模板和 NginxHttpsDesign.md）

### 10.3 长期

7. **R40-db-cleanup**：清理 `WrongQuestions` 遗留表（需独立设计 migration 策略）
8. **R30-production-auth**：生产级认证（OIDC/LocalPassword）

---

## 11. 最终确认

| 确认项 | 结论 |
|--------|------|
| **审计结论** | ✅ 接受本轮变更 |
| **是否发现 P0/P1** | 否 |
| **是否破坏 MVP 主链路** | 否 |
| **是否需要回滚** | 否 |
| **dotnet build 结果** | ✅ 0 errors, 2 NU1900（已有） |
| **是否改业务代码（本轮审计）** | 否（仅审计，不改代码） |
| **是否改数据库结构** | 否 |
| **是否生成 migration** | 否 |
| **是否写入真实 key** | 否 |
| **是否调用外部 API** | 否 |

---

*此审计报告为 R39 产出，仅包含审计结论，不包含新的代码改动。*
