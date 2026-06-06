# 前端页面拆分方案

## 1. 拆分前问题
- `index.html` 同时承载：
  - OCR 上传
  - OCR 结果回填
  - MathLive 公式校对
  - 手动分析输入
  - 分析结果展示
- 首页职责过重，导致：
  - 页面过长
  - 首屏不清晰
  - OCR / analyze DOM 耦合度高
  - 公网演示时首页信息密度过高

## 2. 拆分后页面职责
- `index.html`
  - 仅作为首页入口
  - 展示产品说明与入口卡片
  - 不再承载完整 OCR / analyze 流程
- `analysis.html`
  - 承载手动输入题目、学生解答、手动分析与结果展示
- `ocr.html`
  - 承载图片上传
  - OCR 文本回填
  - MathLive inline 公式卡片校对
  - 手动触发分析与结果展示
- `stats.html`
  - 保持学习统计与排行榜
- `materials.html`
  - 保持课程资料管理与检索调试
- `dev.html`
  - 保持开发工具、symbolic 调试和 MathLive 试验

## 3. 页面加载的 JS/CSS
### 3.1 `index.html`
- `/css/app.css`
- `/js/config.js`
- `/js/api.js`
- `/js/ui.js`
- `/js/auth.js`
- `/js/nav.js`

### 3.2 `analysis.html`
- `/css/app.css`
- MathJax CDN
- `/js/config.js`
- `/js/api.js`
- `/js/ui.js`
- `/js/auth.js`
- `/js/nav.js`
- `/js/knowledge-points.js`
- `/js/analysis.js`

### 3.3 `ocr.html`
- `/css/app.css`
- MathJax CDN
- `/vendor/mathlive/*`
- `/js/config.js`
- `/js/api.js`
- `/js/ui.js`
- `/js/auth.js`
- `/js/nav.js`
- `/js/knowledge-points.js`
- `/js/analysis.js`
- `/js/photo-solution.js`
- `/js/mathlive-ocr.js`

## 4. JS 拆分结果
- `analysis.js`
  - 保持原有 `window.analyzeText`
  - 新增 DOM 存在检查
  - 仅在分析页面相关 DOM 存在时运行
- `photo-solution.js`
  - 保持原有 `window.recognizePhotoSolution`
  - 新增 DOM 存在检查
  - 仅在 OCR 页面相关 DOM 存在时运行
- `mathlive-ocr.js`
  - 保持每条公式一个独立 `math-field`
  - 未重写逻辑，仅继续挂载在 OCR 页面
- `nav.js`
  - 新增 `/analysis.html` 与 `/ocr.html` 的激活路由支持
- `api.js`
  - 未改公开方法名
- `ui.js`
  - 未改公共接口

## 5. 未完成事项
- 还未做浏览器点击级完整回归
- 首页入口文案与卡片样式仍可继续微调
- MathJax 仍未本地 vendor 化
- OCR / analysis 共用表单结构目前为静态复制，后续如有需要可再提取为更细粒度模板

## 6. 手动验证清单
- `/`
  - 首页只展示入口，不展示 OCR / analyze 大表单
- `/analysis.html`
  - 可输入题目与解答
  - 可点击开始分析
  - 可展示分析结果
- `/ocr.html`
  - 可上传图片
  - 可展示 OCR 回填
  - 可展示公式卡片
  - 可进行 MathLive inline 编辑
  - 不自动 analyze
- `/stats.html`
  - 页面正常打开
- `/materials.html`
  - 页面正常打开
- `/dev.html`
  - 页面正常打开

## 7. 决策结论
- 本轮拆分优先保证：
  - 不改后端 API
  - 不改数据库
  - 不引入前端框架
  - 不重写 OCR / MathLive 逻辑
- 拆分结果是一个最小、安全、可回滚的页面级重组：
  - 首页变成入口页
  - 手动分析与 OCR 流程分离
  - 原有 JS 保持基本兼容，仅增加 DOM 守卫
