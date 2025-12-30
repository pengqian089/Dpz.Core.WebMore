# Dpz.Core.WebMore

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

基于 ASP.NET Core Blazor WebAssembly 构建的个人网站应用

[在线预览](https://www.dpangzi.com) | [问题反馈](https://github.com/pengqian089/Dpz.Core.WebMore/issues)

</div>

## 📖 项目简介

Dpz.Core.WebMore 是一个使用 Blazor WebAssembly 技术构建的现代化个人网站应用。项目最初作为学习和实验 Blazor 技术的练习项目，经过不断迭代和完善，现已发展成为功能完善的个人网站，并成功部署上线。

虽然不是主站点，但本应用在外观设计、数据展示等方面与主站点保持了高度一致，提供了流畅的用户体验。

## ✨ 核心特性

### 📝 内容管理
- **文章系统** - 支持文章列表、详情展示、搜索和分类
- **代码笔记** - 代码片段的组织和展示，支持语法高亮
- **碎碎念（Mumble）** - 简短的想法和记录
- **时间线** - 以时间轴形式展示重要事件

### 🎨 多媒体
- **相册管理** - 图片的展示和管理
- **音乐播放** - 音乐列表和播放功能
- **视频展示** - 视频内容的组织和播放

### 🔖 个人工具
- **书签管理** - 网页书签的收藏和分类
- **Steam 游戏** - Steam 游戏库展示和详情
- **BSON 工具** - BSON 数据的格式化展示

### 💬 社交互动
- **评论系统** - 文章和内容的评论功能
- **成就系统** - 个人成就的展示
- **朋友链接** - 友情链接管理
- **实时通知** - 基于 SignalR 的实时推送

### 🎯 技术亮点
- **图标系统** - 集成 Material Design 图标，文件类型图标自动识别
- **对话框服务** - 统一的对话框、通知和 Toast 消息管理
- **分页组件** - 灵活的数据分页支持
- **响应式设计** - 适配各种屏幕尺寸

## 🛠️ 技术栈

- **框架**: ASP.NET Core Blazor WebAssembly (.NET 10.0)
- **前端**: Blazor Components + JavaScript Interop
- **实时通信**: SignalR
- **样式**: 自定义 CSS
- **构建工具**: MSBuild

## 📁 项目结构

```
Dpz.Core.WebMore/
├── Pages/                    # 页面组件
│   ├── Albums.razor         # 相册页面
│   ├── Article.razor        # 文章详情
│   ├── ArticleList.razor    # 文章列表
│   ├── Bookmark.razor       # 书签管理
│   ├── CodeView.razor       # 代码查看
│   ├── Mumble.razor         # 碎碎念
│   ├── Timeline.razor       # 时间线
│   ├── Comment/             # 评论组件
│   └── CodeComponent/       # 代码组件
├── Shared/                   # 共享组件
│   ├── Components/          # 可复用组件
│   ├── MainLayout.razor     # 主布局
│   └── NavMenu.razor        # 导航菜单
├── Service/                  # 服务接口和实现
│   ├── IArticleService.cs
│   ├── ICommentService.cs
│   ├── IMusicService.cs
│   └── Impl/                # 服务实现
├── Models/                   # 数据模型
│   ├── ArticleModel.cs
│   ├── CommentModel.cs
│   ├── Dialog/              # 对话框模型
│   └── ...
├── Helper/                   # 辅助工具
│   ├── Icons/               # 图标帮助类
│   ├── PagedList.cs         # 分页工具
│   └── TypeDiscriminatorConverter.cs
└── wwwroot/                  # 静态资源
    ├── css/                 # 样式文件
    ├── js/                  # JavaScript 文件
    └── index.html           # 入口页面
```

## 🚀 快速开始

### 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- 推荐使用 Visual Studio 2022 / Rider / VS Code

### 运行项目

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd src/Dpz.Core.WebMore
   ```

2. **还原依赖**
   ```bash
   dotnet restore
   ```

3. **运行应用**
   ```bash
   dotnet run
   ```

4. **访问应用**
   
   在浏览器中打开 `https://localhost:5001` 或 `http://localhost:5000`

### 构建发布

```bash
dotnet publish -c Release -o ./publish
```

发布后的文件位于 `./publish/wwwroot` 目录下。

## 📦 部署说明

### Nginx 配置

```conf
server {
    listen                      80;
    listen                      443 ssl http2;
    server_name                 www.dpangzi.com;
    ssl_certificate             /path/to/cert/dpangzi.com_bundle.pem;
    ssl_certificate_key         /path/to/cert/dpangzi.com.key;
    ssl_protocols               TLSv1.2 TLSv1.3;
    ssl_ciphers                 EECDH+CHACHA20:EECDH+AES128:RSA+AES128:EECDH+AES256:RSA+AES256:!MD5;
    ssl_prefer_server_ciphers   on;
    ssl_session_cache           shared:SSL:10m;
    ssl_session_timeout         10m;
    add_header                  Strict-Transport-Security "max-age=31536000";
    error_page 497              https://$host$request_uri;
    
    root                        /path/to/wwwroot;
    location / {
        root                    /path/to/wwwroot;
        try_files               $uri $uri/ /index.html =404;
        limit_req               zone=one burst=60 nodelay;
    }
}
```

### IIS 配置 (web.config)

<details>
<summary>点击展开 web.config 配置</summary>

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.webServer>
        <httpProtocol>
            <customHeaders>
                <remove name="X-Powered-By"/>
            </customHeaders>
        </httpProtocol>
        <staticContent>
            <remove fileExtension=".blat"/>
            <remove fileExtension=".dat"/>
            <remove fileExtension=".dll"/>
            <remove fileExtension=".json"/>
            <remove fileExtension=".wasm"/>
            <remove fileExtension=".woff"/>
            <remove fileExtension=".woff2"/>
            <mimeMap fileExtension=".blat" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".dll" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".dat" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".json" mimeType="application/json"/>
            <mimeMap fileExtension=".wasm" mimeType="application/wasm"/>
            <mimeMap fileExtension=".woff" mimeType="application/font-woff"/>
            <mimeMap fileExtension=".woff2" mimeType="application/font-woff"/>
        </staticContent>
        <httpCompression>
            <dynamicTypes>
                <add mimeType="application/octet-stream" enabled="true"/>
                <add mimeType="application/wasm" enabled="true"/>
            </dynamicTypes>
        </httpCompression>
        <rewrite>
            <rules>
                <rule name="Serve subdir">
                    <match url=".*"/>
                    <action type="Rewrite" url="wwwroot\{R:0}"/>
                </rule>
                <rule name="SPA fallback routing" stopProcessing="true">
                    <match url=".*"/>
                    <conditions logicalGrouping="MatchAll">
                        <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true"/>
                    </conditions>
                    <action type="Rewrite" url="wwwroot\index.html"/>
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>
```

</details>

### 部署步骤

1. 使用 `dotnet publish` 命令构建发布版本
2. 将 `wwwroot` 目录下的所有文件上传到服务器
3. 配置 Web 服务器（Nginx/IIS/Apache）
4. 确保服务器支持 WASM MIME 类型
5. 配置 HTTPS 证书（推荐使用 Let's Encrypt）

## 🔧 开发说明

### 配置文件

应用配置位于 `wwwroot/appsettings.json` 和 `wwwroot/appsettings.Development.json`

```json
{
  "ApiBaseUrl": "https://api.example.com",
  "SignalRHub": "https://api.example.com/hub"
}
```

### 自定义样式

全局样式文件位于 `wwwroot/css/` 目录下，可根据需要修改。

### JavaScript 互操作

JavaScript 文件位于 `wwwroot/js/` 目录下，Blazor 组件可通过 `IJSRuntime` 调用。

## 📝 待办事项

- [ ] 添加国际化支持（i18n）
- [ ] 查看代码时的性能优化

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🔗 相关链接

- [在线访问](https://www.dpangzi.com)
- [Blazor 官方文档](https://docs.microsoft.com/zh-cn/aspnet/core/blazor/)
- [.NET 文档](https://docs.microsoft.com/zh-cn/dotnet/)

## 📮 联系方式

如有问题或建议，欢迎通过以下方式联系：

- 网站: https://www.dpangzi.com
- Issues: [GitHub Issues](https://github.com/your-repo/issues)

---

<div align="center">

**[⬆ 回到顶部](#dpzcorewebmore)**

Made with ❤️ using Blazor

</div>
