
# 格式化 C# — 工具清单在 src/dotnet-tools.json（不是标准的 .config/）
# 必须在 src/ 下运行，而不是仓库根目录
dotnet tool restore     # 在 src/ 下
dotnet csharpier .      # 在 src/ 下（配置：src/.csharpierrc.yaml，csharpier 1.3.0）



所有 `net11.0` 项目都在其 `.csproj` 中通过 `<Features>runtime-async=on</Features>` 启用 Runtime Async（预览功能；可按项目用 `<UseRuntimeAsync>false</UseRuntimeAsync>` 关闭）。两个 `netstandard2.0` 源生成器项目不使用它。

## 编码规范

- 缩进：4 个空格。最大行长：100 字符。
- 文件作用域命名空间：`namespace X.Y;`
- 私有字段：`_camelCase` 前缀
- 始终使用大括号（即使是单行 `if`/`for` 等）
- 不允许行尾内联注释（`var x = 1; // bad`）— 该约定仅靠代码评审约束，没有 Roslyn 分析器强制
- 不允许 public 字段 — 使用属性
- 只有一个构造函数时使用主构造函数（primary constructor）
- 方法名不得使用 `Ensure` 前缀 — 使用更能表达方法目的的命名
- 异步方法必须以 `Async` 结尾，且始终接受 `CancellationToken cancellationToken = default`
- 只用结构化日志 — Serilog 调用中不得使用字符串插值
- 数组/列表返回空集合而非 null（`byte[]?` 可以）
- 参数：尽可能抽象。返回值：尽可能具体。
- 使用 C# 14 的 `extension` 方法语法（不是传统 `this` 参数）
- **服务的公共方法（及接口签名）绝不能直接返回实体类型。** 实体位于 `Dpz.Core.Public.Entity`，只在仓储/服务内部使用；服务的公共 API 必须返回 `Dpz.Core.Public.ViewModel` 中的 DTO / ViewModel / Response 类型（例如 `VmVideo`、`MusicResponse`、`CommentViewModel`）。以此将持久化模型与调用方隔离。
- 对象映射使用 **Mapster**（`IMapper` / `TypeAdapterConfig`），不是 AutoMapper。运行时类型映射必须使用注入的 `IMapper`；不要调用静态 `Adapt<T>()`/`Adapt(...)`，因为那会绕过 DI 注册的自定义映射。
- 返回值类型不得使用匿名、元组等类型

## 服务 DI 注册（源生成器驱动）





## 分支命名

`<type>/<issue-id>-<short-description>` — 类型：`feature/`、`bugfix/`/`fix/`、`hotfix/`、`release/`、`chore/`、`docs/`、`refactor/`、`test/`
