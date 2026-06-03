# 腾讯云 CVM 安全组检查清单

> **R34-f** | 日期：2026-06-03 | 状态：设计文档（未在腾讯云执行）

---

## 1. 说明

本文档为 MathAnalysisAI 部署到腾讯云 CVM 时的安全组配置参考，**当前未在腾讯云执行，不写真实 IP**。

配套文档：
- `Docs/NginxHttpsDesign.md`
- `docs/deploy/nginx/mathanalysis-ai.conf.example`
- `Docs/ForwardedHeadersDesign.md`

---

## 2. 入站规则（Inbound）

### 2.1 必须开放

| 端口 | 协议 | 来源 | 说明 |
|------|------|------|------|
| `80/tcp` | HTTP | `0.0.0.0/0` | HTTP 入口 + ACME challenge |
| `443/tcp` | HTTPS | `0.0.0.0/0` | HTTPS 入口 |

### 2.2 限制开放

| 端口 | 协议 | 来源 | 说明 |
|------|------|------|------|
| `22/tcp` | SSH | **仅限固定管理 IP 或最小网段** | 远程管理。严禁 `0.0.0.0/0` |

管理 IP 示例（仅供示意，不写真实值）：
- 公司 VPN 出口 IP
- 家庭/办公固定 IP
- 跳板机 IP

### 2.3 必须禁止公网入站

| 端口 | 服务 | 风险 |
|------|------|------|
| `1433/tcp` | SQL Server | 数据库直接暴露，极高风险 |
| `4000/tcp` | LiteLLM proxy | API key 暴露 + 可被滥用消费 |
| `5131/tcp` | ASP.NET server | 绕过 Nginx/HTTPS，绕过认证边界 |
| `2375/tcp` | Docker daemon（未加密） | 远程容器控制，极高风险 |
| `2376/tcp` | Docker daemon（TLS） | 同上，即使加密也应限制 |

---

## 3. 出站规则（Outbound）

### 3.1 必须允许

| 目标 | 端口 | 说明 |
|------|------|------|
| `api.deepseek.com` | `443/tcp` | DeepSeek API 调用（通过 LiteLLM） |
| `dashscope.aliyuncs.com` | `443/tcp` | DashScope Qwen-VL OCR API（通过 LiteLLM） |
| `api.nuget.org` | `443/tcp` | NuGet 包还原（构建阶段） |
| `registry-1.docker.io` | `443/tcp` | Docker 镜像拉取与更新 |
| DNS 服务器 | `53/udp` | 域名解析 |

### 3.2 需要时开放

| 目标 | 端口 | 说明 |
|------|------|------|
| 腾讯云镜像仓库 | `443/tcp` | 如使用 TCR 而非 Docker Hub |
| 腾讯云 COS / S3 | `443/tcp` | 如使用云存储备份 `.bak` 文件 |
| Let's Encrypt / ACME | `80/tcp`, `443/tcp` | 证书申请与自动续期（outbound） |
| Ubuntu / Debian 更新源 | `80/tcp`, `443/tcp` | 系统更新 |

---

## 4. 管理安全建议

### 4.1 SSH

- **使用密钥认证**，禁止密码 SSH
```bash
# /etc/ssh/sshd_config
PasswordAuthentication no
PubkeyAuthentication yes
```

- 可进一步收敛：
  - 限制允许登录的用户组（`AllowGroups`）
  - 使用非标准端口降低扫描噪音（不替代密钥）

### 4.2 远程 SQL / LiteLLM 管理

- **不暴露端口到公网**
- 远程管理使用 **SSH Tunnel**：
```bash
# 本地执行：将远程 SQL Server 映射到本地 14333
ssh -L 14333:127.0.0.1:1433 user@your-cvm-ip

# 本地执行：将远程 LiteLLM 映射到本地 4000
ssh -L 4000:127.0.0.1:4000 user@your-cvm-ip
```

### 4.3 文件传输

- `scp` / `rsync` 走 SSH 隧道
- 不开启 FTP / SMB 端口

---

## 5. 服务器端验证命令

部署后应在服务器上验证（不在本地执行）：

```bash
# 查看所有监听端口
ss -tulpn

# 检查 Docker 端口映射
docker compose -f docker-compose.prod.yml ps

# 本地 health check
curl -I http://127.0.0.1:5131/api/health

# 外部 health check（从另一台机器）
curl -I https://your-domain/api/health
```

预期 `ss -tulpn` 输出：
- `:5131` → 仅绑定 `127.0.0.1` 或 Docker 内部网络
- `:4000` → 仅绑定 `127.0.0.1` 或 Docker 内部网络
- `:1433` → 仅绑定 `127.0.0.1` 或 Docker 内部网络
- `:80`, `:443` → Nginx 监听所有接口

---

## 6. 与当前 compose 状态的关系

当前 `docker-compose.prod.yml` 为了本地开发便利，publish 了以下端口：

```
1433:1433   # SQL Server
4000:4000   # LiteLLM
5131:5131   # ASP.NET server
```

这些端口映射在**本地开发机**上是可接受的，但**在生产 CVM 上必须收敛**：

- CVM 安全组层面阻止入站访问这些端口（即使 compose publish 了也无法从公网访问）
- 后续 `R34-d` 建议将 compose 改为仅内网绑定或 `127.0.0.1` 绑定

**注意**：安全组是最后防线，但不是唯一防线。建议 compose 端口也收敛为仅绑定 `127.0.0.1`。

---

## 7. 安全检查清单

部署前逐项确认：

- [ ] `22/tcp` 入站限制到固定 IP（非 `0.0.0.0/0`）
- [ ] SSH 使用密钥认证（`PasswordAuthentication no`）
- [ ] `1433/tcp` 禁止公网入站
- [ ] `4000/tcp` 禁止公网入站
- [ ] `5131/tcp` 禁止公网入站
- [ ] Docker daemon 不暴露到网络
- [ ] `80/tcp` 和 `443/tcp` 开放
- [ ] 出站允许 DeepSeek / DashScope / 系统更新
- [ ] 所有 env 文件权限 `600`（仅服务用户可读）
- [ ] 没有真实 IP / 密码 / key 写入本文档或公开位置

---

## 8. 决策结论

- **当前阶段**：仅文档/清单，不在腾讯云执行
- **部署前**：必须按此清单配置安全组
- **SSH**：强制密钥认证
- **数据库 / LiteLLM / ASP.NET**：不公网暴露，仅内网/127.0.0.1 可达
- **后续**：`R34-d` compose 端口收敛 + `R34-e` 证书申请与续期
