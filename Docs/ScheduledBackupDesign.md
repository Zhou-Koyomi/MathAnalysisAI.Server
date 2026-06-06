# SQL Server 定时备份设计

## 1. 背景与目标
- 当前项目已经形成最小手动备份恢复闭环：
  - `scripts/backup-sqlserver.sh`
  - `scripts/restore-sqlserver.sh` phase 1
  - 临时库只读验证
  - 手动清理临时恢复库
- 当前仍缺：
  - 定时备份
  - 云端异地备份
  - 备份健康检查
  - 定期恢复演练机制

本设计目标：
- 避免只依赖人工记忆做备份；
- 降低误删 volume、主机故障、迁移误操作造成的数据不可恢复风险；
- 为后续 Linux / 腾讯云部署建立基础运维闭环。

当前阶段目标：
- 第一阶段只做本机或服务器上的**定时全量备份设计**
- 不做云端上传
- 不做差异备份 / 日志备份
- 不做覆盖恢复自动化

## 2. 当前手动闭环
当前已具备：
- 手动备份：
  - `scripts/backup-sqlserver.sh`
- 手动恢复到临时库：
  - `scripts/restore-sqlserver.sh`
- 手动执行手册：
  - `Docs/BackupRestoreRunbook.md`

当前边界：
- 已能手动生成 `.bak`
- 已能恢复到临时数据库并做只读验证
- 仍不支持覆盖主库
- 仍不支持自动定时执行

## 3. cron 与 systemd timer 比较
### 3.1 方案 A：cron
优点：
- 简单
- 几乎所有 Linux 都可用
- 上手快

缺点：
- 日志管理较弱
- 环境变量与工作目录容易出错
- 服务状态、失败上下文不如 systemd 清晰
- 并发控制需要自行补齐

适用：
- 简单备用方案
- 临时环境
- 管理要求不高的单机场景

### 3.2 方案 B：systemd timer
优点：
- 更适合长期运行的 Linux 服务器
- 日志进入 `journalctl`
- 可搭配 `EnvironmentFile`
- 可明确声明 `WorkingDirectory`
- 更容易配合 `flock` 做并发控制
- 失败行为和运行用户更清晰

缺点：
- 配置稍复杂
- 不适用于本地 macOS 日常开发

适用：
- 腾讯云 Linux
- 长期运维
- 生产 / 内测服务器

### 3.3 推荐结论
- 本地 Mac：
  - 不强制定时
  - 继续以手动备份为主
- 腾讯云 Linux：
  - **优先推荐 systemd timer**
- cron：
  - 作为简单备用方案
  - 不建议作为生产主方案

## 4. secrets 与环境变量注入
### 4.1 基本原则
- 定时任务不能把 `MSSQL_SA_PASSWORD` 写死在脚本中
- `backup-sqlserver.sh` 继续保持“只从环境变量接收密码”
- 定时任务负责注入环境变量，不让脚本自行读取 secrets 文件

### 4.2 生产 Linux 推荐做法
- 使用：
  - `/etc/mathanalysis-ai/sqlserver.env`
- 由 systemd service 注入：
  - `EnvironmentFile=/etc/mathanalysis-ai/sqlserver.env`

### 4.3 变量命名一致性
推荐直接保持：
- `MSSQL_SA_PASSWORD=...`

这样定时任务无需做额外映射，就能直接调用：
- `scripts/backup-sqlserver.sh`

### 4.4 权限要求
- `/etc/mathanalysis-ai/sqlserver.env`
  - 权限建议：`600`
- 由运行定时任务的用户可读
- 不能在日志中输出密码值

## 5. 备份目录与权限
### 5.1 推荐目录
生产 Linux：
- `/var/backups/mathanalysis-ai`

本地 Mac：
- `~/Backups/mathanalysis-ai`

### 5.2 权限要求
- 目录 owner 应为运行定时任务的用户
- 权限建议：
  - `700`
  - 或至少不公开可读

### 5.3 额外要求
- `.bak` 不进入 Git
- 备份目录不要放在项目仓库内部
- 如果后续做日志文件，也应放到受限目录

## 6. 备份频率
### 6.1 本地开发
- 不自动定时
- 重大变更前手动备份

### 6.2 本地演示
- 演示前手动备份
- 不强制定时

### 6.3 内测
- 每日凌晨 1 次全量备份
- 保留最近 7 天 daily
- 每周保留 4 份 weekly

### 6.4 生产
- 每日全量备份
- 至少保留 30 份 daily
- 后续可再补 12 份 monthly
- 至少保留一份异地副本（后续阶段实现）

