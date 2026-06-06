# Linux 服务器部署试验报告

## 1. 试验目标
- 验证 `MathAnalysisAI.Server` 是否能在 Ubuntu + Docker Compose 环境下完成第一阶段部署闭环。
- 验证 compose 方式下的 `sqlserver` / `litellm` / `server` 三服务能否稳定启动并完成核心主链路。
- 为下一阶段“端口收敛 + Nginx/HTTPS”提供基线。

## 2. 服务器环境
- 操作系统：Ubuntu（服务器）
- 部署方式：Docker Compose
- 主要服务：
  - `mathanalysis-sqlserver`
  - `mathanalysis-litellm`
  - `mathanalysis-server`

## 3. 部署方式
- 使用 `docker-compose.prod.yml` 启动 SQL Server、LiteLLM 与 ASP.NET server。
- 通过服务器本地 env 文件注入 secrets：
  - `/etc/mathanalysis-ai/sqlserver.env`
  - `/etc/mathanalysis-ai/litellm.env`
  - `/etc/mathanalysis-ai/server.env`
- 真实密码、API key、cookie 均保留在服务器本地 env 中，不写入仓库。

## 4. Compose 启动结果
- `docker compose ps` 显示三个容器均为 `Up`。
- `mathanalysis-server` 已进入 `healthy` 状态。
- 本次 Linux 首轮 compose 部署试验已成功跑通。

## 5. SQL migration 结果
- SQL Server 新 volume 场景下已完成数据库迁移。
- 业务数据库 `MathAnalysisAI` 已创建。
- 已确认数据库中存在 20 张表。
- 当前数据库基础结构可支撑登录、分析、统计和课程资料等 MVP 主链路。

## 6. env 配置问题与修复
- 试验过程中曾出现一次 `server.env` 与 `sqlserver.env` 中 SQL 密码不一致的问题。
- 该问题会导致：
  - 登录接口 `500`
  - SQL login failed
- 处理方式：
  - 修正 `server.env` 中连接串密码
  - 重新执行：
    - `docker compose -f docker-compose.prod.yml up -d --force-recreate server`
- 修复后：
  - 登录恢复正常
  - 后续 analyze 与 leaderboard 链路恢复正常

## 7. healthcheck 验证
- `/api/health` 已返回 `200`
- `mathanalysis-server` Docker health 为 `healthy`
- 当前健康检查为 shallow health：
  - 确认应用进程可响应
  - 不检查 DB / LiteLLM / provider 深层可用性

## 8. 登录验证
- `test_student` 登录成功
- session 正常建立
- 当前登录链路在 Linux compose 环境下已验证通过

## 9. LiteLLM / DeepSeek 验证
- LiteLLM 本机直测 `math-reviewer` 已成功
- 说明：
  - LiteLLM 服务正常
  - DeepSeek key 已在服务器本地 env 中生效
  - 文本分析 alias 路由已打通

## 10. analyze 主链路验证
- `/api/learning-analysis/analyze` 已返回 `200`
- 说明 compose 环境下以下链路已跑通：
  - 登录 session
  - ASP.NET server
  - LiteLLM
  - DeepSeek 文本分析
  - 数据落库

## 11. leaderboard 验证
- leaderboard 返回正常
- 已能看到 `test_student` 相关统计更新
- 说明 UserCourseStats / 排行榜链路在 Linux 部署试验中已闭环通过

## 12. 当前安全边界
- 当前 Linux 试验验证的是“服务可运行 + 主链路可用”。
- 当前尚未进入最终公网安全形态。
- Linux 首轮试验时，compose 端口曾绑定到 `0.0.0.0`：

```text
0.0.0.0:1433->1433
0.0.0.0:4000->4000
0.0.0.0:5131->5131
```

- 在安全组未开放这些端口时，短期内仍可控；
- 但它不应作为最终生产部署形态长期保留。
- 当前仓库中的 `docker-compose.prod.yml` 已完成端口收敛修改，改为仅绑定 `127.0.0.1`。
- 服务器侧仍需用户手动 `pull` 新代码并 `--force-recreate` 才会生效。

## 13. 已知问题
- 服务器当前运行中的容器若尚未重建，`1433 / 4000 / 5131` 仍可能保持 `0.0.0.0` 绑定
- Nginx / HTTPS 尚未在服务器真实联调
- Forwarded Headers 仅完成代码与 runbook 级收口，尚未完成真实 Nginx 反代验证
- 当前 `Auth__Mode=Disabled` 仅适用于安全占位 / health，不是正式公网业务登录方案

## 14. 端口收敛目标
当前：

```text
0.0.0.0:1433->1433
0.0.0.0:4000->4000
0.0.0.0:5131->5131
```

目标：

```text
127.0.0.1:1433:1433
127.0.0.1:4000:4000
127.0.0.1:5131:5131
```

公网只应通过 Nginx 暴露：

```text
80/tcp
443/tcp
```

SSH：

```text
22/tcp 仅限管理 IP
```

不应公网开放：

```text
1433/tcp
4000/tcp
5131/tcp
Docker daemon
```

## 15. 端口收敛实施与验证清单
以下为服务器后续手动执行 checklist，本报告不执行真实变更：

1. 在服务器 `pull` 新代码
2. 确认腾讯云安全组未开放 `1433 / 4000 / 5131`
3. 执行：
   - `docker compose -f docker-compose.prod.yml config`
4. 执行：
   - `docker compose -f docker-compose.prod.yml up -d --force-recreate`
5. 执行：
   - `docker compose -f docker-compose.prod.yml ps`
6. 预期端口显示：
   - `127.0.0.1:1433->1433`
   - `127.0.0.1:4000->4000`
   - `127.0.0.1:5131->5131`
7. 验证：
   - `docker compose -f docker-compose.prod.yml ps`
   - `curl -i http://127.0.0.1:5131/api/health`
   - 登录 `test_student`
   - LiteLLM 本机直测
   - analyze 本机测试
8. 确认服务器公网 IP 不能访问 `1433 / 4000 / 5131`
9. 再进入 Nginx / HTTPS 阶段

## 16. 下一步建议
- 下一优先级应为：
  - compose 生产端口收敛
  - Nginx / HTTPS
  - Forwarded Headers 联调
- 在端口收敛完成前，不建议将 `1433 / 4000 / 5131` 视为可公网直连服务

## 17. 结论
- Linux 服务器 compose 首轮部署试验已成功。
- SQL migration、health、login、LiteLLM、analyze、leaderboard 均已验证通过。
- 当前主要剩余风险不是“功能不可用”，而是“公网暴露边界尚未收紧”。
- 下一阶段应进入端口收敛与 Nginx/HTTPS 硬化，而不是继续扩大业务功能范围。
