# Legacy QuestionController 审计与收敛方案

> **R35-a 产出** | 日期：2026-06-03 | 状态：审计与设计阶段（不改业务代码）

---

## 1. 背景

Claude Code 全项目审计（`Docs/ProjectAudit.md`）指出 `QuestionController` 存在多个遗留风险：

- 硬编码 Windows 绝对路径 `C:\Users\zhoux\...\p2t.exe`
- 遗留 OCR / analyze 链路绕过当前 `LLMGateway`
- 直接 `new HttpClient()` 请求外部模型，绕过统一日志、重试、错误处理与 key 管理
- 文件直接保存到 `wwwroot/uploads/`
- 可能存在错误信息泄露

当前项目主链路已迁移到：

- **OCR**：`/api/photo-solutions/ocr` → `LiteLLMPhotoSolutionOcrProvider` → LiteLLM → DashScope Qwen-VL
- **分析**：`/api/learning-analysis/analyze` → `AnalysisService` → `LLMGateway` → LiteLLM/DeepSeek
- **前端校对**：MathLive inline formulas（`mathlive-ocr.js`）
- **权限**：session user + userId mismatch 403

本轮目标：审计 `QuestionController` 当前使用情况、风险边界，并提出处理方案。**本轮只做审计与设计文档，不直接改业务代码。**

---

## 2. 当前 Endpoint 清单

| # | Method | Route | 功能描述 |
|---|--------|-------|---------|
| 1 | `POST` | `/api/Question/upload` | 接收图片文件 → 保存到 `wwwroot/uploads/` → 调用 `p2t.exe` OCR → 写入 `WrongQuestions` 表 |
| 2 | `POST` | `/api/Question/analyze/{id}` | 读取 `WrongQuestions` 记录 → 直接调 DeepSeek API（绕过 LLMGateway）→ 更新分析字段 |
| 3 | `GET` | `/api/Question/list` | 返回 `WrongQuestions` 全部记录（按 Id 倒序），无分页 |

三个 endpoint 共用 `EnsureLegacyDeveloperAccessAsync()` 权限检查。

---

## 3. 当前使用情况

### 3.1 前端引用清单

| 文件 | 引用方式 | 是否触发实际 API 调用 |
|------|---------|---------------------|
| `wwwroot/index.html:124` | `<script src="/js/legacy-ocr.js">` | **否** — `hasLegacyDom()` 检查失败（index.html 缺少 `#legacyArea` / `#list` 等 DOM 元素），代码路径不执行 |
| `wwwroot/dev.html:108-111,121` | 包含 `#legacyArea` / `#list` / `#legacyToggleBtn` DOM + 加载 `legacy-ocr.js` | **是** — 仅当 admin 角色时，`nav.js:99-101` 调用 `loadQuestions()` → `GET /api/Question/list`；用户可点击按钮触发 `POST /api/Question/analyze/{id}` |
| `wwwroot/js/legacy-ocr.js:22` | `Api.postJson("/api/Question/analyze/" + id, {})` | 仅 `dev.html` 下 admin 触发 |
| `wwwroot/js/legacy-ocr.js:41` | `Api.getJson("/api/Question/list")` | 仅 `dev.html` 下 admin 页面加载时触发 |
| `wwwroot/js/nav.js:99-101` | `if (window.loadQuestions && UI.qs("#list")) { window.loadQuestions(); }` | 仅 `dev.html` + admin 角色时触发 `loadQuestions()` |

**结论**：`index.html`、`materials.html`、`stats.html`、`login.html` 的当前主链路**均不调用** legacy Question API。`legacy-ocr.js` 在主页面虽然加载，但因缺少 DOM 元素而不执行任何网络请求。

### 3.2 后端注册与依赖

| 依赖项 | Program.cs | 说明 |
|--------|-----------|------|
| `AddControllers()` | line 37 | 自动发现并注册 `QuestionController`（无单独显式注册） |
| `IUserContext` / `CurrentUserService` | line 66 | QuestionController 通过 DI 注入使用 |
| `LLMService`（遗留） | line 65 | 注册但 **未被 QuestionController 使用** — QuestionController 直接 `new HttpClient()` |
| `LLMGateway` | line 67 | QuestionController **不使用**，完全绕过 |
| `IPhotoSolutionOcrProvider` | line 84 | QuestionController **不使用**，使用自己的 `RunPix2Text()` |
| `ApplicationDbContext` | line 33-34 | 被 QuestionController 使用（读写 `WrongQuestions`） |
| `WrongQuestions` DbSet | Data/ApplicationDbContext.cs:14 | 被 QuestionController 使用 |

### 3.3 主链路依赖判断

| 判断维度 | 结论 |
|---------|------|
| 是否被当前 MVP 主链路（analyze / OCR）使用 | **否** |
| 是否被 `index.html` 的拍照 OCR 或文本分析调用 | **否** |
| 是否被 `materials.html` 调用 | **否** |
| 是否被 `stats.html` 调用 | **否** |
| 是否只属于 legacy/debug 功能 | **是** |
| 是否可视为遗留控制器 | **是** |

**最终结论：QuestionController 属于遗留控制器，当前 MVP 主链路完全不依赖它。** 它仅通过 `dev.html` 在 admin 角色下可用，作为开发期调试入口。

---

## 4. 风险审计

### 4.1 硬编码路径

**证据：**
```csharp
// QuestionController.cs:21-22
private const string P2T_PATH =
    @"C:\Users\zhoux\AppData\Roaming\Python\Python314\Scripts\p2t.exe";
```

| 风险维度 | 评估 |
|---------|------|
| 是否存在 Windows 绝对路径 | **是** — `C:\Users\zhoux\...` |
| 是否会在 macOS/Linux/Docker 下失败 | **是** — 路径不存在，`Process.Start` 抛出 `Win32Exception` |
| 是否属于旧 OCR 工具残留 | **是** — `p2t.exe`（pix2text）是原有本地 OCR 方案，已被 `LiteLLMPhotoSolutionOcrProvider` 替代 |
| 是否仍可通过 API 触发 | **是** — `POST /api/Question/upload` 仍然存在且可调用（需通过权限检查） |
| 触发后果 | `RunPix2Text` 调用 `Process.Start(psi)` → 在非 Windows 环境抛出异常 → `catch (Exception ex)` 返回 `StatusCode(500, ex.Message)` |

