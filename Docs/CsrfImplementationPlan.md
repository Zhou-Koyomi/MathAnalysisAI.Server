# CSRF 实现计划

> **R37-b-prep** | 日期：2026-06-03 | 状态：准备阶段（未实现 CSRF）

---

## 1. 需要 CSRF 保护的接口（State-Changing）

| 接口 | Method | 当前认证 | CSRF 优先级 |
|------|--------|---------|-----------|
| `POST /api/auth/login` | POST | Session（登录后） | HIGH |
| `POST /api/auth/logout` | POST | Session | HIGH |
| `POST /api/learning-analysis/analyze` | POST | Session + userId 验证 | HIGH — 有 API 费用风险 |
| `POST /api/photo-solutions/ocr` | POST | 无认证要求 | HIGH — 有 API 费用风险 |
| `POST /api/course-materials/upload` | POST | teacher/admin | HIGH |
| `POST /api/symbolic/compute` | POST | admin | MEDIUM |
| `GET /api/Question/*` (Legacy) | GET/POST | Production 已禁用 | N/A |

## 2. 不需要 CSRF 保护的接口

| 接口 | 原因 |
|------|------|
| `GET /api/health` | 公开健康检查 |
| `GET /api/auth/me` | 只读，无状态变更 |
| `GET /api/leaderboard/public` | 公开接口 |
| `GET /api/course-materials/search` | 只读检索 |

---

## 3. 当前代码切入点审计

### 3.1 前端 API 层

**文件**：`wwwroot/js/api.js`

当前 `Api.postJson` 和 `Api.postFormData` 只发送 `Content-Type: application/json` header。CSRF token 需要额外 header（如 `X-CSRF-TOKEN`）。

**改造点**：
```javascript
// 在 Api 初始化时获取 token，自动附加到所有 POST 请求
var csrfToken = null;

async function ensureCsrfToken() {
    if (csrfToken) return csrfToken;
    // 从 cookie 读取或从 /api/auth/me 响应 header 获取
    // 具体方式取决于后端实现
    return csrfToken;
}

async function postJson(url, payload) {
    var headers = { "Content-Type": "application/json" };
    var token = await ensureCsrfToken();
    if (token) headers["X-CSRF-TOKEN"] = token;
    // ... rest of implementation
}
```

### 3.2 后端切入点

**文件**：`Program.cs`

需要添加：
```csharp
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".MathAnalysisAI.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});
```

**文件**：`Controllers/AuthController.cs`

需要新增 token 发放端点：
```csharp
[HttpGet("csrf-token")]
public IActionResult GetCsrfToken()
{
    // Antiforgery middleware automatically generates and returns token
    // Frontend reads it from response or dedicated cookie
}
```

### 3.3 受影响的 Controller

所有包含 POST/PUT/DELETE 方法的 controller 默认被 Antiforgery 保护：
- `AuthController` — login, logout
- `LearningAnalysisController` — analyze
- `PhotoSolutionsController` — ocr
- `CourseMaterialsController` — upload
- `SymbolicController` — compute
- `QuestionController` (Legacy) — upload, analyze（Production 已禁用）

需要 `[IgnoreAntiforgeryToken]` 豁免的：
- 无（所有 POST 接口都应被保护）

### 3.4 Curl / API 调试兼容

CSRF 会给 curl 调试带来额外步骤：
```bash
# 先获取 CSRF token
curl -c /tmp/csrf.cookie -b /tmp/mathauth.cookie \
  http://localhost:5131/api/auth/csrf-token

# 从响应或 cookie 中提取 token，附加到后续请求
curl -b /tmp/mathauth.cookie \
  -H "X-CSRF-TOKEN: <token>" \
  -X POST http://localhost:5131/api/learning-analysis/analyze \
  -H "Content-Type: application/json" -d '{...}'
```

建议：在 Development 环境保留 `EnableDevelopmentCsrfBypass` 配置项。

---

## 4. 推荐分阶段实现

### R37-b-1：后端 Antiforgery 配置

- 在 `Program.cs` 添加 `AddAntiforgery` 配置
- 添加 `app.UseAntiforgery()` 中间件
- 添加 `GET /api/auth/csrf-token` 端点
- 默认所有 POST 需要 CSRF token
- 不破坏现有 session 登录

### R37-b-2：前端自动携带 CSRF Token

- 修改 `api.js`：获取 token + 自动附加 header
- 不改变各页面调用方式
- cookie 读取 token（如使用 cookie-based antiforgery）

### R37-b-3：豁免与兼容

- 添加 `[IgnoreAntiforgeryToken]` 到适当的 GET 接口
- Development 环境添加 CSRF bypass 配置
- 更新 curl 调试文档

### R37-b-4：回归测试

- 登录 / logout / analyze / OCR 均通过 CSRF 验证
- 不带 token 的请求返回 400/403
- curl 调试仍可用（带 token）

---

## 5. 与 OIDC / LocalPassword 的关系

- ASP.NET Antiforgery 与任何认证方案兼容
- OIDC 通常使用 Authorization Code Flow + PKCE，其中 `state` 参数提供 CSRF 保护
- `state` 和 Antiforgery token 是**两层独立防护**，不冲突也不重复
- 如果未来切换到 OIDC，Antiforgery 仍然需要（保护 API endpoint，OIDC state 只保护 login flow）

---

## 6. 当前不建议直接实现的原因

1. CSRF 一旦做错会破坏所有 POST 请求（登录、OCR、analyze 全挂）
2. 需要前端 `api.js` 和后端 `Program.cs` 同步改动
3. curl 调试路径会受影响，需要文档化新的调试方法
4. 演示冻结期间不应引入新的认证机制变更

---

## 7. 决策结论

- ❌ 当前不实现（演示冻结 + 破坏性变更风险）
- ✅ 已明确切入点（`api.js` + `Program.cs` + `AuthController`）
- ✅ 推荐路线：R37-b-1 ~ R37-b-4 分 4 步执行
- ✅ 与 OIDC 不冲突（独立的两层防护）
- ⚠️ 实现前需要完整回归所有 POST 接口
