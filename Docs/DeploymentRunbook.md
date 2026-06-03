# Deployment Runbook（R26 Docker 自动重启方案）

## 1. 目标
- CVM 重启或进程异常后，`sqlserver` / `litellm` / `server` 自动恢复。
- API key 通过服务器本地 env 文件注入，不写入仓库。
- 不把 Docker 容器与 systemd 对同一容器做重复托管。
- SQL Server 备份与恢复设计见：`Docs/SqlBackupDesign.md`。
- SQL Server 恢复脚本安全设计见：`Docs/SqlRestoreScriptDesign.md`。
- SQL Server 手动备份与临时恢复手册见：`Docs/BackupRestoreRunbook.md`。
- SQL Server 定时备份设计见：`Docs/ScheduledBackupDesign.md`。
- Nginx + HTTPS 部署设计见：`Docs/NginxHttpsDesign.md`。

## 2. 生产 compose 模板
- 文件：`docker-compose.prod.yml`
- 服务：
  - `sqlserver`
  - `litellm`
  - `server`
- 每个服务都设置：`restart: unless-stopped`
- SQL Server 使用持久化 volume：`mathanalysis_sql_data:/var/opt/mssql`
- `server` 通过 Dockerfile 本地构建（multi-stage .NET 8）。
- `server` 包含 compose healthcheck（`GET /api/health`）。
- 当前 Nginx / HTTPS 仍未实现，公网部署前需先按 `Docs/NginxHttpsDesign.md` 补齐。

## 3. secrets 文件方案
推荐目录：`/etc/mathanalysis-ai`

```bash
sudo mkdir -p /etc/mathanalysis-ai
sudo chown <service-user>:<service-user> /etc/mathanalysis-ai
sudo chmod 700 /etc/mathanalysis-ai
```

### 3.1 litellm.env（占位示例）
`/etc/mathanalysis-ai/litellm.env`
```bash
LITELLM_MASTER_KEY=sk-local-litellm-master-key
DEEPSEEK_API_KEY=your_deepseek_api_key_here
DASHSCOPE_API_KEY=your_dashscope_api_key_here
```

说明：
- `DEEPSEEK_API_KEY` 供文本分析 alias（`math-reviewer/math-solver/math-hint/math-explainer`）使用。
- `DASHSCOPE_API_KEY` 供 `photo-solution-ocr` 视觉 OCR alias 使用。
- 这两个上游 provider key 只注入 LiteLLM，不进入 ASP.NET `server.env`。