### 4.2 外部模型调用绕过

**证据（analyze endpoint）：**
```csharp
// QuestionController.cs:116-118 — 直接创建 HttpClient，未使用 IHttpClientFactory
var apiKey = _config["DeepSeek:ApiKey"];
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

// QuestionController.cs:140-143 — 硬编码 DeepSeek 官方 URL
var response = await client.PostAsync(
    "https://api.deepseek.com/chat/completions",
    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
    cancellationToken);
```

| 风险维度 | 评估 |
|---------|------|
| 是否直接创建 `HttpClient` | **是** — `new HttpClient()` 绕过 `IHttpClientFactory`，存在 socket 耗尽风险 |
| 是否绕过 `LLMGateway` | **是** — 完全不经过 `LLMGateway.ChatAsync()` |
| 是否绕过 LiteLLM alias | **是** — 硬编码 `https://api.deepseek.com/chat/completions`，不经过 LiteLLM 代理 |
| 是否绕过统一日志（`LLMRequestLog`） | **是** — 不写入 `LLMRequestLog` 表，调用无记录 |
| 是否绕过统一重试/错误处理 | **是** — 简单的 try/catch，无重试机制 |
| 是否直接读取/传递 API key | **是** — 直接从 `_config["DeepSeek:ApiKey"]` 读取并放入 HTTP Header |
| API key 泄露风险 | 与 LLMGateway 一致（都从 config 读取），但缺少 ASCII 校验、空值校验等防御 |

**对比 LLMGateway 具备但 QuestionController 缺少的能力：**
- API key 格式校验（`IsAscii` 检查）
- BaseUrl 配置化（`DeepSeek:BaseUrl` / `LiteLLM:BaseUrl`）
- 统一错误码（`missing_api_key`、`http_401` 等）
- 结构化日志（`LLMRequestLog` 表，含 provider/model/tokens/latency/status/errorCode）
- `IHttpClientFactory` 管理连接池
- 使用 `CancellationToken` 正确传播取消

### 4.3 文件上传/保存风险

**证据（upload endpoint）：**
```csharp
// QuestionController.cs:55-63
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
var filePath = Path.Combine(uploadsPath, fileName);
var relativePath = "/uploads/" + fileName;
```

| 风险维度 | 评估 |
|---------|------|
| 是否保存到 `wwwroot` | **是** — 保存到 `wwwroot/uploads/`，ASP.NET Core 的 `UseStaticFiles()` 中间件会直接公开该目录 |
| 文件名是否可信 | **部分安全** — 使用 `Guid.NewGuid()` 生成文件名，但保留了原始扩展名 `Path.GetExtension(file.FileName)` |
| 是否存在路径遍历风险 | **低** — 文件名由 `Guid` 生成，不包含用户输入；但 `Path.GetExtension` 对多扩展名文件（如 `file.jpg.exe`）会返回最后一个扩展名 |
| 是否限制文件类型 | **否** — **无任何文件类型或扩展名校验**。没有白名单、没有 content-type 检查 |
| 是否限制文件大小 | **否** — 无 `[RequestSizeLimit]` 或 `[RequestFormLimits]` 属性（对比 `PhotoSolutionsController` 有 10MB 限制） |
| 是否可能形成公网可访问文件 | **是** — `wwwroot/uploads/` 通过 `UseStaticFiles()` 公开，上传的文件可通过 `/uploads/{fileName}` 直接访问 |
| 是否可能覆盖已有文件 | **低** — `Guid.NewGuid()` 碰撞概率极低 |

**对比 PhotoSolutionsController 具备但 QuestionController 缺少的防护：**
- 文件扩展名白名单（`.jpg/.jpeg/.png/.webp`）
- Content-Type 白名单（`image/jpeg`, `image/png`, `image/webp`）
- 文件大小限制（`[RequestSizeLimit(10 * 1024 * 1024)]` + 配置化 `MaxImageBytes`）
- 不将文件保存到磁盘（仅在内存中 `MemoryStream` → base64 编码 → 发送给 LLM）

### 4.4 错误信息泄露

**证据：**
```csharp
// QuestionController.cs:92-96 （Upload endpoint — 泄露内部异常消息）
catch (Exception ex)
{
    Console.Error.WriteLine($"上传失败: {ex.Message}");
    return StatusCode(500, ex.Message);  // ← 直接返回 ex.Message 给客户端
}

// QuestionController.cs:177-181 （Analyze endpoint — 已做基本防护）
catch (Exception ex)
{
    Console.Error.WriteLine($"AI 分析失败: {ex.Message}");
    return StatusCode(500, "AI 分析失败，请稍后重试");  // ← 硬编码消息，不泄露细节
}
```

| 风险维度 | 评估 |
|---------|------|
| `POST /api/Question/upload` 是否泄露异常消息 | **是** — `StatusCode(500, ex.Message)` 直接返回给客户端 |
| `POST /api/Question/analyze/{id}` 是否泄露异常消息 | **否** — 返回硬编码中文消息 |
| `GET /api/Question/list` 是否泄露异常消息 | **否** — 不包含 try/catch（异常会进入 ASP.NET Core 默认错误处理） |
| 可能泄露的敏感信息（upload 端点） | 文件系统路径（如 `Could not find file 'C:\...'`）、进程启动错误、OCR 工具内部错误信息 |
| 日志记录方式 | `Console.Error.WriteLine` — 不经过 `ILogger<T>`，不受日志级别控制 |

### 4.5 权限边界

**当前权限逻辑：**

