# Nginx + HTTPS 部署设计

## 1. 背景与目标
- 当前项目已经完成本地演示版 MVP，并已进入演示冻结状态。
- 当前公网部署前仍缺少：
  - Nginx 反向代理
  - HTTPS 证书与续期路线
  - Forwarded Headers 信任链
  - Cookie Secure 策略
  - 上传体积限制与公网入口边界
  - LiteLLM / SQL Server 非公网暴露约束
- 本设计目标是为后续 `R34-b` Nginx 配置模板、`R34-c` ASP.NET Forwarded Headers、`R34-d` compose 端口收敛提供统一边界。

## 2. 推荐网络拓扑
### 2.1 路线 A：宿主机 Nginx
- 腾讯云 CVM 宿主机安装 Nginx。
- Nginx 监听：
  - `80/tcp`
  - `443/tcp`
- Nginx 反代到：
  - `http://127.0.0.1:5131`
- `server` 容器对外只需要本机可达，不需要公网直接访问。
- `LiteLLM` 与 `SQL Server` 不通过公网开放。

这是当前阶段的推荐路线，原因：
- 更接近腾讯云 CVM 的常规部署方式。
- 更便于配合 `certbot` 或宿主机证书目录管理。
- 证书续期、Nginx reload、日志排障都更直观。
- 当前仓库中的 `docker-compose.prod.yml` 已同步按此路线收敛为：
  - `127.0.0.1:5131`
  - 便于后续 Nginx 直接反代本机 ASP.NET server

### 2.2 路线 B：Nginx 容器化
- 将 Nginx 作为 compose 内单独服务。
- Nginx 加入 Docker 网络，反代到：
  - `http://server:5131`
- 证书目录通过 volume 挂载。

这条路线可作为后续增强，但当前不作为第一阶段推荐，原因：
- 配置复杂度更高。
- 证书与续期目录挂载更容易出错。
- 当前项目仍在公网前置安全补齐阶段，先用宿主机 Nginx 更稳。

## 3. 端口与安全组策略
### 3.1 公网应开放的端口
- `80/tcp`
- `443/tcp`
- `22/tcp`
  - 应尽量限制到固定管理 IP 或最小范围。

### 3.2 不应公网开放的端口
- `1433/tcp`：SQL Server
- `4000/tcp`：LiteLLM
- `5131/tcp`：ASP.NET server
- Docker daemon 相关端口

### 3.3 与当前 compose 状态的关系
- 当前仓库中的 `docker-compose.prod.yml` 已完成 `R34-d-local`：
  - `127.0.0.1:1433:1433`
  - `127.0.0.1:4000:4000`
  - `127.0.0.1:5131:5131`
- 若服务器运行中的容器仍显示 `0.0.0.0`，说明服务器尚未 `pull` 新代码并 `--force-recreate`。
- 当前仍未完成的，是服务器侧真实生效验证与后续 Nginx/HTTPS 接入。

## 4. Nginx 反向代理设计
### 4.1 目标
- `https://your-domain/` 反代到 ASP.NET server。
- 保留原始 Host。
- 传递真实客户端 IP 与原始协议。
- 支撑 OCR / analyze 的较长请求时间。
- 支撑 PDF 与图片上传。

### 4.2 推荐代理头
- `Host`
- `X-Real-IP`
- `X-Forwarded-For`
- `X-Forwarded-Proto`
- `X-Forwarded-Host`

### 4.3 推荐关键参数
- `client_max_body_size 120m`
  - 当前后端 PDF 上传上限为 100MB，Nginx 需要大于或等于该值。
- `proxy_read_timeout 300s`
  - 如后续 OCR / analyze 响应更慢，可提高到 `600s`。
- `proxy_send_timeout 300s`
- `send_timeout 300s`

### 4.4 关于 buffering
- 第一阶段可保持 Nginx 默认 buffering 行为。
- 若后续出现大响应、流式输出或超时排障需求，再专门评估是否调整 `proxy_buffering`。

## 5. HTTPS / 证书路线
### 5.1 路线比较
#### A. Let's Encrypt + certbot
- 优点：
  - 成本低
  - 自动续期成熟
  - 公网域名常规方案
- 适合：
  - 腾讯云 CVM 直接对外暴露的常规站点

#### B. 腾讯云 SSL 证书
- 优点：
  - 适合已有腾讯云证书管理流程
  - 与腾讯云资源体系更统一
- 缺点：
  - 自动化路径可能取决于后续实际运维方式

#### C. 自签证书
- 只适合：
  - 内网测试
  - 非正式临时验证
- 不适合正式公网访问。

### 5.2 当前推荐
- 公网正式部署：
  - 优先 `Let's Encrypt + certbot`
  - 或腾讯云 SSL 证书