### 3.2 server.env（占位示例）
`/etc/mathanalysis-ai/server.env`
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5131
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=MathAnalysisAI;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true
LLMGateway__Mode=litellm
LiteLLM__BaseUrl=http://litellm:4000/v1/chat/completions
LiteLLM__ApiKey=sk-local-litellm-master-key
Auth__Mode=Disabled
Auth__EnableDevelopmentFallback=false
Auth__EnableDevelopmentMaterialAccessOverride=false
Auth__EnableDevelopmentLegacyAccessOverride=false
Auth__EnableDevelopmentSymbolicAccessOverride=false
```

对比说明：
- 上述是 **production-safe 示例**；
- 若只是本地演示 compose，可改为：
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `Auth__Mode=DevelopmentUsername`
  - 并按本地演示需要决定是否启用 `Auth__EnableDevelopmentFallback=true`
- 但这类配置**不可用于公网 Production**。

### 3.3 sqlserver.env（占位示例）
`/etc/mathanalysis-ai/sqlserver.env`
```bash
ACCEPT_EULA=Y
MSSQL_SA_PASSWORD=YourStrongPassword@123
```

权限建议：
```bash
sudo chmod 600 /etc/mathanalysis-ai/*.env
```

### 3.4 本地 compose 验证时的读权限说明
- `docker compose` 会以当前执行用户读取 `env_file`。
- 若 `/etc/mathanalysis-ai/*.env` 是 `root:wheel` 且 `600`，普通用户执行 compose 可能报 `permission denied`。
- 本地验证建议：
  - 将 env 文件 `chown` 给当前执行 compose 的用户；
  - 保持 `chmod 600`。
- 生产服务器建议：
  - `chown` 到部署用户/服务用户；
  - 保持最小权限原则。

## 4. 构建、启动与检查
在项目目录执行：
```bash
docker compose -f docker-compose.prod.yml config
docker compose -f docker-compose.prod.yml build server
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f
docker compose -f docker-compose.prod.yml logs -f server
```

生产前、迁移前、演示前、升级前，建议先执行一次手动数据库备份：
```bash
MSSQL_SA_PASSWORD='***' ./scripts/backup-sqlserver.sh
```

说明：
- 该脚本只备份 `MathAnalysisAI` 数据库；
- 不备份 `/etc/mathanalysis-ai/*.env`；
- 生成的 `.bak` 文件不要进入 Git。
- 生产恢复前必须优先阅读 `Docs/SqlRestoreScriptDesign.md`；
- 默认恢复策略应优先恢复到临时数据库名，而不是直接覆盖主库。
- 生产部署前建议至少执行一次“手动备份 + 临时恢复验证”，流程见 `Docs/BackupRestoreRunbook.md`。
- 生产环境后续建议优先采用 `systemd timer` 做定时备份，当前仍仅完成设计，未实现。

临时库恢复演练命令（phase 1）：
```bash
MSSQL_SA_PASSWORD='***' \
BACKUP_FILE='/path/to/MathAnalysisAI_YYYYMMDD_HHMMSS.bak' \
./scripts/restore-sqlserver.sh
```

说明：
- 当前 restore 脚本只恢复到临时数据库；
- 当前不支持覆盖 `MathAnalysisAI`；
- 生产恢复前建议先恢复到临时库完成验证。

若修改了 `/etc/mathanalysis-ai/server.env` 或 `/etc/mathanalysis-ai/litellm.env`：
```bash
docker compose -f docker-compose.prod.yml up -d --force-recreate server
docker compose -f docker-compose.prod.yml up -d --force-recreate litellm
```

说明：
- `env_file` 内容变化后，普通 `docker compose restart` 不会重新注入环境变量。
- 需要 `--force-recreate` 让新容器读取更新后的 env。

本地验证结果（已完成）：
- `docker compose -f docker-compose.prod.yml config` 通过。
- `docker compose -f docker-compose.prod.yml build server` 通过。
- 已生成镜像：`mathanalysis-server:latest`。
- `docker compose -f docker-compose.prod.yml up -d` 已通过。
- `sqlserver` / `litellm` / `server` 均已实测 `Running`。
- `curl -i http://localhost:5131/api/health` 已返回 `200` 与 `status=ok`。
- `docker inspect --format='{{json .State.Health}}' mathanalysis-server` 已实测为 `healthy`。
- compose SQL 新 volume 场景下，已完成数据库迁移、登录/session、LiteLLM 直测与 analyze 成功闭环验证。

说明：
- `server` 镜像不包含真实 key。
- key 始终由 `/etc/mathanalysis-ai/server.env` 注入。
- `DEEPSEEK_API_KEY` / `DASHSCOPE_API_KEY` 只存在于 `/etc/mathanalysis-ai/litellm.env`。
- `server.env` 只负责 ASP.NET 侧运行参数，如 `LiteLLM__ApiKey`、`LiteLLM__BaseUrl`、数据库连接串与环境开关。
- Production-safe 示例中必须显式设置 `Auth__Mode`，不能留空。
- 当前若 `ASPNETCORE_ENVIRONMENT=Production` 且：
  - `Auth__Mode=DevelopmentUsername`
  - 或任一 Development fallback / override 为 `true`
  - 应用会在启动时 fail-fast 拒绝启动。
- `Auth__Mode=Disabled` 当前可用于“生产安全配置占位”或仅健康检查场景；
  但它**不应被视为真实业务登录方案**。
- `LiteLLM__BaseUrl` 当前必须填写为完整 OpenAI-compatible endpoint，而不是仅主机根地址：
  - 正确：`http://litellm:4000/v1/chat/completions`
  - 错误：`http://litellm:4000`
- Compose 网络内：
  - SQL Server 主机名用 `sqlserver`（不是 `localhost`）
  - LiteLLM 主机名用 `litellm`（不是 `localhost`）
- 安全提示：`docker compose config` 会展开 `env_file` 内容。真实 key 场景不要将其输出粘贴到聊天、公开日志或 issue。

## 4.1 首次部署数据库迁移
首次使用新 SQL Server volume 时，容器内通常只有系统库（`master/tempdb/model/msdb`），还没有 `MathAnalysisAI` 业务库。

需要先执行数据库迁移：
```bash
dotnet ef database update --connection "Server=localhost,1433;Database=MathAnalysisAI;User Id=sa;Password=YourStrongPassword@123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
```

已验证可成功应用当前迁移链：
- `Init`
- `InitialCreate`
- `AddLearningPlatformSchema`
- `AddMathAnalysisSeedData`
- `AddCourseMaterialKnowledgeBase`

完成后再验证：
- `test_student` 登录
- LiteLLM 直测 `math-reviewer`
- `/api/learning-analysis/analyze`

## 4.2 DeepSeek 401 鉴权排查
- 若前端“开始分析”报 `DeepseekException - Authentication Fails` / `code=401`：
  - 先确认 `/etc/mathanalysis-ai/litellm.env` 中的 `DEEPSEEK_API_KEY` 已替换为真实有效 key；
  - 确认修改后已重新创建或重启 LiteLLM 容器；
  - 若日志中报错包含 `Received Model Group=math-reviewer`，说明 ASP.NET -> LiteLLM 的 alias 路由正常，失败点在上游 DeepSeek 鉴权；
  - 若 LiteLLM 仍像在使用占位值，检查 `litellm.env` 是否仍保留 `placeholder`。

## 4.3 常见错误排查
- `Unsafe auth configuration for Production`：
  - 原因：`Auth__Mode` 为空、为 `DevelopmentUsername`，或某个 Development fallback / override 为 `true`
  - 处理：改为 production-safe 配置，并 `docker compose -f docker-compose.prod.yml up -d --force-recreate server`
- `Cannot open database "MathAnalysisAI"`：
  - 原因：新 SQL volume 还未执行业务迁移。
  - 处理：先执行 `dotnet ef database update --connection ...`
- `DeepseekException - Authentication Fails` / `code=401`：
  - 原因：`DEEPSEEK_API_KEY` 无效、未注入 LiteLLM、或 LiteLLM 未在换 key 后重建/重启。
- `Method Not Allowed`：
  - 原因：`LiteLLM__BaseUrl` 不是完整 endpoint。
  - 当前代码要求：`LiteLLM__BaseUrl=http://litellm:4000/v1/chat/completions`
- `docker compose config` 泄露 env：
  - 该命令会展开 `env_file` 内容，真实 key 不要贴到聊天、日志或 issue。

## 4.1 Healthcheck 说明（当前为浅检查）
- 后端 endpoint：`GET /api/health`
- 返回示例结构：
  - `status`
  - `service`
  - `timestampUtc`
  - `environment`
- 当前只检查应用进程可响应，不检查：
  - 数据库连通性
  - LiteLLM 连通性
  - provider alias 可用性
- 容器刚启动时，healthcheck 可能在启动窗口期短暂失败（例如前 1-2 次连接失败）。
- 只要后续转为 `healthy`，通常可视为正常启动过程。

验收命令：
```bash
docker compose -f docker-compose.prod.yml ps
curl -i http://localhost:5131/api/health
docker inspect --format='{{json .State.Health}}' mathanalysis-server
```

登录 + analyze 验证（compose 环境）：
```bash
curl -i -c /tmp/mathauth.cookie -X POST http://localhost:5131/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test_student"}'
```

```bash
curl -b /tmp/mathauth.cookie http://localhost:5131/api/auth/me
```

```bash
curl -i -b /tmp/mathauth.cookie -X POST http://localhost:5131/api/learning-analysis/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 200,
    "chapterId": 307,
    "problemText": "判断反常积分 ∫_1^∞ 1/x^2 dx 是否收敛。",
    "studentSolutionText": "因为 1/x^2 趋于 0，所以积分收敛。",
    "analysisMode": "review_solution",
    "userId": 1
  }'
```

## 5. 自动重启恢复链路
服务器重启后：
1. `systemd` 拉起 `docker` daemon（需 `systemctl enable docker`）
2. Docker 读取已有容器与 `restart: unless-stopped` 策略
3. 自动重启 `sqlserver` / `litellm` / `server`
4. `env_file` 再次注入 secrets
5. 服务恢复

开启 Docker 开机自启：
```bash
sudo systemctl enable docker
sudo systemctl start docker
```

重启后验证：
```bash
docker compose -f docker-compose.prod.yml ps
curl http://localhost:5131/
```

## 6. systemd 示例（可选）
### 6.1 推荐做法
- systemd 只管理 `docker` daemon（推荐）或只管理整个 compose project。
- 不要同时对同一容器：
  - 设 `restart: unless-stopped`
  - 又写单独 systemd 容器服务
  否则容易出现重启冲突。

### 6.2 如果要用 systemd 管 compose project（可选）
`/etc/systemd/system/mathanalysis-compose.service`
```ini
[Unit]
Description=MathAnalysis Compose Stack
Requires=docker.service
After=docker.service network.target

[Service]
Type=oneshot
WorkingDirectory=/opt/mathanalysis
RemainAfterExit=yes
ExecStart=/usr/bin/docker compose -f docker-compose.prod.yml up -d
ExecStop=/usr/bin/docker compose -f docker-compose.prod.yml down

[Install]
WantedBy=multi-user.target
```

## 7. Docker Compose env_file 要点
- `litellm` 使用：`/etc/mathanalysis-ai/litellm.env`
- `server` 使用：`/etc/mathanalysis-ai/server.env`
- `sqlserver` 使用：`/etc/mathanalysis-ai/sqlserver.env`
- `infra/litellm/config.yaml` 继续使用 `os.environ/DEEPSEEK_API_KEY` / `os.environ/DASHSCOPE_API_KEY`，不写真实 key。

## 8. 注意事项
- `restart policy` 不等于健康检查。
- 服务“假活”后续要补 healthcheck（见后续任务）。
- SQL Server 必须使用 volume 持久化。
- 生产环境关闭 Development fallback/override。
- 不提交任何 `.env` 到 Git。
- 不打印完整环境变量，不截图暴露 key。
- key 泄漏后立即轮换。
- 当前生产镜像未内置 Python/SymPy，`/api/symbolic/compute` 可能返回 `worker_unavailable`。
  - 后续可单独做 `R26-symbolic-container` 增强镜像。

## 9. 多 key 说明
- 单 key 通常可并发，但受 RPM/TPM/余额/风控限制。
- 开发期一个 key 通常够用。
- 内测可准备 backup key。
- 生产阶段应做限流、配额、队列、fallback、费用监控，而不是只堆 key。
- “两个 DeepSeek 同题推理”应作为质量增强链路单独设计，不与普通负载均衡混用。

## 10. 后续任务建议
- `R26-healthcheck-deep`：增加 `/api/health/deep`（DB / LiteLLM / provider 细分状态）
- `R26-backup`：SQL Server 定时备份
- `R26-nginx-https`：Nginx + HTTPS
- `R26-log-rotate`：日志与磁盘空间治理
