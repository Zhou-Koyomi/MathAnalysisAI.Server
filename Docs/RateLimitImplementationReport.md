# R36-b Rate Limiting 实现报告

> **日期**：2026-06-03 | **状态**：已实现+已验证 | **运行时验证**：✅ 通过（R36-b-fix，R40-fix 已补 `/api/health` 豁免）

---

## 1. 实现范围

按照 R36-a 设计文档，为以下接口实现 ASP.NET Core Rate Limiting：

| 接口 | Policy | 限制 | 分区键 |
|------|--------|------|--------|
| `POST /api/auth/login` | `login` | 5 req/min | IP |
| `POST /api/learning-analysis/analyze` | `analyze` | 3 req/min | userId（fallback IP） |
| `POST /api/photo-solutions/ocr` | `ocr` | 2 req/min | userId（fallback IP） |
| `POST /api/symbolic/compute` | `symbolic` | 10 req/min | userId（fallback IP） |

---

## 2. 修改文件清单

| 文件 | 改动 | 详情 |
|------|------|------|
| `Program.cs` | 新增 `AddRateLimiter` 配置 + `UseRateLimiter` 中间件 | 添加 `Microsoft.AspNetCore.RateLimiting` 和 `System.Threading.RateLimiting` using；4 个命名 policy + 1 个 GlobalLimiter；`app.UseRouting()` + `app.UseRateLimiter()` |
| `Controllers/AuthController.cs` | 新增 `using` + `[EnableRateLimiting]` | `POST /api/auth/login` 标注 `login` policy |
| `Controllers/LearningAnalysisController.cs` | 新增 `using` + `[EnableRateLimiting]` | `POST /api/learning-analysis/analyze` 标注 `analyze` policy |
| `Controllers/PhotoSolutionsController.cs` | 新增 `using` + `[EnableRateLimiting]` | `POST /api/photo-solutions/ocr` 标注 `ocr` policy |
| `Controllers/SymbolicController.cs` | 新增 `using` + `[EnableRateLimiting]` | `POST /api/symbolic/compute` 标注 `symbolic` policy |
| `Controllers/HealthController.cs` | 新增 `using` + `[DisableRateLimiting]` | `GET /api/health` 显式豁免全局限流 |
| `Docs/RateLimitDesign.md` | 更新状态 | "设计阶段" → "已实现" |
| `Docs/CurrentProjectStatus.md` | 新增 R36-b 完成记录 |  |
| `Docs/RateLimitImplementationReport.md` | 新增（本文档） |  |

---

## 3. Policy 清单

### 3.1 login

```csharp
// Program.cs
options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"login:{context.Connection.RemoteIpAddress}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 3
        }));
```

- 类型：Fixed Window
- 分区键：客户端 IP
- 限制：5 req/min + 3 queue

### 3.2 analyze

```csharp
options.AddPolicy("analyze", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"analyze:{GetPartitionKey(context)}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 2
        }));
```

- 分区键：`user:{userId}`（已登录），否则 `ip:{address}`
- 限制：3 req/min + 2 queue

### 3.3 ocr

```csharp
options.AddPolicy("ocr", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"ocr:{GetPartitionKey(context)}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 1
        }));
```

- 分区键：`user:{userId}`（已登录），否则 `ip:{address}`
- 限制：2 req/min + 1 queue

### 3.4 symbolic

```csharp
options.AddPolicy("symbolic", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"symbolic:{GetPartitionKey(context)}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 5
        }));
```

- 分区键：`user:{userId}`（已登录），否则 `ip:{address}`
- 限制：10 req/min + 5 queue

### 3.5 GlobalLimiter（安全网）

```csharp
options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1000,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 100
        }));
```

- 所有未标注 policy 的请求使用此全局限制
- 非常宽松（1000 req/min），仅作为最后防线

---

## 4. Endpoint 绑定清单

| Controller | Method | 标注 | Policy | 有效 |
|-----------|--------|------|--------|------|
| `AuthController` | `POST /api/auth/login` | `[EnableRateLimiting("login")]` | `login` | ✅ |
| `AuthController` | `GET /api/auth/me` | 无 | GlobalLimiter | — |
| `AuthController` | `POST /api/auth/logout` | 无 | GlobalLimiter | — |
| `LearningAnalysisController` | `POST /api/learning-analysis/analyze` | `[EnableRateLimiting("analyze")]` | `analyze` | ✅ |
| `PhotoSolutionsController` | `POST /api/photo-solutions/ocr` | `[EnableRateLimiting("ocr")]` | `ocr` | ✅ |
| `SymbolicController` | `POST /api/symbolic/compute` | `[EnableRateLimiting("symbolic")]` | `symbolic` | ✅ |
| `HealthController` | `GET /api/health` | `[DisableRateLimiting]` | 豁免 | ✅ |
| `LeaderboardController` | `GET /api/leaderboard/public` | 无 | GlobalLimiter | — |
| `QuestionController`（Legacy） | 全部 | 无 | GlobalLimiter | — |

注意：`QuestionController` 在 Production 返回 404（R35-c），因此 GlobalLimiter 对其无实际影响。

---

## 5. 429 响应行为

当请求超过限制时，返回：

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/json