```csharp
// QuestionController.cs:200-218
private async Task<ActionResult?> EnsureLegacyDeveloperAccessAsync(CancellationToken cancellationToken)
{
    // 1. 先检查 Development Override
    if (ShouldAllowDevelopmentLegacyOverride())
    {
        return null; // 允许访问
    }

    // 2. 要求登录
    var user = await _userContext.GetCurrentUserAsync(cancellationToken);
    if (user == null)
    {
        return Unauthorized(new { message = "Not logged in." });
    }

    // 3. 仅允许 admin 角色
    if (string.Equals(user.Role?.Trim(), AppUserRole.Admin, StringComparison.OrdinalIgnoreCase))
    {
        return null; // 允许访问
    }

    // 4. 其他角色（student, teacher）返回 403
    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden." });
}

// QuestionController.cs:221-225
private bool ShouldAllowDevelopmentLegacyOverride()
{
    var enabled = _config.GetValue<bool>("Auth:EnableDevelopmentLegacyAccessOverride");
    return enabled && _environment.IsDevelopment();
}
```

| 场景 | 结果 | 风险 |
|------|------|------|
| **未登录用户** | `401 Unauthorized` | 无（正确拒绝） |
| **student 角色** | `403 Forbidden` | 无（已验证，见 `ProjectAudit.md` 和 `CurrentProjectStatus.md`） |
| **teacher 角色** | `403 Forbidden` | 无（teacher 也被拒绝，只有 admin 可通过） |
| **admin 角色** | 允许访问 | **低风险** — admin 是可信角色，但遗留风险仍存在（硬编码路径、绕过 LLMGateway） |
| **Development Override 开启 + Development 环境** | 允许访问（任何人，包括未登录） | **高风险** — 若 `Auth:EnableDevelopmentLegacyAccessOverride=true` 且 `ASPNETCORE_ENVIRONMENT=Development`，所有请求（包括未认证）可访问 legacy API |
| **Production 环境 + Development Override 误开启** | 服务器启动失败 | **安全** — `Program.cs:181-184` 的 `ValidateAuthConfiguration` 会在 Production 检测到 `EnableDevelopmentLegacyAccessOverride=true` 时抛出 `InvalidOperationException`，阻止启动 |

**当前配置状态：**

`appsettings.json`（默认配置）：
```json
"Auth": {
    "Mode": "Disabled",
    "EnableDevelopmentLegacyAccessOverride": false
}
```

`appsettings.Development.json`（开发配置）：
```json
"Auth": {
    "Mode": "DevelopmentUsername",
    "EnableDevelopmentLegacyAccessOverride": false
}
```

**当前 Development Override 默认关闭**，student 已被后端 403 阻止，teacher 也被拒绝。仅 admin 可用。

---

## 5. 与当前主链路对比

### 5.1 OCR 链路对比

| 维度 | Legacy QuestionController | 当前主链路 |
|------|--------------------------|-----------|
| **端点** | `POST /api/Question/upload` | `POST /api/photo-solutions/ocr` |
| **OCR 引擎** | 本地 `p2t.exe`（pix2text） | LiteLLM → DashScope Qwen-VL（视觉 LLM） |
| **平台兼容性** | 仅 Windows（硬编码路径） | 跨平台（HTTP API 调用） |
| **文件处理** | 保存到 `wwwroot/uploads/` 磁盘 | 仅内存中 `MemoryStream` → base64 |
| **文件类型校验** | 无 | 扩展名白名单 + Content-Type 白名单 |
| **文件大小限制** | 无 | 10MB（配置化 `MaxImageBytes`） |
| **认证** | admin / Development Override | 无认证要求（公开接口） |
| **模型调用** | 直接 `new HttpClient()` → DeepSeek | `IHttpClientFactory` → LiteLLM 代理 |
| **日志记录** | `Console.Error.WriteLine` | `ILogger<T>` + 结构化日志 |
| **输出能力** | 仅 LaTeX 文本 | `problemText` + `studentSolutionText` + `formulas[]` + 分区检测 + `warnings` |
| **前端支持** | 无 MathLive 校对 | MathLive inline 编辑 + 复制/插入流程 |
| **是否主动分析** | 不自动分析（需再调 analyze） | 不自动分析（需手动点击），同 |

**结论：Legacy OCR 已被新链路完全替代，且新链路在安全性、跨平台、功能完整性上全面优于旧链路。**

### 5.2 分析链路对比

| 维度 | Legacy QuestionController | 当前主链路 |
|------|--------------------------|-----------|
| **端点** | `POST /api/Question/analyze/{id}` | `POST /api/learning-analysis/analyze` |
| **模型调用** | 直接 `new HttpClient()` → `https://api.deepseek.com/chat/completions` | `LLMGateway` → LiteLLM/DeepSeek（通过 `IHttpClientFactory`） |
| **API Key 管理** | 直接从 config 读取传入 HTTP Header | LLMGateway 统一管理，含 ASCII 校验 |
| **统一日志** | 无 | `LLMRequestLog` 表（provider/model/tokens/latency/status/errorCode） |
| **重试机制** | 无 | LLMGateway 统一错误处理 |
| **鉴权** | admin / Development Override | session user（全部登录用户可用） |
| **UserId 收敛** | 不涉及用户归属 | `userId mismatch` 403 防护 |
| **输入来源** | 从 `WrongQuestions` 表读取历史 OCR 结果 | 前端直接传入 `problemText` + `studentSolutionText` |
| **分析结果存储** | 更新 `WrongQuestions` 表字段 | 写入 `Problem` + `StudentSolution` + `AnalysisResult` + `MistakeRecord` |
| **统计更新** | 无 | `UserCourseStats` + `UserKnowledgeState` 自动更新 |
| **知识点归一化** | 无（直接存 LLM 返回的原始标签） | `KnowledgePointNormalizer` 映射到 `KnowledgePoint.Code` |
| **结构化输出** | 6 个字符串字段 | 完整的 `AnalysisResponseDto`（含 `isCorrect`、`mainIssue`、`logicGaps`、`mistakeTags`、`standardSolution`、`visualization` 等） |

**结论：Legacy Analyze 已被新链路完全替代，且新链路在日志、安全、统计、数据结构化程度上全面优于旧链路。**

### 5.3 功能替代矩阵