- 不建议长期以 HTTP 明文提供服务。

## 6. HTTP 到 HTTPS 跳转
- `80` 端口主要用于：
  - ACME challenge
  - HTTP 到 HTTPS 跳转
- 普通用户访问 HTTP 时，应跳转到 HTTPS。
- HSTS 可作为后续增强，但在域名、证书与重定向策略稳定之前不建议过早强开。

## 7. ASP.NET Forwarded Headers
- 后续 ASP.NET 需要支持：
  - `X-Forwarded-For`
  - `X-Forwarded-Proto`
- 目标：
  - 正确识别 HTTPS 原始请求
  - 为重定向、Cookie Secure、真实 IP 记录提供基础

### 7.1 设计原则
- 只信任来自 Nginx 的反代来源。
- 不应盲目信任任意客户端自己伪造的 `X-Forwarded-*` header。

### 7.2 后续任务
- `R34-c` 负责在 `Program.cs` 中启用并收紧 Forwarded Headers 配置。
- 本轮只做设计，不改代码。

## 8. Cookie / Auth 策略
- 当前 `Production-safe` 配置下：
  - `Auth__Mode=Disabled`
  - 仅适合健康检查或安全占位
  - 不适合真实用户公网主流程
- 后续生产认证完成后，Cookie 应至少满足：
  - `Secure`
  - `HttpOnly`
  - 合理的 `SameSite`
  - 合理过期时间

结论：
- HTTPS 是生产 Cookie 安全策略的前置条件。
- 在 `LocalPassword` / `Oidc` 未落地前，不建议将主流程真正对公网开放。
- `DevelopmentUsername` 不能用于 Production。

## 9. Health endpoint 暴露策略
- 当前 `/api/health` 是 shallow health：
  - 只确认应用可响应
  - 不检查 DB / LiteLLM / provider
- 该接口可以保留为基础探针。
- 但未来若新增 `/api/health/deep`：
  - 不建议直接公网公开
  - 应考虑 internal/admin 限制或仅内网探测

## 10. LiteLLM / SQL Server 暴露边界
- `LiteLLM:4000` 不应暴露公网。
- `SQL Server:1433` 不应暴露公网。
- 如需远程维护：
  - 优先 SSH tunnel
  - 或 VPN / 内网跳板
- Nginx 只反代 ASP.NET server：
  - 不反代 LiteLLM 管理面
  - 不反代 SQL Server
- Docker daemon socket 不应暴露网络。

## 11. 上传限制与路径安全
- 当前 PDF 上传限制为 100MB。
- Nginx `client_max_body_size` 必须大于或等于后端限制，否则请求会先在 Nginx 被拦截。
- OCR 图片上传也应统一走同一套上传限制。
- 不应将 uploads 目录配置为可浏览静态目录。
- 当前不应开放 PDF 文件的公网直链下载，除非后续单独设计下载权限与签名策略。

## 12. 日志策略
- Nginx 应维护：
  - access log
  - error log
- 不应记录：
  - request body
  - password
  - token
  - key
- URL 中也不应承载敏感信息。
- 日志轮转可在后续 `R34-g` 单独设计。

## 13. 腾讯云部署注意
- 安全组建议只开放：
  - `80`
  - `443`
  - 受限范围的 `22`
- 域名解析使用 A 记录指向 CVM 公网 IP。
- 域名备案、证书归属与地区规则按实际部署场景处理。
- 若证书使用自动续期，需要保证 `80` 端口挑战路径可用。
- 若后续接入 CDN / WAF：
  - 需重新审视真实 IP 传递
  - 需重新定义可信代理来源

## 14. 后续任务拆分
- `R34-b`：Nginx 配置模板
- `R34-c`：ASP.NET Forwarded Headers 配置
- `R34-d`：compose 生产端口收敛
- `R34-e`：证书申请与续期 runbook
- `R34-f`：腾讯云安全组检查清单
- `R34-g`：Nginx 日志与 logrotate

## 15. 决策结论
- 当前阶段推荐：
  - 宿主机 Nginx
  - HTTPS 终止在 Nginx
  - Nginx 反代到 `127.0.0.1:5131`
- SQL Server 与 LiteLLM 不应暴露公网。
- 公网前必须补齐：
  - 生产认证
  - HTTPS
  - Forwarded Headers
  - 端口收敛
- 当前设计只完成方案层；Nginx 配置模板已生成：`deploy/nginx/mathanalysis-ai.conf.example`（R34-b）。真实服务器配置尚未执行。
- Linux 首轮部署试验已确认下一阶段优先级应为：
  - 服务器侧完成 `R34-d` 端口收敛生效验证
  - 随后再做真实 Nginx / HTTPS 接入
