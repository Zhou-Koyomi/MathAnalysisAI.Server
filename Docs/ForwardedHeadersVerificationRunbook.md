# Forwarded Headers 部署验证 Runbook

> **R34-c-verify** | 日期：2026-06-04 | 状态：待 Nginx 部署环境

---

## 1. 前置条件

- [ ] Nginx 已安装并按 `deploy/nginx/mathanalysis-ai.conf.example` 配置
- [ ] 域名已 DNS 解析到服务器
- [ ] 证书已申请并部署（Let's Encrypt 或腾讯云）
- [ ] `server.env` 已配置 `ForwardedHeaders__KnownNetworks`（Docker bridge 网段）

## 2. 部署时确认的配置项

### 2.1 Docker bridge IP 网段

```bash
# 在服务器上确认 Docker bridge 网段
docker network inspect bridge | grep Subnet
# 常见输出：172.17.0.0/16 或 172.18.0.0/16
```

然后在 `server.env` 中配置：
```bash
ForwardedHeaders__KnownNetworks=172.16.0.0/12
```

### 2.2 验证 Forwarded Headers

```bash
# 从外部通过 HTTPS 访问
curl -I https://your-domain/api/health

# 在 ASP.NET 日志中确认是否识别 HTTPS
# 预期：日志中的 scheme 应为 https，不是 http
```

## 3. 验证清单

- [ ] `/api/health` 可通过 HTTPS 访问
- [ ] HTTP 请求自动跳转 HTTPS（301）
- [ ] Cookie `Secure` 标记可正确设置（当 ForwardedHeaders 识别 HTTPS 后）
- [ ] 客户端 IP 在日志中正确记录（不全是 127.0.0.1）
- [ ] LiteLLM 4000 端口外网不可达
- [ ] SQL Server 1433 端口外网不可达
- [ ] ASP.NET 5131 端口外网不可达

## 4. 当前不验证

- 不安装 Nginx
- 不申请证书
- 不配置云安全组
- 不修改 docker-compose.prod.yml 端口绑定

---

*此 runbook 为部署准备文档，当前不会在本地执行。*
