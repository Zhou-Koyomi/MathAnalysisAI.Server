# SQL Server 备份与恢复设计

## 1. 背景与目标
- 当前项目已经完成本地演示版 MVP，并进入演示冻结状态。
- 当前 SQL Server 运行在 Docker Compose 中：
  - 容器：`mathanalysis-sqlserver`
  - 镜像：`mcr.microsoft.com/mssql/server:2022-latest`
  - 数据库：`MathAnalysisAI`
  - volume：`mathanalysis_sql_data:/var/opt/mssql`
- 当前全项目审计中，数据库备份与恢复闭环缺失是最重要的高风险项之一。

本设计目标：
1. 明确当前数据库的备份对象与不备份对象。
2. 设计手动 `.bak` 备份与恢复方案。
3. 为后续自动化脚本、定时任务、云端备份和恢复演练提供路线。
4. 在不改业务代码、不执行真实备份的前提下，把规则和操作边界先定清楚。

执行手册见：
- `Docs/BackupRestoreRunbook.md`
- `Docs/ScheduledBackupDesign.md`

## 2. 当前数据库环境
### 2.1 基础环境
- SQL Server 容器名：`mathanalysis-sqlserver`
- SQL Server Compose 服务名：`sqlserver`
- 数据库名：`MathAnalysisAI`
- SQL Server 数据目录 volume：
  - `mathanalysis_sql_data:/var/opt/mssql`

### 2.2 连接方式
本地宿主机连接：
```text
Server=localhost,1433;Database=MathAnalysisAI;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true
```

Compose 网络内连接：
```text
Server=sqlserver,1433;Database=MathAnalysisAI;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true
```

### 2.3 当前主要业务数据
当前数据库包含的核心数据包括：
- 用户与权限：
  - `AppUsers`
- 课程与知识点：
  - `Course`
  - `Chapter`
  - `KnowledgePoint`
- 分析主链路：
  - `Problem`
  - `StudentSolution`
  - `AnalysisResult`
  - `MistakeRecord`
- 学习统计：
  - `UserCourseStats`
  - `UserKnowledgeState`
- 模型调用日志：
  - `LLMRequestLog`
- 课程资料知识库：
  - `CourseMaterial`
  - `MaterialChunk`
  - `MaterialChunkKnowledgePoint`

这些数据都属于备份对象。

## 3. 风险分析
### 3.1 当前风险
- 执行 `docker compose down -v` 会删除 SQL Server volume。
- 本机磁盘损坏、宿主机损坏或 Docker 数据目录损坏会导致数据库丢失。
- 误迁移、误清表、误删课程资料都会影响业务数据完整性。
- 若只“有备份文件”但没有恢复演练，备份本身并不可靠。

### 3.2 典型风险场景
- 演示前误删 volume，导致 `MathAnalysisAI` 业务库消失。
- 迁移前误操作，导致表结构或数据破坏。
- 本地 compose 升级或清理 Docker 时误删 volume。
- 生产环境中宿主机异常，但没有外部 `.bak` 备份副本。

### 3.3 风险结论
- 当前 volume 持久化只能解决“容器重启不丢数据”，不能替代备份。
- 备份策略与恢复演练必须在正式部署前补齐。

## 4. 备份对象与不备份对象
### 4.1 备份对象
备份目标是业务数据库：
- `MathAnalysisAI`

包括其中的所有业务表与数据：
- 用户
- 课程
- 题目
- 解答
- 分析结果
- 统计
- 课程资料
- 检索 chunk
- 模型日志

### 4.2 不备份对象
以下内容不属于数据库备份对象：
- `/etc/mathanalysis-ai/*.env`
- DeepSeek / DashScope / LiteLLM key
- `docker compose config` 展开输出
- 宿主机上的 `.env.local`
- LiteLLM 配置中的真实 secrets
- 代码仓库文件本身

说明：
- 数据库备份只负责业务数据恢复；
- secrets 应通过独立的 secrets 管理方案维护，不应混入 `.bak` 备份设计。

## 5. 手动备份方案
### 5.0 当前实施状态（R33-b）
- 已新增手动备份脚本：
  - `scripts/backup-sqlserver.sh`
- 该脚本当前职责：
  - 检查 Docker 与 SQL Server 容器状态
  - 检查数据库是否存在
  - 在容器内执行 `BACKUP DATABASE`
  - 将 `.bak` 复制到宿主机备份目录
  - 输出宿主机备份文件路径与大小
- 当前仍未实现：
  - 覆盖主库 restore 脚本（phase 1 临时库恢复脚本已实现，见 `scripts/restore-sqlserver.sh`）
  - 定时备份
  - 云端上传

脚本示例用法：
```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
./scripts/backup-sqlserver.sh
```