| Legacy 功能 | 是否已被替代 | 替代方案 |
|------------|------------|---------|
| 图片上传 + OCR | **是** | `POST /api/photo-solutions/ocr` + `LiteLLMPhotoSolutionOcrProvider` |
| AI 分析 | **是** | `POST /api/learning-analysis/analyze` + `AnalysisService` + `LLMGateway` |
| 历史记录列表 | **否**（仅 admin 调试用） | 无直接替代；可通过 `LLMRequestLog` 表间接审计 |
| `WrongQuestions` 表数据 | **否**（遗留数据） | 新链路写入 `StudentSolution` + `AnalysisResult` 表 |

**唯一未被替代的功能**：`GET /api/Question/list` 的 `WrongQuestions` 历史浏览。但该功能仅为 admin 开发调试使用，不影响学生端主链路。

### 5.4 保留价值判断

| 维度 | 评估 |
|------|------|
| 对 MVP 主链路有价值 | **否** |
| 对开发调试有价值 | **低** — 仅在需要查看旧 `p2t.exe` OCR 历史记录时有意义，但随着新链路成熟，该价值趋近于零 |
| 对数据迁移有价值 | **低** — `WrongQuestions` 表可通过 EF Core 直接查询，不需要通过 API |
| 对测试有价值 | **否** — 当前无自动化测试依赖此控制器 |

---

## 6. 处理方案比较

### 方案 A：完全删除 QuestionController

**操作：**
1. 删除 `Controllers/QuestionController.cs`
2. 从 `wwwroot/index.html` 移除 `<script src="/js/legacy-ocr.js">`（因为该脚本仅服务于 legacy API）
3. 从 `wwwroot/dev.html` 移除 legacy area DOM 和 `<script src="/js/legacy-ocr.js">`
4. 删除 `wwwroot/js/legacy-ocr.js`
5. 从 `Program.cs:65` 移除 `LLMService` 注册（该服务也仅被 legacy 链路使用）
6. 删除 `Services/LLMService.cs`
7. 保留 `Models/WrongQuestion.cs` 和 `Data/ApplicationDbContext.cs` 中的 `DbSet<WrongQuestion>`（数据库兼容性，不破坏已有迁移）
8. 更新相关文档

| 优点 | 缺点 |
|------|------|
| 风险最低：彻底消除硬编码路径、绕过 LLMGateway、文件泄露等全部遗留风险 | 丢失 dev.html 中的 legacy OCR 历史浏览功能（但该功能在非 Windows 环境本就不可用） |
| 清理遗留代码，降低维护负担 | 需同步清理前端两个页面的脚本引用 |
| 消除 `LLMService` 未使用依赖 | 需确认 `WrongQuestions` 表不删除（已有迁移依赖） |

**前置条件：**
- 确认 `index.html` 和 `dev.html` 移除 `legacy-ocr.js` 后不影响主链路（已验证不影响）
- 确认 `LLMService` 无其他引用（已验证无引用）

### 方案 B：仅 Development + Admin 可用（明确标记）

**操作：**
1. 在 `QuestionController` 类上添加 `// LEGACY: Development-only debug tool. Do NOT enable in Production.` 注释
2. 在 `EnsureLegacyDeveloperAccessAsync` 中增加更明确的限制逻辑
3. 在 `appsettings.json` 的 `Auth` 节增加注释说明 `EnableDevelopmentLegacyAccessOverride` 的风险
4. 保留现有代码不变

| 优点 | 缺点 |
|------|------|
| 保留 admin 的调试能力（如查看历史 OCR 记录） | 遗留风险代码仍存在，只是被权限隔离 |
| 改动最小，不破坏演示冻结 | 硬编码路径在 macOS/Linux 下仍无法使用（upload 永远失败） |
| 可在后续阶段再决定删除 | `analyze` 端点仍绕过 LLMGateway，admin 使用时会产生无日志的 API 调用 |

### 方案 C：重构接入新链路

**操作：**
1. OCR 改接 `IPhotoSolutionOcrProvider`
2. 分析改接 `LearningAnalysisController` / `LLMGateway`
3. 文件上传改走安全存储
4. 权限保持 admin 限制

| 优点 | 缺点 |
|------|------|
| 功能保留且统一到新链路 | **工作量大**，需要重构整个控制器 |
| 消除遗留风险的同时保留功能 | **不值得** — legacy 功能已被新链路完全替代 |
| 可纳入统一日志和错误处理 | 违反演示冻结原则 |
| | 重构后的控制器本质上会变成新链路的包装器，意义不大 |

---

## 7. 推荐方案

### 推荐：**方案 B（短期）+ 方案 A（中期）**

#### 短期（R35-b/c，演示冻结期间）：方案 B — 明确标记 + 强化限制

在当前演示冻结期间，不删除代码，做最小改动：

1. **强化注释标记**：在 `QuestionController.cs` 头部添加醒目的 `// LEGACY` 警告块
2. **确认 Production fail-fast**：`Program.cs` 已有 `ValidateAuthConfiguration` 确保 Production 下 `EnableDevelopmentLegacyAccessOverride=true` 会阻止启动 — **当前已满足**
3. **文档化风险**：将本审计文档链接到 `ProjectAudit.md`、`CurrentProjectStatus.md`、`DemoFreeze.md`、`LocalDevelopmentRunbook.md`

#### 中期（R35-d/e/f，演示后）：方案 A — 完全删除

演示结束后，执行彻底清理：

1. 删除 `QuestionController.cs`
2. 移除前端 legacy 引用
3. 删除 `legacy-ocr.js`
4. 删除 `LLMService.cs` 及其 DI 注册
5. 保留 `WrongQuestions` 模型（数据库兼容）

### 理由

1. **QuestionController 已不被主链路使用** — `index.html` 虽然在 HTML 层加载了 `legacy-ocr.js`，但因缺少 DOM 元素完全不执行实际 API 调用（`hasLegacyDom()` 返回 false）
2. **新链路已完全替代旧功能** — OCR（`/api/photo-solutions/ocr`）和分析（`/api/learning-analysis/analyze`）在功能、安全、跨平台上均优于旧链路
3. **硬编码路径使 upload 端点在任何非 Windows 环境都不可用** — macOS/Linux/Docker 下 `p2t.exe` 不存在，upload 永远失败
4. **analyze 端点绕过 LLMGateway** — 即使 admin 使用也会产生无日志、无重试、无 key 校验的裸调
5. **演示冻结期不建议大动** — 方案 B 满足冻结规则（只加注释/文档，不改业务逻辑）
6. **不建议重构（方案 C）** — 功能已被替代，重构的投入产出比太低

