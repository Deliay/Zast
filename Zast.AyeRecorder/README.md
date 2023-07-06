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
首次运行会询问首选的录制格式、码率、录制位置等，按照CLI指引设置即可。也可以在配置文件中`setting.json`中修改相应的配置。

### 使用指令
- `Zast.AyeRecorder` 无参数运行，按照配置开始录制
- `Zast.AyeRecorder config` 打开CLI指引
- `Zast.AyeRecorder add [room]` 增加需要录制的直播间
- `Zast.AyeRecorder remove [room]` 停止录制对应直播间

###

## 依赖
- 基本运行时 [.net 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- M38U 解码/编码 [ffmpeg](https://ffmpeg.org/download.html)
