# SQL Server 恢复脚本设计

## 1. 背景与目标
- 当前项目已经完成：
  - `R33-a`：SQL Server 备份与恢复方案设计
  - `R33-b`：手动备份脚本 `scripts/backup-sqlserver.sh`
- 当前仍未完成：
  - restore 脚本
  - restore 演练 runbook
  - 自动备份 / 定时备份
  - 云端备份

本设计目标是为脚本：
- `scripts/restore-sqlserver.sh`

确定安全边界、默认行为和恢复验证规范。

当前实施状态：
- `R33-c-impl` phase 1 已实现 `scripts/restore-sqlserver.sh`
- 当前只支持恢复到临时数据库
- 当前不支持覆盖 `MathAnalysisAI`
- 当前不支持停服覆盖恢复
- 已完成一次真实临时库恢复验证
- 当前仍未执行主库覆盖恢复

执行手册见：
- `Docs/BackupRestoreRunbook.md`

## 2. 默认安全策略
当前 restore 脚本必须采用“默认不伤主库”的安全策略：

- 默认不覆盖 `MathAnalysisAI`
- 默认恢复到临时数据库名
- 默认不停止 `server`
- 默认不影响当前演示主库
- 默认不修改现有业务数据

推荐默认恢复数据库名格式：
```text
MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS
```

覆盖主库必须显式启用，并满足双重保护：
1. 设置：
   - `ALLOW_OVERWRITE=true`
2. 明确输入确认文本，例如：
   - `RESTORE MathAnalysisAI`

若没有显式确认，脚本必须直接退出。

## 3. 环境变量与参数
当前脚本支持以下环境变量：

- `SQL_CONTAINER_NAME`
  - 默认：`mathanalysis-sqlserver`
- `SQL_DATABASE_NAME`
  - 默认：`MathAnalysisAI`
- `RESTORE_DATABASE_NAME`
  - 默认：自动生成 `MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS`
- `MSSQL_SA_PASSWORD`
  - 必填
- `BACKUP_FILE`
  - 必填，宿主机上的 `.bak` 路径
- `ALLOW_OVERWRITE`
  - 默认：`false`
- `STOP_SERVER_BEFORE_RESTORE`
  - 默认：`false`
- `SERVER_CONTAINER_NAME`
  - 默认：`mathanalysis-server`
说明：
- phase 1 默认路径只走“恢复到临时库”
- `ALLOW_OVERWRITE=true` 在当前实现中会直接拒绝执行
- `STOP_SERVER_BEFORE_RESTORE=true` 在当前实现中会直接拒绝执行

## 4. 输入校验
当前脚本会先做输入校验，再进入 restore 逻辑。

至少应检查：
- `docker` 命令可用
- Docker daemon 可访问
- SQL Server 容器存在且 running
- `MSSQL_SA_PASSWORD` 非空
- `BACKUP_FILE` 非空
- `BACKUP_FILE` 存在
- `BACKUP_FILE` 后缀是 `.bak`
- `BACKUP_FILE` 不在 Git 仓库内
  - 若在仓库内，应强警告并建议中止
- 容器内 `sqlcmd` 可用
- 目标数据库名合法
- 若检测到覆盖主库意图：
  - `ALLOW_OVERWRITE=true`
  - 或 `RESTORE_DATABASE_NAME == SQL_DATABASE_NAME`
  - 当前实现直接拒绝执行

目标数据库名合法性建议：
- 仅允许字母、数字、下划线
- 不允许空白、分号、引号、路径字符

## 5. 默认恢复到临时库流程
phase 1 当前恢复流程如下：

1. 检查输入与运行环境
2. 将宿主机 `.bak` 复制到 SQL Server 容器临时目录
3. 读取 backup logical file names
4. 生成目标数据库名
5. 使用 `RESTORE DATABASE ... WITH MOVE ...` 恢复到目标数据库名
6. 删除容器内临时 `.bak`
7. 做只读验证
8. 输出恢复结果与下一步建议

关键点：
- 默认恢复目标不是 `MathAnalysisAI`
- 默认不要求停止 `server`
- 默认不影响当前演示主流程

