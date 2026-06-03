# ASP.NET Forwarded Headers 设计

> **R34-c-design** | 日期：2026-06-03 | 状态：设计阶段（不改代码）

---

## 1. 背景与目标

当 ASP.NET Core 运行在 Nginx 反向代理之后时：

- 客户端连接的是 `https://example.com`（Nginx 443 端口）
- Nginx 通过 `http://127.0.0.1:5131` 反代到 ASP.NET server
- ASP.NET 看到的 `Request.Scheme` 是 `http`（不是 `https`）
- ASP.NET 看到的 `Request.Host` 是 `127.0.0.1:5131`（不是 `example.com`）
- 重定向、Cookie Secure、OAuth redirect_uri 等逻辑将基于错误的协议和域名生成 URL

### 1.1 需要解决的问题

| 问题 | 影响 |
|------|------|
| `Request.Scheme` 错误识别为 `http` | Cookie 可能不设 Secure 标记；重定向 URL 使用错误协议 |
| 客户端 IP 丢失 | 日志/审计中所有请求看起来都来自 `127.0.0.1` |
| Host 不正确 | 自引用 URL（如 API 返回的链接）使用内网地址 |

### 1.2 Forwarded Headers 的作用

ASP.NET Core 的 Forwarded Headers Middleware 读取上游代理设置的 header：

- `X-Forwarded-For` → 还原真实客户端 IP
- `X-Forwarded-Proto` → 还原原始协议（https/http）
- `X-Forwarded-Host` → 还原原始 Host

---

## 2. 当前 Nginx 模板传递的 Headers

Nginx 模板（`deploy/nginx/mathanalysis-ai.conf.example`）已配置：

```nginx
proxy_set_header X-Real-IP         $remote_addr;
proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host  $host;
```

---

## 3. 信任模型

### 3.1 核心原则：只信任已知代理

- **不应盲目信任**客户端自行伪造的 `X-Forwarded-*` header
- **只信任**来自 Nginx 反向代理的 header
- 如果有 CDN/WAF 在前，需将 CDN/WAF 也加入信任链

### 3.2 当前部署拓扑中的可信来源

```
Client (公网)
  → Nginx (宿主机, 127.0.0.1)  ← 唯一可信反向代理
    → ASP.NET server (Docker 容器)
```

Nginx 在宿主机运行，ASP.NET 在 Docker 容器中，Nginx 通过 `http://127.0.0.1:5131` 反代。

### 3.3 Docker Bridge 网络下的 IP 问题

如果 Nginx 在宿主机，ASP.NET server 在 Docker 容器中：
- 从 ASP.NET 视角看，请求来自 Docker bridge gateway（通常是 `172.17.0.1` 或类似）
- Forwarded Headers Middleware 需要信任这个 IP

**部署时必须确认**：ASP.NET 容器看到的来自宿主机的源 IP 是什么，并将该 IP 或网段加入 `KnownProxies`。

---

## 4. 设计方案

### 4.1 推荐：仅信任本地环回与 Docker bridge gateway

```csharp
// Program.cs — 在 app.UseRouting() 之前，紧接 builder.Build() 之后
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // 只信任本地来源（Nginx 在宿主机，通过 127.0.0.1 或 Docker bridge 连接）
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // 信任本地环回
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
        System.Net.IPAddress.Parse("127.0.0.0"), 8));

    // 信任 Docker bridge 网段（部署时确认实际网段）
    // 常见值：172.17.0.0/12 或 172.18.0.0/16
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
        System.Net.IPAddress.Parse("172.16.0.0"), 12));
});

// ... builder.Build() 之后
app.UseForwardedHeaders(); // ← 必须在 UseRouting / UseAuthentication 之前
```

### 4.2 为什么不用 `ForwardedHeaders.All`

- `X-Forwarded-Host`：当前阶段不需要修改 Host
- `X-Forwarded-For`：需要（正确记录客户端 IP）
- `X-Forwarded-Proto`：需要（正确识别 HTTPS 协议）