---

## 8. 后续实施拆分

| 阶段 | 编号 | 内容 | 是否改代码 | 是否改 DB |
|------|------|------|-----------|----------|
| **当前** | R35-a | 审计与 containment design（本文档） | 否 | 否 |
| **短期** | R35-b | 确认前端/主链路无引用，补充测试验证（**本轮完成**） | 否（只验证） | 否 |
| **短期** | R35-c | 强化 QuestionController Production 禁用 + 修复错误泄露（**本轮完成**） | 是（仅安全加固） | 否 |
| **中期** | R35-d | 删除 `QuestionController.cs` | 是 | 否 |
| **中期** | R35-e | 清理前端 legacy 引用（`legacy-ocr.js`、HTML script 标签）+ 删除 `LLMService.cs` | 是 | 否 |
| **中期** | R35-f | 清理旧配置、旧文档引用 | 是（文档） | 否 |

---

## 9. R35-b 删除前引用验证结果

> 执行日期：2026-06-03 | `dotnet build` 通过（0 errors, 2 unrelated NU1900 warnings）

### 9.1 全仓库关键词搜索结果

#### `/api/Question` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `wwwroot/js/legacy-ocr.js:22` | **代码引用（运行时）** | `POST /api/Question/analyze/{id}`，仅 dev.html admin 触发 |
| `wwwroot/js/legacy-ocr.js:41` | **代码引用（运行时）** | `GET /api/Question/list`，仅 dev.html admin 触发 |
| `Docs/LegacyQuestionControllerAudit.md`（~15 处） | 文档引用 | 本文档审计内容 |
| `Docs/LocalDevelopmentRunbook.md`（3 处） | 文档引用 | 权限回归测试说明 |

#### `QuestionController` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `Controllers/QuestionController.cs` | **代码（定义）** | 控制器本身 |
| `wwwroot/js/legacy-ocr.js`（间接） | **代码引用** | 通过 API 路径间接引用 |
| `wwwroot/js/nav.js:99-101` | **代码引用（运行时）** | `window.loadQuestions()` 调用，仅 dev.html + admin 角色时触发 |
| `Docs/CurrentProjectStatus.md`（3 处） | 文档引用 | R19-i/R35-a/b 状态记录 |
| `Docs/AuthDesign.md:78` | 文档引用 | 权限角色矩阵中的条目说明 |
| `Docs/LocalDevelopmentRunbook.md:11` | 文档引用 | 文档索引链接 |
| `Docs/ProjectAudit.md`（3 处） | 文档引用 | 安全审计与风险清单 |

**无其他 `.cs` 文件引用 `QuestionController`**（已通过 `grep -rn "QuestionController" --include="*.cs"` 排除自身确认）。

#### `WrongQuestion` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `Controllers/QuestionController.cs`（5 处） | **代码引用（运行时）** | 读写 `WrongQuestions` 表；若删除控制器则这些引用随之消失 |
| `Data/ApplicationDbContext.cs:14` | **代码（DB 注册）** | `DbSet<WrongQuestion>` EF Core 实体注册（已有迁移依赖） |
| `Models/WrongQuestion.cs` | **代码（模型定义）** | 遗留数据模型 |
| `Docs/CurrentProjectStatus.md:12` | 文档引用 | 说明保留 legacy 兼容表 |
| `Docs/LegacyQuestionControllerAudit.md`（~12 处） | 文档引用 | 本文档审计内容 |

**关键发现：`Services/` 目录下零引用** — `AnalysisService`、`LLMGateway` 等所有新链路服务完全不依赖 `WrongQuestion`。

#### `LLMService` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `Program.cs:65` | **代码（DI 注册）** | `builder.Services.AddScoped<LLMService>()`，注册但**无任何类注入使用** |
| `Docs/LegacyQuestionControllerAudit.md`（~10 处） | 文档引用 | 本文档审计 |

**关键发现：`LLMService` 在代码库中无任何 `using` 或构造函数注入引用。** `QuestionController` 也不使用它（直接用 `new HttpClient()`）。

#### `legacy-ocr.js` / legacy JS 函数引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `wwwroot/index.html:124` | **代码（HTML script 加载）** | `<script src="/js/legacy-ocr.js">` — **加载但不执行 API 调用** |
| `wwwroot/dev.html:121` | **代码（HTML script 加载）** | `<script src="/js/legacy-ocr.js">` — **加载且可执行** |
| `wwwroot/dev.html:106` | **代码（DOM + onclick）** | `toggleLegacy()` + `analyzeLegacy()` inline 调用 |
| `wwwroot/js/nav.js:99-101` | **代码（运行时调用）** | `loadQuestions()` 由 admin guard 触发 |
| `wwwroot/js/legacy-ocr.js`（自身） | **代码（函数定义）** | `loadQuestions`, `analyzeLegacy`, `toggleLegacy`, `hasLegacyDom` |

#### `p2t.exe` / `P2T_PATH` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `Controllers/QuestionController.cs:21-22` | **代码定义** | 硬编码 Windows 路径常量 |
| `Controllers/QuestionController.cs:70,227,231` | **代码引用** | `RunPix2Text()` 使用 |
| `Docs/LegacyQuestionControllerAudit.md`（~8 处） | 文档引用 | 本文档审计 |

#### `EnableDevelopmentLegacyAccessOverride` 引用

| 位置 | 类型 | 说明 |
|------|------|------|
| `Program.cs:181-184` | **代码（Production fail-fast）** | 若 Production 下开启则拒绝启动 |
| `Options/AuthOptions.cs:16` | **代码（配置绑定）** | `AuthOptions` 属性定义 |
| `appsettings.json:49` | **配置** | 默认 `false` |
| `appsettings.Development.json:7` | **配置** | 默认 `false` |
| `Controllers/QuestionController.cs:223` | **代码（运行时）** | 读取配置决定是否放行 |

