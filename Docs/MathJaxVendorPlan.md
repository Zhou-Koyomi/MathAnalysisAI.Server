# MathJax 本地 Vendor 计划

> **R38-b** | 日期：2026-06-03 | 状态：计划阶段（未下载，未替换）

---

## 1. 当前状态

| 页面 | 引用 | 风险 |
|------|------|------|
| `wwwroot/index.html:7` | `<script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js">` | HIGH — CDN 可被劫持，无 SRI |
| `wwwroot/dev.html:7` | 同上 | HIGH |
| `wwwroot/vendor/mathjax/` | **不存在** | — |

MathLive 已本地 vendor（`/vendor/mathlive/`），MathJax 是唯一的外部 CDN 依赖。

---

## 2. 原因：为什么本轮不替换

- 当前环境无法从 jsdelivr CDN 下载 MathJax（需外网连接）
- 不能伪造 SRI hash（无法验证完整性）
- 演示冻结期间不宜替换大依赖

---

## 3. MathJax 3 本地 Vendor 步骤

### 3.1 下载（在网络可用时执行）

```bash
# 进入 vendor 目录
cd wwwroot/vendor

# 创建 mathjax 目录
mkdir -p mathjax/es5

# 方式 A：从 npm 获取
npm pack mathjax@3
tar -xzf mathjax-3*.tgz
cp -r package/es5/* mathjax/es5/

# 方式 B：从 jsdelivr CDN 下载（需外网）
# 下载主入口文件
curl -o mathjax/es5/tex-mml-chtml.js \
  https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js

# 下载依赖文件（MathJax 3 会根据需要动态加载以下文件）
# 建议下载完整的 es5 目录以支持离线使用
# 可使用 wget 镜像整个 es5 目录：
wget -r -np -nH --cut-dirs=3 \
  https://cdn.jsdelivr.net/npm/mathjax@3/es5/ \
  -P mathjax/es5/
```

### 3.2 验证完整性

```bash
# 计算 SRI hash（下载后执行）
openssl dgst -sha384 -binary wwwroot/vendor/mathjax/es5/tex-mml-chtml.js | openssl base64 -A
# 输出格式：sha384-<hash>

# 或使用 shasum
shasum -b -a 384 wwwroot/vendor/mathjax/es5/tex-mml-chtml.js | \
  awk '{ print $1 }' | xxd -r -p | base64
```

### 3.3 替换 HTML 引用

将 `index.html:7` 和 `dev.html:7` 中的：

```html
<script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"></script>
```

替换为：

```html
<script src="/vendor/mathjax/es5/tex-mml-chtml.js"></script>
```

### 3.4 可选：添加 SRI（如果保留 CDN）

如果选择保留 CDN 引用，至少添加 SRI：

```html
<script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"
        integrity="sha384-<VERIFIED_HASH>"
        crossorigin="anonymous"></script>
```

⚠️ **必须**使用从可信任来源获取的、已亲自验证的 SRI hash。**不要**从不可信网页复制 hash。

---

## 4. 影响评估

### 4.1 替换后验证

- [ ] `index.html` 公式渲染正常
- [ ] `dev.html` 公式渲染正常
- [ ] 分析结果中的 LaTeX 公式正常显示
- [ ] 离线环境（断开外网）公式仍可渲染
- [ ] `.dockerignore` 确认 `!wwwroot/vendor/mathjax/` 不被排除

### 4.2 已知影响范围

MathJax 仅用于页面公式渲染（视觉展示），**不影响**：
- OCR、analyze、login、stats、leaderboard 核心功能
- 后端 API 调用
- MathLive 编辑器

如果 MathJax 加载失败，公式会回退为原始 LaTeX 文本显示，不影响功能。

---

## 5. .dockerignore 更新

当前 `.dockerignore` 已保留 `wwwroot/vendor/`：

```dockerignore
!wwwroot/vendor/
!wwwroot/vendor/**
```

`mathjax` 子目录会自动被包含，无需修改。

---

## 6. 文件大小估算

MathJax 3 完整 es5 目录约 5-8 MB（含字体文件）。对仓库大小和 Docker 构建时间有轻微影响，但可接受。

---

## 7. 后续执行

| 阶段 | 内容 | 时机 |
|------|------|------|
| R38-b-plan | 本文档（计划） | ✅ 当前 |
| R38-b-download | 下载 MathJax 3 到本地 vendor | 网络可用时 |
| R38-b-replace | 替换 HTML 引用 | R38-b-download 完成后 |
| R38-b-verify | 验证公式渲染离线可用 | R38-b-replace 完成后 |

---

## 8. 决策结论

- ❌ 当前不替换（无网环境，无法下载和验证 MathJax）
- ✅ 当前风险可接受（MathJax 仅影响公式渲染视觉效果，不影响核心功能）
- ✅ 后续优先本地 vendor（与 MathLive 策略一致）
- ⚠️ 保留 CDN + 无 SRI 在演示期间属于已知风险，R38-a 已记录
