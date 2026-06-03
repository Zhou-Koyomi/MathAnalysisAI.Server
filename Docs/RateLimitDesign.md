# API 限流设计（Rate Limiting）

> **R36-a + R36-b** | 日期：2026-06-03 | 状态：已实现（R36-b）

---

## 1. 为什么需要限流

### 1.1 成本风险

| 接口 | 涉及费用 | 风险 |
|------|---------|------|
| `POST /api/photo-solutions/ocr` | DashScope Qwen-VL API 调用费用 | 高频调用直接产生费用 |
| `POST /api/learning-analysis/analyze` | DeepSeek API 调用费用 | 同上 |
| `POST /api/symbolic/compute` | CPU 资源消耗（本地计算） | DoS 风险 |

### 1.2 资源风险

- `analyze` 单次请求可能需要 5-60 秒（取决于 prompt 大小和模型响应）
- 无限制并发可能导致所有 ASP.NET 线程池线程被占用
- 无限制请求可能压垮 LiteLLM 代理或达到上游 API rate limit

### 1.3 安全风险

- 未登录用户可调用 OCR（当前 `PhotoSolutionsController` 不要求认证）
- 恶意用户可能滥用 OCR/analyze 进行非教育目的的 API 调用

---

## 2. 接口分级策略

### 2.1 `/api/learning-analysis/analyze`（核心 - 高成本）

| 维度 | 建议值 | 说明 |
|------|--------|------|
| 每用户每分钟 | **3 次** | 正常学习场景下足够（需要看完上次结果才分析下一题） |
| burst | 5 次 | 允许短暂突发（如重新分析） |
| 未登录 | **禁止**（返回 401） | 已有 auth 检查；限流作为补充 |

### 2.2 `/api/photo-solutions/ocr`（OCR - 高成本）

| 维度 | 建议值 | 说明 |
|------|--------|------|
| 每用户每分钟 | **2 次** | 拍照 OCR 非高频操作 |
| burst | 3 次 | |
| 未登录每 IP 每分钟 | **1 次** | 当前接口无认证要求；至少限制匿名滥用 |

### 2.3 `/api/symbolic/compute`（计算 - 本地成本）

| 维度 | 建议值 | 说明 |
|------|--------|------|
| 每用户每分钟 | **10 次** | 符号计算为本地 CPU 成本，限制较宽松 |
| burst | 15 次 | |
| 未登录 | **禁止**（需 admin） | 已有 auth 检查 |

### 2.4 `/api/auth/login`（登录 - 安全）

| 维度 | 建议值 | 说明 |
|------|--------|------|
| 每 IP 每分钟 | **5 次** | 防暴力枚举（当前仅有用户名，后续加密码后更重要） |
| burst | 8 次 | |

### 2.5 其他接口

| 接口 | 策略 | 说明 |
|------|------|------|
| `GET /api/leaderboard/public` | 宽松限制（每 IP 10次/分钟） | 公开接口，但无直接费用 |
| `GET /api/health` | 不限流 | 健康检查 |
| `GET /api/auth/me` | 不限流 | 前端轮询已读用户 |
| `POST /api/course-materials/*` | 按 admin/teacher 角色放宽 | 管理接口，不面向大量用户 |
| `GET /api/Question/*` | Legacy — Production 已禁用，Development 不限 | 仅 dev/admin 调试用 |

---

## 3. 限流维度

### 3.1 按身份

| 身份 | 策略 |
|------|------|
| **未登录用户** | 仅允许操作公开接口，严格限制（建议 1-2 次/分钟/接口） |
| **student** | 按 userId 限流，默认建议值 |
| **teacher** | 比 student 宽松 2-3 倍（可能需要批量上传资料等） |
| **admin** | 最宽松，但仍有限流防止脚本错误导致的无限循环 |

### 3.2 按维度组合

- **主维度**：`userId`（已登录用户）
- **辅助维度**：`IP`（未登录 + 防止多用户共享 IP 绕过）
- **Endpoint 维度**：每个接口独立限流桶

---

## 4. 推荐技术方案

### 4.1 ASP.NET Core Rate Limiting Middleware（.NET 7+ 内置）

```csharp
// Program.cs — 示例伪代码，不在 R36-a 实现
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("analyze", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2 // burst: allow 2 queued
            }));

    // ... 其他策略
});

// 应用到 controller:
[EnableRateLimiting("analyze")]
```

### 4.2 备选方案

| 方案 | 优点 | 缺点 |
|------|------|------|
| ASP.NET Core Rate Limiting Middleware | 内置，配置简单，支持多种策略 | 默认内存存储，多实例不同步 |
| Redis 分布式限流 | 多实例同步 | 需引入 Redis Stack |
| Nginx `limit_req` | 在代理层限流 | 缺乏 userId 粒度 |
| LiteLLM 内置限流/预算 | 直接控制 LLM 消费 | 仅限 LLM 调用，不覆盖 OCR 等 |

### 4.3 推荐路线

**第一阶段**：ASP.NET Core Rate Limiting Middleware（内存）
- 适合单实例部署
- 零外部依赖
- 覆盖所有接口

**后续增强**：如多实例部署，可考虑 Redis 分布式限流或 Nginx 层辅助限流。

---

## 5. 错误返回

### 5.1 HTTP 429 Too Many Requests

```json
{
    "message": "请求过于频繁，请稍后重试。",
    "retryAfter": 30
}
```

- 包含 `Retry-After` header
- 不泄露具体限流窗口大小、剩余配额等内部实现细节

### 5.2 前端处理

- 收到 429 时在 UI 上显示友好提示
- 建议自动倒计时后恢复按钮

---

## 6. 日志与监控建议

- 不记录每次被限流请求的完整 payload（避免日志膨胀）
- 建议记录摘要指标：
  - `rate_limit_hit` 计数（按 endpoint + userId/IP）
  - 每分钟限流触发次数
- 后续可接入 ASP.NET 指标或 Prometheus

---

## 7. 与 LiteLLM 内部限流的关系

- LiteLLM 自身支持预算（budget）和 rate limit
- 但 LiteLLM 的限流作用于 **LLM provider 调用层面**，不感知用户体验
- 建议：
  - ASP.NET 层限流控制**用户频率**（用户体验保护）
  - LiteLLM 层限流控制**API 消费**（成本保护）
  - 两层互补，不是替代关系

---

## 8. 当前阶段不覆盖的场景

- 不实现 CAPTCHA / 验证码
- 不实现基于行为分析的异常检测
- 不实现 IP 黑名单 / 自动封禁
- 不实现按 API key 限流（当前无多 key 场景）

---

## 9. 后续实施拆分

| 阶段 | 内容 | 时机 |
|------|------|------|
| R36-a | 本文档（设计） | ✅ 当前 |
| R36-b | 在 `Program.cs` 中启用 ASP.NET Core Rate Limiting Middleware | 演示冻结解除后 |
| R36-c | 前端 429 处理 + UI 提示 | R36-b 完成后 |
| R36-d | 日志/指标监控 | R36-b 完成后 |
| R36-e | 根据实际使用数据调优限流数值 | 有真实用户后 |

---

## 10. 决策结论

- ✅ **必须接入**限流（OCR/LLM 有费用风险）
- ✅ **推荐** ASP.NET Core Rate Limiting Middleware（内存模式）
- ✅ **接口独立**限流策略（analyze=3次/分, OCR=2次/分, login=5次/分）
- ⚠️ 数值为初始建议，后续需根据实际使用调整
- ⚠️ 当前不实现（演示冻结期）