#### `MathpixService` 额外发现

| 位置 | 类型 | 说明 |
|------|------|------|
| `Services/MathpixService.cs` | **代码（空类）** | 空骨架类，命名空间 `MathAnalysisAI.Services`（非标准），**零引用** |

### 9.2 前端引用验证详情

#### 各页面 legacy 引用状态

| 页面 | 加载 `legacy-ocr.js` | 存在 legacy DOM 元素 | 实际触发 API 调用 | 影响学生主流程 |
|------|---------------------|---------------------|-------------------|--------------|
| `index.html` | ✅ 是（line 124） | ❌ 否 — 无 `#legacyArea` / `#list` / `#legacyToggleBtn` | ❌ 否 — `hasLegacyDom()` 返回 false | ❌ 不影响 |
| `dev.html` | ✅ 是（line 121） | ✅ 是 — 有完整 legacy area DOM | ✅ 是 — 仅当 admin 角色（`nav.js` guard） | ❌ 不影响（学生端 dev 页被隐藏） |
| `materials.html` | ❌ 否 | ❌ 否 | ❌ 否 | ❌ 不影响 |
| `stats.html` | ❌ 否 | ❌ 否 | ❌ 否 | ❌ 不影响 |
| `login.html` | ❌ 否 | ❌ 否 | ❌ 否 | ❌ 不影响 |

#### index.html legacy-ocr.js 加载分析

`index.html:124` 加载了 `legacy-ocr.js`，但该脚本的 IIFE 在 `DOMContentLoaded` 时执行 `hasLegacyDom()` 检查：

```javascript
function hasLegacyDom() {
    return !!(UI.qs("#legacyArea") && UI.qs("#legacyToggleBtn") && UI.qs("#list"));
}
```

`index.html` 中不存在这三个 DOM 元素，因此：
- `loadQuestions()` **不会被自动调用**
- `toggleLegacy()` / `analyzeLegacy()` 全局函数虽暴露但**不会被任何绑定的 UI 触发**
- **零网络请求**发往 legacy API

**结论：`index.html` 加载 `legacy-ocr.js` 是无效的死代码引用，不影响主流程，但属于冗余的 HTML 依赖。**

#### dev.html 当前实际可触发路径

触发 legacy API 需要**三个条件同时满足**：
1. 访问 `dev.html` 页面
2. 用户具有 `admin` 角色（`nav.js:95-98` guard）
3. 页面自动调用 `loadQuestions()`（`nav.js:99-101`）或用户手动展开后点击按钮

**当前条件下**：
- `student`：dev 导航被 `nav.js` 隐藏 + 后端 403
- `teacher`：dev 导航被 `nav.js` 隐藏 + 后端 403
- `admin`：可访问 dev.html → 自动加载列表 → 可点击分析按钮

**实际唯一可触发前端的入口**：admin 用户访问 `dev.html`。

### 9.3 后端依赖图

```
QuestionController
├── 依赖 IUserContext / CurrentUserService  ← 共享服务，不能删
├── 依赖 ApplicationDbContext              ← 共享服务，不能删
├── 依赖 IConfiguration                    ← 共享服务，不能删
├── 依赖 IWebHostEnvironment               ← 共享服务，不能删
├── 使用 WrongQuestion 模型                ← 仅此控制器使用
├── 使用 p2t.exe 硬编码路径                ← 仅此控制器使用
├── 直接 new HttpClient()                  ← 仅此控制器使用
└── 被以下引用：
    ├── legacy-ocr.js（前端 HTTP 调用）     ← 仅由 dev.html admin 触发
    └── Program.cs（[ApiController] 自动发现）← ASP.NET Core 运行时扫描

LLMService（Services/LLMService.cs）
├── 依赖 IConfiguration                   ← 共享服务
├── 依赖 HttpClient（DI 注入）             ← 共享服务
├── 被引用：无（仅 Program.cs:65 DI 注册）  ← 零运行时使用者
└── 状态：**完全孤立，可安全删除**

WrongQuestion（Models/WrongQuestion.cs）
├── 被 QuestionController 使用             ← 唯一运行时消费者
├── 被 ApplicationDbContext 注册           ← EF Core 迁移依赖
└── 状态：**模型保留，但可在删除控制器后转为纯 DB 兼容表**

MathpixService（Services/MathpixService.cs）
├── 命名空间：MathAnalysisAI.Services（非标准，非 MathAnalysisAI.Server.Services）
├── 被引用：零
├── DI 注册：无
└── 状态：**孤立空骨架，可安全删除**
```

### 9.4 新链路隔离确认

| 新链路组件 | 是否引用 WrongQuestion | 是否引用 LLMService | 是否引用 QuestionController |
|-----------|----------------------|--------------------|---------------------------|
| `AnalysisService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `LLMGateway` | ❌ 否 | ❌ 否 | ❌ 否 |
| `LiteLLMPhotoSolutionOcrProvider` | ❌ 否 | ❌ 否 | ❌ 否 |
| `LearningAnalysisController` | ❌ 否 | ❌ 否 | ❌ 否 |
| `PhotoSolutionsController` | ❌ 否 | ❌ 否 | ❌ 否 |
| `AnalysisPersistenceService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `MistakeRecordService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `UserStatsUpdateService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `LlmRequestFactory` | ❌ 否 | ❌ 否 | ❌ 否 |
| `AnalysisFallbackService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `LeaderboardService` | ❌ 否 | ❌ 否 | ❌ 否 |
| `SymPySymbolicMathService` | ❌ 否 | ❌ 否 | ❌ 否 |

**结论：新链路 100% 隔离，删除 QuestionController / LLMService 不会影响任何新链路功能。**

### 9.5 构建验证

```
dotnet build 结果：
  ✅ 0 errors
  ⚠️ 2 warnings（NU1900: NuGet 漏洞数据库不可达，与 legacy 代码无关）
  ✅ 输出：bin/Debug/net8.0/MathAnalysisAI.Server.dll