可选环境变量：
- `SQL_CONTAINER_NAME`，默认 `mathanalysis-sqlserver`
- `SQL_DATABASE_NAME`，默认 `MathAnalysisAI`
- `BACKUP_HOST_DIR`，默认 `~/Backups/mathanalysis-ai`
- `MSSQL_SA_PASSWORD`，必填，由环境变量提供
- `BACKUP_KEEP_DAYS`，当前仅预留，暂未实现自动清理

### 5.1 推荐备份目录
容器内建议目录：
- `/var/opt/mssql/backups`

宿主机建议目录：
- 生产 Linux：
  - `/var/backups/mathanalysis-ai`
- 本地 Mac：
  - `~/Backups/mathanalysis-ai`
  - 或 `/Users/night_creek/Backups/mathanalysis-ai`

建议：
- 备份目录必须在 Git 工作区之外；
- 备份文件不得进入仓库；
- 备份目录权限应限制为当前运维用户可读写。

### 5.2 方案 A：容器内执行 `BACKUP DATABASE`
推荐通过 SQL Server 原生 `.bak` 备份：

```sql
BACKUP DATABASE [MathAnalysisAI]
TO DISK = N'/var/opt/mssql/backups/MathAnalysisAI-20260603-120000.bak'
WITH INIT, COPY_ONLY, COMPRESSION, STATS = 10;
```

特点：
- 使用 SQL Server 原生备份格式；
- 便于后续恢复；
- 可以直接在容器内完成数据库快照。

文档示例命令（仅示例，不在本轮执行）：
```bash
docker exec -it mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P '***' \
  -Q "BACKUP DATABASE [MathAnalysisAI] TO DISK = N'/var/opt/mssql/backups/MathAnalysisAI-YYYYMMDD-HHMMSS.bak' WITH INIT, COPY_ONLY, COMPRESSION, STATS = 10"
```

注意：
- SQL Server 容器内必须存在 `/var/opt/mssql/backups`，且 SQL Server 进程有权限写入。
- 文件名不要包含密码、key、cookie 等敏感信息。

### 5.3 方案 B：将 `.bak` 复制到宿主机
备份写入容器后，再复制到宿主机目录：

```bash
docker cp mathanalysis-sqlserver:/var/opt/mssql/backups/MathAnalysisAI-YYYYMMDD-HHMMSS.bak ~/Backups/mathanalysis-ai/
```

说明：
- 容器内 `.bak` 不应作为唯一副本；
- 至少应复制到宿主机安全目录；
- 后续可继续扩展到对象存储。
- 当前 `scripts/backup-sqlserver.sh` 默认在成功复制到宿主机后删除容器内临时 `.bak`，避免容器内长期堆积备份文件。

### 5.4 备份命名建议
建议格式：
```text
MathAnalysisAI-YYYYMMDD-HHMMSS.bak
```

例如：
```text
MathAnalysisAI-20260603-235500.bak
```

建议不要在文件名中包含：
- 用户名
- 密码
- key
- 主机详细信息

## 6. 手动恢复方案
### 6.1 恢复原则
- 恢复操作必须是手动确认动作；
- 默认不能自动覆盖现有数据库；
- 恢复前要确认目标数据库是否允许被覆盖；
- 恢复时应尽量先停止应用写入。

### 6.2 推荐恢复顺序
1. 停止 `server` 容器，避免写入：
   - 保持 `sqlserver` 运行
2. 将 `.bak` 放入 SQL Server 容器可访问路径
3. 确认是否需要覆盖现有 `MathAnalysisAI`
4. 若恢复到同名数据库：
   - 可能需要 `SINGLE_USER`
   - 恢复后再切回 `MULTI_USER`
5. 执行 `RESTORE DATABASE`
6. 启动或重启 `server`
7. 检查 `/api/health`
8. 登录 `test_student`
9. 运行固定 analyze 样例验证

### 6.3 同名库恢复示例思路
恢复到同名数据库时，典型步骤为：

```sql
ALTER DATABASE [MathAnalysisAI] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE [MathAnalysisAI]
FROM DISK = N'/var/opt/mssql/backups/MathAnalysisAI-YYYYMMDD-HHMMSS.bak'
WITH REPLACE, RECOVERY, STATS = 10;

ALTER DATABASE [MathAnalysisAI] SET MULTI_USER;
```

说明：
- `WITH REPLACE` 有覆盖风险，必须人工确认；
- 不建议把“覆盖恢复”写成默认自动化脚本行为。

### 6.4 临时库恢复演练
更安全的恢复演练方式是恢复到临时数据库名，例如：
- `MathAnalysisAI_RestoreTest`

这样可以：
- 不覆盖主库；
- 先验证 `.bak` 可用性；
- 再决定是否对主库执行恢复。

restore 脚本的默认安全策略与覆盖主库保护规则，见：
- `Docs/SqlRestoreScriptDesign.md`

phase 1 临时库恢复示例：
```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
BACKUP_FILE='/path/to/MathAnalysisAI_YYYYMMDD_HHMMSS.bak' \
./scripts/restore-sqlserver.sh
```