### 4.3 为什么必须 Clear 再 Add

ASP.NET Core 默认 `KnownProxies` 和 `KnownNetworks` 包含本地环回地址。但 Docker bridge 网络不是默认信任的。为确保部署兼容性，显式配置比依赖默认值更安全。

---

## 5. 与 Cookie / Auth 的关系

### 5.1 Cookie Secure

当前 session cookie 配置（`Program.cs:42-48`）：

```csharp
options.Cookie.HttpOnly = true;
options.Cookie.SameSite = SameSiteMode.Lax;
```

启用 Forwarded Headers 后：
- `X-Forwarded-Proto: https` 被正确解析 → `Request.IsHttps == true`
- Session cookie 可以设 `Secure` 标记（当前未设置，但在正确的 HTTPS 检测下可以安全启用）

### 5.2 后续生产认证

- OIDC `redirect_uri` 强制 HTTPS
- `LocalPassword` Cookie 也需要 `Secure`
- 必须先有 Forwarded Headers，否则服务器认为永远是 HTTP 请求

---

## 6. 安全注意事项

### 6.1 不可盲目信任任何来源

- **绝对不要**使用 `options.ForwardLimit = null` + 空 `KnownProxies`（这会信任所有 header）
- 如果后续接入 CDN/WAF（如腾讯云 CDN）：
  - 需将 CDN 的回源 IP 段加入 `KnownNetworks`
  - 需确认 CDN 不传递客户端伪造的 header

### 6.2 Header 验证

- ASP.NET Core Forwarded Headers Middleware 本身不做签名验证
- 信任模型完全基于 IP 白名单（`KnownProxies` / `KnownNetworks`）
- 确保网络层面 Nginx 和 ASP.NET 之间没有不可信中间件

---

## 7. 部署前检查清单

部署 Forwarded Headers 前需确认：

- [ ] 确认 Nginx 配置已部署且通过 `nginx -t`
- [ ] 确认 Nginx 设置了所有需要的 proxy header
- [ ] 在容器内执行 `printenv` 确认网络信息
- [ ] 在容器内 `curl -H "X-Forwarded-Proto: https" http://127.0.0.1:5131/api/health` 验证 header 透传
- [ ] 确认 Docker bridge IP 网段并加入 `KnownNetworks`
- [ ] 确认登录后 Cookie Secure 行为和 HTTPS 重定向正确
- [ ] 确认 `/api/auth/me` 返回的 URL 不包含 `127.0.0.1:5131`

---

## 8. 与腾讯云部署的关系

| 场景 | 影响 |
|------|------|
| 无 CDN，直连 Nginx | 信任宿主机 127.0.0.1 即可 |
| 腾讯云 CDN 在前 | 需将 CDN 回源 IP 加入 `KnownNetworks`；需确认 CDN 透传原始 `X-Forwarded-*` |
| 腾讯云 CLB 在前 | CLB 通常会设置 `X-Forwarded-For` 和 `X-Forwarded-Proto` |

---

## 9. 后续实施拆分

| 阶段 | 内容 | 时机 |
|------|------|------|
| R34-c-design | 本文档（设计） | ✅ 当前 |
| R34-c-impl | 在 `Program.cs` 中启用 Forwarded Headers Middleware | 演示冻结解除后 |
| R34-c-verify | 部署环境验证 HTTPS 检测、Cookie Secure、真实 IP | 有部署环境后 |

---

## 10. 决策结论

- ✅ **必须开启** Forwarded Headers Middleware 才能正确识别 HTTPS
- ✅ **只信任** Nginx（和可能的 CDN）来源
- ✅ **不实现**在当前演示冻结期间
- ✅ **实现时**更改 `Program.cs`（仅添加 middleware 配置，不影响现有业务逻辑）
- ⚠️ 部署时需确认 Docker bridge IP 网段并加入 `KnownNetworks`
- ⚠️ 生产认证（R30）依赖 Forwarded Headers 正确工作