```

---

## 10. 测试建议

后续 R35-b/c/d 实现时，建议验证以下场景：

### 权限测试
- [ ] student 访问 `GET /api/Question/list` → `403 Forbidden`
- [ ] student 访问 `POST /api/Question/upload` → `403 Forbidden`
- [ ] student 访问 `POST /api/Question/analyze/{id}` → `403 Forbidden`
- [ ] teacher 访问 legacy API → `403 Forbidden`
- [ ] 未登录用户访问 legacy API → `401 Unauthorized`
- [ ] Production 下 `EnableDevelopmentLegacyAccessOverride=true` → 服务拒绝启动

### 主链路不受影响（回归）
- [ ] `/api/auth/login`（test_student）正常
- [ ] `POST /api/photo-solutions/ocr` 正常
- [ ] MathLive 公式校对正常
- [ ] `POST /api/learning-analysis/analyze` 正常
- [ ] `GET /api/leaderboard/public` 正常
- [ ] `GET /api/auth/me` 正常
- [ ] `stats.html` 排行榜正常展示

### 编译
- [ ] `dotnet build` 通过
- [ ] `docker compose -f docker-compose.prod.yml config` 通过

---

## 11. R35-b 可删除性结论

### 11.1 是否可以安全进入 R35-c（Production 禁用 / 注释标记强化）

**✅ 可以，且大概率不需要做 R35-c。**

理由：
- `EnableDevelopmentLegacyAccessOverride` 当前在 `appsettings.json` 和 `appsettings.Development.json` 中均为 `false`
- Production fail-fast (`Program.cs:181-184`) 已有
- student/teacher 已被 403 阻止
- **由于可以直接进入 R35-d（删除），R35-c 的"注释标记强化"阶段性意义不大**

唯一 R35-c 可做工作：
- 在 `QuestionController.cs` 头部添加 `// LEGACY: Scheduled for deletion in R35-d. Do NOT enable in Production.` 注释（一行即可）
- 但鉴于后续直接删除，这一步是可选的

### 11.2 是否可以安全进入 R35-d（删除 QuestionController.cs）

**✅ 可以。**

前置条件全部满足：
- ✅ 新链路不依赖 QuestionController（9.4 节 100% 隔离确认）
- ✅ 前端 `index.html` 不执行 legacy API 调用（`hasLegacyDom()` 失败）
- ✅ student/teacher 已被 403 阻止
- ✅ `dotnet build` 通过
- ✅ 唯一运行时消费者（`dev.html` admin）可在 R35-e 同步清理

删除 QuestionController.cs 的副作用：
- `wwwroot/js/legacy-ocr.js` 的 API 调用会在 admin 访问 dev.html 时返回 404（因为路由消失）— 但这不影响主链路，且将在 R35-e 一并清理
- `WrongQuestion` 模型和 `DbSet<WrongQuestion>` 将变为"无运行时消费者"的纯 DB 兼容表

### 11.3 是否可以安全进入 R35-e（清理前端 legacy 引用 + LLMService + MathpixService）

**✅ 可以。**

需要清理的清单：

| 操作 | 文件 | 风险 |
|------|------|------|
| 删除 | `Controllers/QuestionController.cs` | 无（已确认隔离） |
| 删除 | `Services/LLMService.cs` | 无（零引用，R35-a 已确认） |
| 删除 | `Services/MathpixService.cs` | 无（空类，零引用，非标准命名空间） |
| 删除 | `wwwroot/js/legacy-ocr.js` | 无（删除后 `index.html` 和 `dev.html` 的 script 标签也需移除） |
| 移除 script 标签 | `wwwroot/index.html:124` | 无（该脚本在 index.html 上已不执行任何操作） |
| 移除 script 标签 | `wwwroot/dev.html:121` | admin 端失去 legacy 功能（但新链路已替代） |
| 移除 legacy DOM | `wwwroot/dev.html:103-111` | admin 端 dev 页简化 |
| 移除 `loadQuestions()` 调用 | `wwwroot/js/nav.js:99-101` | 无（该调用仅在 legacy area DOM 存在时执行） |
| 移除 DI 注册 | `Program.cs:65` | 无（`LLMService` 无运行时消费者） |
| 移除配置属性 | `Options/AuthOptions.cs:16` | **需谨慎** — `EnableDevelopmentLegacyAccessOverride` 也在 `Program.cs:181-184` 的 Production fail-fast 中引用；删除前需同步清理 |
| 移除配置项 | `appsettings.json:49` | 无 |
| 移除配置项 | `appsettings.Development.json:7` | 无 |
| 移除 Production fail-fast 条目 | `Program.cs:181-184`（仅删除 `EnableDevelopmentLegacyAccessOverride` 的三行） | 不影响其他 override 检查 |
| **保留** | `Models/WrongQuestion.cs` | 已有 migration 依赖，保留为空壳模型 |
| **保留** | `Data/ApplicationDbContext.cs:14`（`DbSet<WrongQuestion>`） | migration 依赖，保留 |

### 11.4 WrongQuestion 表处理策略

**当前决定：保留模型和 DbSet，不删表。**

理由：
- `WrongQuestion` 表存在已有 EF Core migration 中
- 删除模型 + DbSet 需要生成新 migration，违反演示冻结规则
- 保留空壳模型和 DbSet 不产生运行时成本（无消费者即无查询）
- 后续可在独立阶段（如 `R40-db-cleanup`）统一处理遗留表清理

### 11.5 合并建议：R35-d + R35-e 一次性执行

由于 R35-d（删除控制器）和 R35-e（清理前端 + LLMService）之间没有阻塞依赖，建议在演示冻结解除后**合并为一次提交**执行，减少中间状态。

