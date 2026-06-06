# CSRF 防护设计

> **R37-a** | 日期：2026-06-03 | 状态：设计阶段（不实现）

---

## 1. 当前风险分析

### 1.1 认证机制

项目使用 **Cookie/Session** 认证（`Program.cs:41-48`）：

```csharp
options.Cookie.Name = ".MathAnalysisAI.Auth";
options.Cookie.HttpOnly = true;
options.Cookie.SameSite = SameSiteMode.Lax;
```

Session cookie 随每个请求自动发送（浏览器行为），这意味着如果用户登录后访问恶意网站，恶意网站可能通过以下方式攻击：

- 自动提交 `<form>` 到 `POST /api/learning-analysis/analyze`
- 使用 JavaScript `fetch()` 发起跨站请求
- 利用 `SameSite=Lax` 的局限性（跨站 Form POST 请求会携带 cookie）

### 1.2 需要保护的接口（State-Changing）

| 接口 | Method | 风险 | 保护优先级 |
|------|--------|------|----------|
| `POST /api/auth/login` | POST | 登录伪造 | HIGH |
| `POST /api/auth/logout` | POST | 强制登出 | MEDIUM |
| `POST /api/learning-analysis/analyze` | POST | 伪造分析请求（消耗 API 费用） | HIGH |
| `POST /api/photo-solutions/ocr` | POST | 伪造 OCR 请求（消耗费用） | HIGH |
| `POST /api/course-materials/*` | POST | teacher/admin 接口 | HIGH |
| `POST /api/symbolic/compute` | POST | admin 接口 | MEDIUM |
| `GET /api/Question/*` | N/A | Legacy，Production 已禁用 | N/A |

### 1.3 SameSite Cookie 的局限性

当前使用 `SameSite=Lax`：
- **阻止**了大多数跨站 `fetch()` / `XMLHttpRequest` 攻击
- **不阻止**跨站 `<form method="POST">` 提交（`Lax` 允许 top-level navigation）
- **不阻止**某些浏览器的特殊情况

结论：**`SameSite` Cookie 不能单独作为 CSRF 防护。**

---

## 2. 候选方案

### 2.1 方案 A：ASP.NET Antiforgery（推荐）

ASP.NET Core 内置 Antiforgery 中间件提供成熟的 CSRF 防护：

```csharp
// Program.cs — 示例，当前不实现
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".MathAnalysisAI.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});
```

前端使用方式：
```javascript
// 启动时从 cookie 或 endpoint 获取 token
const token = getCsrfToken(); // 从 cookie 或 /api/csrf-token 获取

// 每次 POST 请求携带 header
fetch('/api/learning-analysis/analyze', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': token
    },
    body: JSON.stringify(payload)
});
```

优点：
- ASP.NET Core 原生支持
- 与现有 session 体系兼容
- Token 验证在服务端，不可伪造

缺点：
- 需要前端改造（所有 POST 请求需添加 header）
- 需要新增 token 获取端点或 cookie 读取逻辑

### 2.2 方案 B：自定义 Header Token

与方案 A 类似，但使用自定义实现：

- 服务端生成随机 token 存入 session
- 前端从 `/api/auth/me` 或专用端点获取 token
- 每次 POST 时携带 `X-CSRF-Token` header

优点：完全可控；缺点：需要手动实现所有验证逻辑。

### 2.3 方案 C：Double Submit Cookie

- 服务端设置非 HttpOnly cookie（如 `XSRF-TOKEN`）
- 前端读取 cookie 值并在请求 header 中回传
- 服务端比对 cookie 和 header 是否一致

优点：纯前端实现；缺点：安全性不如方案 A/B（cookie 可能被 XSS 读取）。

---

## 3. 推荐方案

**方案 A：ASP.NET Antiforgery**

原因：
1. 与 ASP.NET Core 深度集成，维护成本低
2. 支持 cookie + header 模式（适合 SPA）
3. 自动验证，不需要在每个 Controller Action 上手写检查
4. 与未来的 OIDC / production auth 兼容

### 3.1 实现影响范围

| 组件 | 需要改动 | 复杂度 |
|------|---------|--------|
| `Program.cs` | 添加 `AddAntiforgery()` 配置 | 低 |
| 前端 `api.js` | 获取 token + 自动附加 header | 低 |
| 前端所有页面 | 无需改动（`api.js` 统一处理） | 无 |
| Controller | 默认所有 POST 自动验证；GET/公开接口可用 `[IgnoreAntiforgeryToken]` 豁免 | 低 |

### 3.2 需要豁免 Antiforgery 的接口

| 接口 | 原因 |
|------|------|
| `GET /api/health` | 健康检查 |
| `GET /api/leaderboard/public` | 公开接口 |
| `GET /api/auth/me` | 读取当前用户，无状态变更 |
| `GET /api/course-materials/*` | 读取操作用 GET 方法 |

### 3.3 API 客户端/curl 调试兼容

对于自动化测试和 API 客户端（curl、Postman 等）：
- 需要先获取 CSRF token（从 cookie 或 `/api/csrf-token` 端点）
- 或在 Development 环境临时豁免
- 建议提供配置开关：`Security:EnableCsrfProtection`（默认 true）

---

## 4. 与 OAuth/OIDC 的关系

- 如果用 OIDC Authorization Code Flow，CSRF 保护更重要（防止 login CSRF）
- ASP.NET Antiforgery 与 OIDC 不冲突
- 需确保 OIDC `state` 参数和 CSRF token 是独立的两层防护

---

## 5. 后续实施拆分

| 阶段 | 内容 | 时机 |
|------|------|------|
| R37-a | 本文档（设计） | ✅ 当前 |
| R37-b | 在 `Program.cs` 中启用 Antiforgery + 前端接入 | 演示冻结解除后 |
| R37-c | 前端 `api.js` 自动附加 CSRF header | R37-b 同步完成 |
| R37-d | CSRF token 端点或 `/api/auth/me` 扩展 | R37-b 同步完成 |
| R37-e | CSRF 回归测试（所有 POST 接口） | R37-b/c/d 完成后 |

---

## 6. 决策结论

- ✅ **必须接入** CSRF 防护（当前 Cookie/Session 认证存在 CSRF 风险）
- ✅ **推荐** ASP.NET Core Antiforgery Middleware
- ✅ **实现不影响**现有业务逻辑（仅添加 token 验证层）
- ⚠️ 需要前端改动（`api.js` 统一附加 token header）
- ⚠️ 当前不实现（演示冻结期）