## 7. 保留与清理策略
### 7.1 当前脚本状态
- `backup-sqlserver.sh` 当前仅预留了 `BACKUP_KEEP_DAYS`
- 尚未实现自动清理

### 7.2 推荐路线
定时备份第一版建议：
- **先只做定时备份，不自动清理**

原因：
- 自动清理属于高风险动作
- 在恢复演练机制稳定前，不应默认删除旧备份

### 7.3 后续清理设计建议
后续可单独设计：
- `R33-cleanup-design`

若未来实现清理，建议：
- 只清理符合命名模式的 `.bak`
- 不删除非脚本生成文件
- 先支持 dry-run
- 显式 `CLEANUP_CONFIRM=true` 才执行删除

## 8. 并发控制
必须避免多个备份任务同时运行。

推荐：
- 在 Linux systemd service 中用 `flock`
- 若已有任务运行，新任务应退出并写日志

设计原则：
- 不允许并发写同一备份文件
- 不允许多个定时任务同时进入备份流程

## 9. 日志策略
### 9.1 systemd timer 路线
- 通过 `journalctl` 查看日志
- 脚本输出建议包括：
  - 开始时间
  - 数据库名
  - 备份文件路径
  - 文件大小
  - 成功 / 失败

### 9.2 不应输出的内容
- 密码
- API key
- cookie
- `docker compose config`

### 9.3 可选本地日志文件
可选写入：
- `/var/log/mathanalysis-ai/backup.log`

但第一阶段不是必需；若启用，也应限制目录权限。

## 10. 失败处理与告警
### 10.1 第一阶段
- 只记录失败日志
- 不做外部告警
- 不调用外部 API

### 10.2 后续阶段
后续可在 `R33-g` 中加入：
- backup healthcheck
- 失败告警
- 邮件 / 企业微信 / 监控系统对接

### 10.3 常见失败
- Docker daemon 未运行
- SQL 容器未 running
- 密码错误
- `BACKUP_HOST_DIR` 无权限
- 磁盘空间不足
- `sqlcmd` 不存在
- `docker cp` 失败

## 11. 定期恢复演练
必须强调：
- 只有备份，没有恢复演练，不算可靠备份

建议：
- 内测：
  - 每周一次临时恢复演练
- 生产：
  - 每月一次临时恢复演练

演练方式：
- 继续使用 `restore-sqlserver.sh`
- 恢复到临时数据库
- 验证核心表
- 验证后手动清理临时库
- 不自动覆盖主库

## 12. systemd timer 草案
本轮只写设计，不创建真实文件。

建议两个文件：
- `mathanalysis-backup.service`
- `mathanalysis-backup.timer`

### 12.1 service 要点
- 调用：
  - `scripts/backup-sqlserver.sh`
- 使用：
  - `EnvironmentFile=/etc/mathanalysis-ai/sqlserver.env`
- 设置：
  - `BACKUP_HOST_DIR=/var/backups/mathanalysis-ai`
- 设置：
  - `WorkingDirectory=/path/to/MathAnalysisAI.Server`
- 使用：
  - `flock`

### 12.2 timer 要点
- 每日凌晨运行
- 可配置：
  - `RandomizedDelaySec`

说明：
- 不写真实密码
- 不把具体生产路径写死为唯一方案

## 13. cron 草案
cron 仅作为备用方案。

要求：
- wrapper 里 source env
- 保证 `PATH` 包含 `docker`
- 将日志重定向到安全目录

不建议：
- 直接把密码写进 crontab
- 把 cron 作为生产主方案

## 14. 与现有脚本关系
- `backup-sqlserver.sh` 保持手动可用
- 定时任务只是调用它
- `restore-sqlserver.sh` 不纳入自动定时
- 定时任务不做恢复
- 恢复演练仍需人工触发

## 15. 后续实施拆分
- `R33-e`：当前 scheduled backup design
- `R33-e-impl`：systemd service / timer 模板
- `R33-e-test`：Linux 环境定时触发验证
- `R33-cleanup-design`：备份清理策略设计
- `R33-g`：备份健康检查与失败告警
- `R33-cos`：腾讯云 COS 异地上传

## 16. 决策结论
- 当前推荐路线：
  - 本地继续手动备份
  - 服务器优先 `systemd timer`
- cron 作为备用，不作为生产主方案
- 第一版定时备份先做“定时全量备份”，不做自动清理、不做云上传
- 恢复演练必须成为定期流程的一部分