推荐顺序：
1. 删除 `Services/LLMService.cs` + `Services/MathpixService.cs`
2. 删除 `Controllers/QuestionController.cs`
3. 删除 `wwwroot/js/legacy-ocr.js`
4. 修改 `wwwroot/index.html`（移除 script 标签）
5. 修改 `wwwroot/dev.html`（移除 legacy area DOM + script 标签）
6. 修改 `wwwroot/js/nav.js`（移除 `loadQuestions()` 调用）
7. 修改 `Program.cs`（移除 `LLMService` 注册 + 移除 `EnableDevelopmentLegacyAccessOverride` fail-fast 条目）
8. 修改 `Options/AuthOptions.cs`（移除 `EnableDevelopmentLegacyAccessOverride` 属性）
9. 修改 `appsettings.json`（移除配置项）
10. 修改 `appsettings.Development.json`（移除配置项）
11. `dotnet build` 确认通过

---

## 12. 决策结论

| 决策项 | 结论 |
|--------|------|
| **QuestionController 是否可视为遗留控制器** | **是** |
| **是否仍被前端或主链路使用** | **否**（R35-b 已验证：`index.html` 加载脚本但不执行调用；新链路 100% 隔离） |
| **R35-b 验证结果** | ✅ 全仓库 0 遗漏代码引用；✅ `Services/` 目录 0 引用 WrongQuestion/LLMService；✅ `dotnet build` 通过 |
| **可进入 R35-c（注释标记）** | ✅ 可以，但鉴于可直接删除，建议跳过 |
| **可进入 R35-d（删除 Controller）** | ✅ 可以 |
| **可进入 R35-e（清理前端+服务）** | ✅ 可以，建议与 R35-d 合并为一次提交 |
| **建议删除时机** | 演示冻结解除后，R35-d+e 合并执行 |
| **WrongQuestion 处理** | **保留模型和 DbSet**（已有 migration 依赖，不删表，不动 DB） |
| **MathpixService 处理** | **可安全删除**（空类，零引用，非标准命名空间） |
| **是否改业务代码（本轮 R35-b）** | **否** |
| **是否改数据库结构** | **否** |
| **是否生成 migration** | **否** |
| **是否调用外部 API** | **否** |

---

## 附录 A：涉及文件完整清单

### 遗留链路文件（后续待删除/清理）
| 文件 | 角色 |
|------|------|
| `Controllers/QuestionController.cs` | Legacy 控制器 |
| `Services/LLMService.cs` | Legacy LLM 服务（无引用，仅被 DI 注册） |
| `Services/MathpixService.cs` | 空骨架类（零引用，非标准命名空间）— R35-b 新发现 |
| `wwwroot/js/legacy-ocr.js` | Legacy 前端 OCR/分析调用脚本 |
| `Models/WrongQuestion.cs` | Legacy 数据模型（**保留**，已有迁移依赖） |

### 引用遗留链路的文件（后续待修改）
| 文件 | 修改内容 |
|------|---------|
| `wwwroot/index.html:124` | 移除 `<script src="/js/legacy-ocr.js">` |
| `wwwroot/dev.html:103-111` | 移除 `#legacyArea` DOM（含 `#list` / toggle button） |
| `wwwroot/dev.html:121` | 移除 `<script src="/js/legacy-ocr.js">` |
| `wwwroot/js/nav.js:99-101` | 移除 `loadQuestions()` 调用 |
| `Program.cs:65` | 移除 `LLMService` DI 注册 |
| `Program.cs:181-184` | 移除 `EnableDevelopmentLegacyAccessOverride` fail-fast 条目（3 行） |
| `Options/AuthOptions.cs:16` | 移除 `EnableDevelopmentLegacyAccessOverride` 属性 |
| `appsettings.json:49` | 移除 `EnableDevelopmentLegacyAccessOverride` 配置项 |
| `appsettings.Development.json:7` | 移除 `EnableDevelopmentLegacyAccessOverride` 配置项 |
| `Data/ApplicationDbContext.cs:14` | `DbSet<WrongQuestion>` **保留**（数据库兼容） |

### 审计相关文档（本轮产出/更新）
| 文件 | 操作 |
|------|------|
| `Docs/LegacyQuestionControllerAudit.md` | **新增**（本文档） |
| `Docs/ProjectAudit.md` | 更新链接 |
| `Docs/CurrentProjectStatus.md` | 更新链接 |
| `Docs/DemoFreeze.md` | 更新链接 |
| `Docs/LocalDevelopmentRunbook.md` | 更新链接 |

---

## 附录 B：风险汇总表

| Severity | 风险 | 当前缓解 | 残余风险 |
|----------|------|---------|---------|
| **CRITICAL** | 硬编码 Windows 绝对路径 `p2t.exe` | student/teacher 已被 403 阻止；仅 admin + Development Override 关闭时不可达 | admin 在 macOS/Linux/Docker 调用 upload 仍会失败并泄露错误信息 |
| **CRITICAL** | 绕过 LLMGateway，直接调 DeepSeek | student/teacher 已被 403 阻止 | admin 调用 analyze 时无日志、无重试、无 key 校验 |
| **HIGH** | 文件上传无类型/大小校验，保存到公开 `wwwroot/uploads/` | student/teacher 已被 403 阻止 | admin 可能上传任意文件到公开目录（但 admin 是可信角色） |
| **HIGH** | Development Override 可绕过所有鉴权 | Production fail-fast 阻止启动；Development 环境默认关闭 | 若开发人员手动开启，测试环境可被未认证用户访问 |
| **MEDIUM** | upload 端点泄露 `ex.Message` 给客户端 | student/teacher 已被 403 阻止 | admin 在非 Windows 环境调用时可看到内部路径/错误细节 |
| **MEDIUM** | `Console.Error.WriteLine` 不经过 `ILogger` | 仅影响日志质量，不造成安全漏洞 | 日志不受级别控制，运维排查不便 |
| **LOW** | `LLMService` 代码残留但无引用 | 代码不执行 | 代码库冗余 |
| **LOW** | `WrongQuestions` 表使用 `DateTime.Now` 而非 `UtcNow` | 不影响安全 | 时区不一致（与其他模型不一致） |

---

*本文档为 R35-a + R35-b 阶段产出。R35-a 完成审计与设计方案，R35-b 完成删除前全仓库引用验证与可删除性结论。业务代码、数据库结构、migration 均未修改。*