当前状态：
- `backup-sqlserver.sh` 已真实备份验证
- `restore-sqlserver.sh` phase 1 已真实临时库恢复验证

## 7. 备份频率与保留策略
### 7.1 本地开发
- 重大变更前手动备份一次
- migration 前手动备份一次
- 资料批量导入前手动备份一次

### 7.2 本地演示 / Demo
- 演示前手动备份一次
- 演示重要数据准备完成后再固定留一份

### 7.3 内测
- 每日全量备份一次
- 保留最近 7 天
- 每周保留 1 份，保留 4 周

### 7.4 生产
- 每日全量备份一次
- 后续可再设计差异备份 / 日志备份
- 至少保留一份独立于宿主机的数据副本

### 7.5 保留策略建议
- dev：最近 3-5 份
- demo：演示前固定 1 份 + 最近 1-2 份
- internal beta：7 daily + 4 weekly
- production：30 daily + 12 monthly（可按成本调整）

## 8. 云端 / 腾讯云备份路线
### 8.1 当前建议路线
第一阶段：
- 先生成本地 `.bak`
- 再复制到宿主机安全目录

第二阶段：
- 上传到腾讯云 COS / 对象存储

### 8.2 安全原则
- 备份上传用到的密钥不进入仓库；
- 备份上传 credentials 不写进脚本明文；
- 可后续考虑加密 `.bak`；
- 上传失败应有日志，后续可再扩展告警。

### 8.3 当前不做的事
本轮不设计：
- 自动上传实现细节
- 云端生命周期策略
- 备份加密实现
- 告警渠道实现

这些留给后续 `R33-e`。

## 9. 自动化实施路线
本轮只设计，不实现。推荐后续拆分：
- `R33-b`：手动 backup script
- `R33-c`：restore script
- `R33-d`：cron / systemd timer 自动执行
- `R33-e`：腾讯云 COS 上传
- `R33-f`：恢复演练 runbook
- `R33-g`：备份健康检查

建议顺序：
1. 先做 `R33-b` 手动备份脚本
2. 再做 `R33-c` 恢复脚本
3. 然后做 `R33-f` 恢复演练
4. 最后才做定时和云上传

## 10. 安全与隐私要求
- `.bak` 可能包含：
  - 用户数据
  - 题目与解答
  - 分析结果
  - 学习统计
  - 课程资料与 chunk
  - 模型日志
- `.bak` 不得提交到 Git
- `.bak` 不得直接粘贴到聊天
- 备份目录权限必须限制
- 备份文件命名不要包含 secret
- 备份文件本身不包含 API key，但仍包含个人学习数据，必须视为敏感数据
- 恢复测试不得默认覆盖生产库

## 11. 恢复演练方案
### 11.1 最小恢复演练
推荐最小恢复演练：
1. 在本地准备一份 `.bak`
2. 恢复到临时数据库名或临时 volume 环境
3. 检查核心表存在：
   - `AppUsers`
   - `AnalysisResults`
   - `UserCourseStats`
   - `CourseMaterials`
4. 检查 `test_student` 是否存在
5. 检查最近分析结果是否存在
6. 跑固定 analyze 样例
7. 确认系统基本可用

### 11.2 演练原则
- 不要求每次都恢复主库
- 优先恢复到临时库做验证
- 恢复演练必须形成记录，不能只“备份了但没试过”

## 12. 常见错误
- `Cannot open backup device`
  - 备份路径不存在或权限不对
- `Exclusive access could not be obtained`
  - 目标数据库仍有连接，需 `SINGLE_USER`
- `The backup set holds a backup of a database other than the existing database`
  - 可能需要检查目标数据库名或使用 `WITH REPLACE`
- `RESTORE DATABASE is terminating abnormally`
  - `.bak` 文件损坏、路径错误或版本兼容问题
- 恢复后应用异常
  - 需要重新检查 `/api/health`
  - 检查登录与 analyze 固定样例

## 13. 后续任务拆分
- `R33-a`：SQL backup 设计
- `R33-b`：手动 backup script（已完成）
- `R33-c`：restore script design（已完成）
- `R33-c-impl`：restore script phase 1（临时库恢复）实现（已完成）
- `R33-d`：定时备份
- `R33-e`：COS / 对象存储上传
- `R33-f`：恢复演练 runbook
- `R33-g`：备份健康检查

## 14. 决策结论
- 当前数据库必须建立 `.bak` 备份与恢复闭环。
- 备份对象是 `MathAnalysisAI` 业务数据库，而不是 secrets 文件。
- 推荐先落地“手动 `.bak` + 宿主机保存”的最小可靠方案。
- 演示前允许做备份，且不视为扩功能。
- 演示前禁止 `docker compose down -v`。
- 正式部署前，至少应具备：
  - 手动备份能力
  - 手动恢复能力
  - 一次恢复演练记录
