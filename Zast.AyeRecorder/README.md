# Aye Recorder (录伊会)
> [关注阿伊蕾喵，关注阿伊蕾特谢谢喵](https://space.bilibili.com/117906)
>
> [关注阿伊蕾喵，关注阿伊蕾特谢谢喵](https://space.bilibili.com/117906)
>
> [关注阿伊蕾喵，关注阿伊蕾特谢谢喵](https://space.bilibili.com/117906)

纯命令行的录播姬，支持录制B站`m3u8`格式的录播，也可以录制B站`flv`单文件录播。本项目依赖`ffmpeg`，运行前请保证环境变量中存在`ffmpeg`可执行文件。

本项目基于`.NET 7`，请提前安装 `.NET 7` 运行时。

## 使用指引

### 安装依赖
##### .NET
程序基于`.NET 7`为了能让程序可以执行，请先安装 `.NET 7` 运行时。访问[这里](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)安装。

##### FFmpeg
程序会将B站的`m3u8`编码为`flv`，需要ffmpeg支持。访问[这里](https://ffmpeg.org/download.html)安装。

### 配置
首次运行会询问首选的录制格式、码率，按照CLI指引设置即可。也可以在配置文件中`setting.json`中修改相应的配置。

#### 存储路径
没有特殊配置则存储到`Working Directory`下，每一个直播间独立为一个文件夹
- 文件夹固定为`{房间号}`
- 默认文件名为`{房间号}-{开始时间}-{标题}-{画质}.flv`，可自定义。

#### 可用变量
- 房间号
- 开始时间
- 当场直播开始时间
- 标题
- 录制画质

编写格式参照 [dotliquid](https://github.com/dotliquid/dotliquid)。

### 使用指令
- `Zast.AyeRecorder` 无参数运行，按照配置开始录制
- `Zast.AyeRecorder config` 打开CLI指引
- `Zast.AyeRecorder add [room]` 增加需要录制的直播间
- `Zast.AyeRecorder remove [room]` 停止录制对应直播间

## 杂项
初看B站的API，貌似支持杜比和HDR流，这个之后广泛应用了再考虑抓取吧。感觉能推这种流的场景，流本身一般也是加密的。**能付费还是要付费的呀**。

## 依赖
- 基本运行时 [.net 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- M38U 解码/编码 [ffmpeg](https://ffmpeg.org/download.html)
