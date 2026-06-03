# SQL Server 手动备份与临时恢复手册

## 1. 适用范围
本手册用于以下场景：
- 本地演示前手动备份
- migration 前手动备份
- 升级前备份
- 生产部署前备份验证
- 恢复演练

当前范围明确为：
- 支持手动备份
- 支持恢复到临时数据库
- 支持恢复后只读验证
- 支持手动清理临时恢复库

当前不支持：
- 覆盖 `MathAnalysisAI` 主库恢复
- 自动定时备份
- 云端上传
- 差异备份 / 日志备份

定时备份设计见：
- `Docs/ScheduledBackupDesign.md`

## 2. 前置条件
执行前请确认：
- Docker daemon 正常
- `mathanalysis-sqlserver` 处于 `running`
- `MathAnalysisAI` 数据库存在
- 已知 `MSSQL_SA_PASSWORD`
- [backup-sqlserver.sh](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/scripts/backup-sqlserver.sh) 存在
- [restore-sqlserver.sh](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/scripts/restore-sqlserver.sh) 存在
- 宿主机备份目录可写

可先检查：
```bash
docker ps --format '{{.Names}} {{.Status}}'
```

## 3. 手动备份流程
推荐使用项目外目录保存 `.bak`：

```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
BACKUP_HOST_DIR="$HOME/Backups/mathanalysis-ai" \
./scripts/backup-sqlserver.sh
```

成功后脚本会输出：
- 宿主机备份文件路径
- 备份文件大小

注意：
- 不要把 `.bak` 放在 repo 目录内
- 不要提交 `.bak`
- 不要在聊天里发送 `.bak`
- `.bak` 含有用户学习数据、题目、解答、分析结果

## 4. 临时恢复验证流程
使用刚生成的 `.bak` 做临时恢复验证：

```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
BACKUP_FILE='/path/to/MathAnalysisAI_YYYYMMDD_HHMMSS.bak' \
./scripts/restore-sqlserver.sh
```

如需显式指定临时库名：

```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
BACKUP_FILE='/path/to/MathAnalysisAI_YYYYMMDD_HHMMSS.bak' \
RESTORE_DATABASE_NAME='MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS' \
./scripts/restore-sqlserver.sh
```

强调：
- 当前不会覆盖 `MathAnalysisAI`
- 当前不会停止 `server`
- 当前不支持 `ALLOW_OVERWRITE=true`
- 当前不支持 `STOP_SERVER_BEFORE_RESTORE=true`

## 5. 验证恢复结果
`restore-sqlserver.sh` 会自动执行只读验证，当前验证项包括：
- `AppUsers`
- `Courses`
- `Chapters`
- `KnowledgePoints`
- `Problems`
- `StudentSolutions`
- `AnalysisResults`
- `UserCourseStats`
- `__EFMigrationsHistory`

并输出：
- `AppUsers count`
- `AnalysisResults count`
- `__EFMigrationsHistory count`

说明：
- 这些 count 仅用于确认恢复库可查询
- 不输出用户完整题目、解答、`RawResponseJson`

## 6. 清理临时恢复库
恢复验证完成后，可手动清理临时数据库。

建议先列出临时库确认：

```bash
docker exec mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P 'YourStrongPassword@123' \
  -Q "SELECT name FROM sys.databases WHERE name LIKE 'MathAnalysisAI_RestoreTest_%'"
```

确认目标数据库名后，再执行删除：

```bash
docker exec mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P 'YourStrongPassword@123' \
  -Q "DROP DATABASE [MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS]"
```

注意：
- **不要写错数据库名**
- **不要删除 `MathAnalysisAI`**

## 7. 常见错误
- `MSSQL_SA_PASSWORD` 未设置
  - 原因：环境变量缺失
- Docker daemon 未运行
  - 原因：Docker Desktop / daemon 未启动
- 容器不存在或未 running
  - 原因：`mathanalysis-sqlserver` 未启动
- `BACKUP_FILE` 不存在
  - 原因：路径写错或备份文件已删除
- `.bak` 后缀不对
  - 原因：传入了错误文件
- `sqlcmd` 不存在
  - 原因：容器镜像缺少对应工具路径
- 目标临时库已存在
  - 原因：库名冲突，脚本默认拒绝覆盖
- `docker cp` 后权限导致 SQL Server 读不到 `.bak`
  - 当前脚本已在容器内自动修正 owner / permission
- 磁盘空间不足
  - 原因：SQL Server 数据目录或宿主机空间不足
- SQL Server 版本不兼容
  - 原因：`.bak` 与目标实例版本不匹配

## 8. 安全注意事项
- 不读取 `/etc/mathanalysis-ai/*.env`
- 不打印密码
- 不打印 key
- `.bak` 不进 Git
- `.bak` 不发聊天
- 不执行 `docker compose down -v`
- 当前不支持覆盖主库
- 覆盖主库恢复必须另开 `R33-overwrite-restore` 并单独审查

## 9. 推荐使用场景
- 演示前
- migration 前
- 修改认证 / 权限前
- 部署前
- 生产升级前

## 10. 决策结论
- 当前已形成最小手动备份恢复闭环：
  - 能手动备份
  - 能恢复到临时数据库
  - 能做只读验证
  - 能手动清理临时库
- 当前 `P0` 风险已经从“无备份恢复能力”降级为：
  - 仍无自动化备份
  - 仍无异地备份
  - 仍无覆盖主库恢复 runbook

下一步重点应转向：
- 定时备份
- 云端异地备份
- 恢复演练 runbook 细化
- 备份健康检查