{
    "message": "请求过于频繁，请稍后重试。",
    "retryAfter": 30
}
```

- 不泄露 partition key
- 不泄露窗口大小或剩余配额
- 不泄露内部异常信息

---

### 5.1 中间件顺序（R36-b-fix 修复后）

```csharp
// Program.cs — 正确的顺序
app.UseRouting();          // 1. 确定 endpoint（需要 metadata）
app.UseSession();          // 2. 读取 session（供 rate limiter 获取 userId）
app.UseRateLimiter();      // 3. 应用限流（可读取 endpoint metadata + session userId）
app.UseAuthorization();    // 4. 认证/授权
app.UseEndpoints(...);     // 5. 执行 endpoint（MapControllers）
```

关键点：
- `UseRouting` 必须在 `UseRateLimiter` 之前（R36-b 原始设计用 `MapControllers` 隐式路由，导致 `UseRateLimiter` 看不到 endpoint metadata）
- `UseSession` 必须在 `UseRateLimiter` 之前（保证 `GetPartitionKey` 可读取 `auth_user_id`）
- `UseEndpoints(endpoints => endpoints.MapControllers())` 替代 `MapControllers()`（避免重复路由）

---

## 6. 测试结果

### 6.1 运行时验证（R36-b-fix）

**环境**：Docker compose Production

**login 限流测试**（15 并行请求）：
```
3 × HTTP 200
12 × HTTP 429
```
✅ 限流生效

**429 响应体**：
```json
{"message":"请求过于频繁，请稍后重试。","retryAfter":30}
```
✅ 不泄露内部细节，不暴露 partition key 或剩余配额

**health 端点**（R36-b-fix 早期验证）：
```http
HTTP 200
{"status":"ok","service":"MathAnalysisAI.Server","timestampUtc":"...","environment":"Production"}
```
⚠️ 该早期结论后来被 R40 审计推翻：在未显式豁免前，重复请求 `/api/health` 会命中 `GlobalLimiter`。

### 6.2 运行时验证（R40-fix）

**环境**：Docker compose Production，容器内 `curl`

**health 连续请求**：
```text
4 × HTTP 200
```
✅ `/api/health` 已通过 `[DisableRateLimiting]` 显式豁免，不再受 `GlobalLimiter` 影响

**login 连续请求**：
```text
8 × HTTP 429
```
✅ `/api/auth/login` 限流仍然生效；本次验证时窗口内已有历史请求，因此直接进入 429，不影响“策略仍有效”的结论

### 6.3 dotnet build

```
dotnet build:
  ✅ 0 errors
  ⚠️ 2 NU1900 warnings（NuGet 漏洞数据库不可达 — 已有，非本次引入）
  ✅ 输出：bin/Debug/net8.0/MathAnalysisAI.Server.dll
```

### 6.4 运行时验证（旧说明）

**状态**：❌ 未执行

**原因**：Docker 镜像仓库 `registry-1.docker.io` 不可达（context deadline exceeded），无法重新构建 server 镜像。当前运行的容器使用的是旧代码。

**验证 curl 命令**（网络恢复后执行）：

```bash
# 登录限流测试
for i in $(seq 1 10); do
  curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST http://localhost:5131/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test_student"}'
done
# 预期：前5次 200，第6-8次 200（queue），第9+次 429

# Health 已在 R40-fix 后通过 [DisableRateLimiting] 豁免
curl -s -o /dev/null -w "%{http_code}" http://localhost:5131/api/health
# 预期：200
```

---

## 7. 已知限制

### 7.1 内存存储

当前使用内存模式（默认），限制：

- **单实例**：多实例部署时每个实例有独立的限流计数器，无法实现全局限流
- **重启丢失**：容器/进程重启后计数器清空
- **不持久化**：无法查询历史限流记录

### 7.2 UseSession 顺序（R36-b-fix 已修复 ✅）

~~分区键使用 `HttpContext.Session.GetInt32("auth_user_id")` 获取用户 ID。这依赖 Session 中间件先于 RateLimiter 执行。~~ 

**已修复**：中间件顺序调整为 `UseRouting` → `UseSession` → `UseRateLimiter`。Session 在 RateLimiter 之前执行，`GetPartitionKey` 可正确读取 `auth_user_id`。同时 `UseEndpoints` 替代了 `MapControllers` 避免重复路由。

### 7.3 限流数值待验证

当前限流数值（analyze=3, ocr=2, login=5, symbolic=10）为初始建议值，**未经过真实用户行为数据验证**。

---

## 8. 后续建议

### 8.1 短期（演示后）

1. **R36-c**：前端 429 UI 浏览器侧验证
2. **R36-e**：添加限流日志（记录 429 触发，但不记录 payload）

### 8.2 中期（有真实用户后）

3. **R36-f**：基于实际使用数据调整限流数值
4. **R36-g**：多实例部署时迁移到 Redis 分布式限流

### 8.3 长期

5. 接入 ASP.NET 指标（Prometheus/OpenTelemetry）监控限流命中率
6. 与 LiteLLM 内部 budget 联动

---

## 9. 回滚建议

如需回滚，按以下步骤操作：

1. 从 `Program.cs` 移除：
   - `using Microsoft.AspNetCore.RateLimiting;`
   - `using System.Threading.RateLimiting;`
   - `builder.Services.AddRateLimiter(...)` 完整配置块
   - `app.UseRouting();`（如果原来没有显式调用）
   - `app.UseRateLimiter();`
2. 从 4 个 Controller 文件移除：
   - `using Microsoft.AspNetCore.RateLimiting;`
   - `[EnableRateLimiting("...")]` 属性
3. 执行 `dotnet build` 确认通过

回滚不影响任何业务逻辑（Rate Limiting 为纯附加层）。

---

*此报告为 R36-b 产出。限流代码已实现并通过编译。运行时验证延迟到 Docker 镜像仓库恢复后执行。*
