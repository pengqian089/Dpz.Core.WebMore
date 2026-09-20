# Dpz.Core.WebMore

基于 Blazor WebAssembly 的个人网站客户端，目标框架 `net11.0`，运行时为 CoreCLR（`UseMonoRuntime=false`）并启用 runtime-async（`Features=runtime-async=on`），单项目。所有内容都在 `src/`；解决方案为 `src/Dpz.Core.WebMore.slnx`（旧的 `.sln` 已删除）。仓库没有任何测试项目。

## 常用命令（在 `src/` 下执行）

- 构建：`dotnet build Dpz.Core.WebMore.slnx`
- 运行：`dotnet run --project Dpz.Core.WebMore`
  - 需要 .NET 11 SDK；
  - 地址以 `Properties/launchSettings.json` 为准：http://localhost:3508 / https://localhost:3509
  - README 中的 5000/5001 已过时；`launchBrowser=false`，不会自动打开浏览器
- 验证改动 = 构建通过 + 手动验证页面，没有测试可跑

## 必须知道的机制

- **服务 DI 是反射自动注册的**：`Program.cs` 中 `RegisterInject` 扫描 `Dpz.Core.WebMore.Service` 命名空间下的接口，与 `Dpz.Core.WebMore.Service.Impl` 下的唯一实现做 `AddScoped` 配对。新服务必须严格放进这两个命名空间，否则不会被注入。
- **CoreCLR + runtime-async**：`UseMonoRuntime=false` 启用 browser-wasm 的 CoreCLR（发布时程序集为 `.wasm`/webcil，而非 `.dll`）；`Features=runtime-async=on` 启用 async2，需要 CoreCLR 运行时，不要退回 Mono。异步代码不要新增 `[MethodImpl(MethodImplOptions.Synchronized)]` 等 runtime-async 不支持的写法。
- **开发服务器是 Gateway**：`Microsoft.AspNetCore.Components.Gateway` 已取代过时的 `Microsoft.AspNetCore.Components.WebAssembly.DevServer`（11 中已标记 Deprecated）；`launchSettings.json` 不再需要 `inspectUri`。
- **index.html 占位符**：`index.html` 中的 `<link rel="preload" id="webassembly">`、空 `<script type="importmap">`、`_framework/blazor.webassembly#[.{fingerprint}].js` 依赖 csproj 的 `OverrideHtmlAssetPlaceholders=true` 在构建/发布时替换；`StaticWebAssetSpaFallbackEnabled=true` 让 Gateway 对深层路由回退到 index.html。
- **启动配置缺一不可**：`wwwroot/appsettings.json` 必须包含 `BaseAddress`、`SourceSite`、`AssetsHost`、`LibraryHost`，缺任何一个都会在启动时抛异常。
- 开发环境（`appsettings.Development.json`）指向本地后端：API `https://localhost:53381`、主站 `https://localhost:37701`、资源/库 `https://localhost:5505`。这些服务不在本仓库，未启动时页面能渲染但数据请求全部失败。
- `index.html` 从 `dpangzi.com` 加载 FontAwesome/Prism/tocbot/Photoswipe/lazyload 等外部资源；`Program.cs` 中 `UpyunHost` 是硬编码 CDN。不要把这些改成仓库内本地文件。
- `dpz.core.enumlibrary` 来自 `src/NuGet.config` 配置的 GitHub Packages 源，restore 需要 GitHub 凭据（报 401/403 时先检查这里）。

## CSS 构建（非标准 Blazor 流程，容易漏）

- 在 `src/Dpz.Core.WebMore` 下执行 `./build.ps1`：合并 `wwwroot/css/*.css`（自动跳过 `global.min.*`）→ `cleancss` 压缩 → 生成带 MD5 前 8 位的 `global.min.<hash>.css` → 改写 `wwwroot/index.html` 中的 `<link>`。
- 脚本依赖全局命令 `cleancss`（clean-css-cli），仓库未声明该依赖，本机当前未安装。
- **新增或修改 `wwwroot/css/` 下的 CSS 后必须重新运行该脚本**，否则页面引用不到改动。
- `.razor.css` 作用域样式不走该脚本，由 Blazor 编译进 `Dpz.Core.WebMore.styles.css`。

## 编码约定

权威文档：`src/EncodingConventions.md`（中文）。对 `src/` 文件生效的是 `src/.editorconfig`（`root = true`，根目录 `.editorconfig` 被遮蔽）。与默认习惯差异最大的几条：

- 一个类型一个 `.cs` 文件；Blazor 组件/页面的代码放在独立的 `.razor.cs` 分部文件
- 优先构造函数/主构造函数注入，不用 `[Inject]`（现有组件形如 `public partial class Article(IArticleService articleService)`）
- 只有单个构造函数时必须使用主构造函数；不允许 public 字段；严格按 nullable 语义编码
- 4 空格缩进、最长 100 列、控制语句始终带大括号、文件作用域命名空间、命名空间 = 项目名.目录
- UI 文案、注释、提交信息使用中文；提交遵循 Conventional Commits（如 `feat(hash-tool): ...`、`chore: ...`），分支用 `feat/*` 或 `develop-*`，PR 合入 `main`