## 6. 覆盖主库流程
覆盖主库属于高风险路径，当前仍未实现。

后续建议流程：

1. 明确要求：
   - `ALLOW_OVERWRITE=true`
2. 明确要求：
   - `RESTORE_DATABASE_NAME=MathAnalysisAI`
3. 建议先执行一次新的备份
4. 要求停止 `server` 容器，避免写入
   - 若 `STOP_SERVER_BEFORE_RESTORE=false`，脚本应拒绝继续
5. 将主库切换为 `SINGLE_USER`
6. 执行：
   - `RESTORE DATABASE ... WITH REPLACE`
7. 切回 `MULTI_USER`
8. 重启 `server`
9. 验证 `/api/health`
10. 登录 `test_student`
11. 跑固定 analyze 样例

设计原则：
- 不允许“未停服务直接覆盖主库”
- 不允许“未确认直接恢复主库”
- 不允许“恢复失败后自动重试覆盖”

## 7. 只读验证
恢复完成后，脚本会执行只读验证。

建议验证项：
- 数据库存在
- 核心表存在：
  - `AppUsers`
  - `Courses`
  - `Chapters`
  - `KnowledgePoints`
  - `Problems`
  - `StudentSolutions`
  - `AnalysisResults`
  - `UserCourseStats`
- 可查询 `AppUsers` 数量
- 可查询 `AnalysisResults` 数量
- 可查询 `__EFMigrationsHistory`
- 输出迁移数量

验证输出要求：
- 不输出敏感字段
- 不输出用户完整解答内容
- 不输出 API key、密码、cookie

## 8. 失败处理
当前脚本应对以下常见失败场景给出清晰错误提示：

- backup file 不存在
- 密码错误
- 数据库已存在但未允许覆盖
- logical file name 读取失败
- restore 失败
- `docker cp` 失败
- 权限不足
- SQL Server 磁盘空间不足
- 尝试覆盖主库但 `server` 未停止

失败时必须满足：
- 不删除原数据库
- 不自动重试覆盖
- 不默认切换到危险模式
- 输出下一步排查建议

## 9. 安全要求
必须明确：

- restore 脚本不读取 `/etc/mathanalysis-ai/*.env`
- 不打印密码
- 不打印 API key
- 不打印 cookie
- 不输出 `docker compose config`
- `.bak` 包含用户数据，不得发到聊天或提交 Git
- 默认恢复到临时库
- 覆盖主库必须显式确认
- 不允许无确认覆盖 `MathAnalysisAI`

补充要求：
- 恢复脚本应只输出必要的恢复摘要
- 不记录完整 SQL 语句中的敏感值

## 10. 与 DemoFreeze 的关系
- 演示前允许恢复到临时库做验证
- 演示前不建议覆盖主库
- 如果必须覆盖主库，属于 `P0 / P1` 风险修复，必须记录原因
- 仍然禁止 `docker compose down -v`

恢复主库前建议记录：
- 操作时间
- 操作人
- 触发原因
- 使用的备份文件名
- 影响范围

## 11. 后续实施拆分
- `R33-c`：restore script design
- `R33-c-impl`：实现 `restore-sqlserver.sh` phase 1（已完成）
- `R33-c-test`：用临时数据库名做一次恢复演练
- `R33-d`：定时备份
- `R33-e`：腾讯云 COS 上传
- `R33-f`：恢复演练 runbook
- `R33-g`：备份健康检查

推荐顺序：
1. 先完成 phase 1 临时库恢复演练
2. 验证成功后补 runbook
3. 最后再考虑覆盖主库脚本化

## 12. 决策结论
- restore 脚本默认必须恢复到临时数据库名，而不是覆盖主库。
- 覆盖 `MathAnalysisAI` 必须显式启用、显式确认、显式停服。
- 恢复后必须做只读验证，不应只依赖 `RESTORE DATABASE` 成功返回。
- 在演示冻结期内，restore 能力属于风险控制，不属于扩功能；但默认应以“临时库恢复演练”为主，不主动碰主库。
- 当前 `scripts/restore-sqlserver.sh` 已落实上述 phase 1 约束：只恢复到临时库，直接拒绝覆盖主库。
