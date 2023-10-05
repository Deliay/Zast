# B站部分接口访问实现
该项目主要用于弹幕获取，然后存库，支持登陆到指定账号进行抓取

## API实现
| 类 | 用途
| - | - |
| BiliLiveCrawler | 直播弹幕、直播流相关API |
| BiliVideoCrawler | 视频相关信息API |

## Websocket 弹幕抓取实现
见 `WebsocketCrawler` 文件夹的 `README`，可用事件可以在`ICommandBase`类实现中找到。

## 使用示例

### 获得直播间弹幕流
代码示例：
```csharp
var uid = 403496L;
var uidCookie = "...";
var roomId = 11306L;

// 用于获得登陆状态下观看直播的token，及弹幕服务器地址
var liveCrawler = new BiliLiveCrawler();
liveCrawler.SetCookie(uidCookie);

var liveTokenResponse = await liveCrawler.GetLiveToken(roomId, cancellationToken);
var token = liveTokenResponse.Token;

// 循环弹幕服务器地址
foreach (var spectatorHost in liveToken.Hosts)
{
    // websocket获得直播间事件
    using var wsClient = new WebsocketClient();

    // 连接弹幕服务器，填入使用cookie获得的token
    await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, roomId, uid, token, "wss", cancellationToken);

    // 获得事件
    await foreach(var @event in wsClient.Events(cancellationToken))
    {
        if (@event is Normal normalMessage)
        {
            var cmd = ICommandBase.Parse(normal.RawContent);
            ...见下方处理事件示例
        }
    }
}
```

### 处理事件：使用CommandSubscriber
```csharp
// 事先准备好CommandSubscriber类
using var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => ...);
cmdHandler.Subscribe<DanmuMsg>(async (msg) => ...);
cmdHandler.Subscribe<SuperChatMessage>((msg) => ...);
cmdHandler.Subscribe<SendGift>(async (msg) => ...);

// 用CommandSubscriber处理直播事件
await commandHandler.Handle(cmd);
```

### 处理事件：手动处理
```csharp
// 或者手动处理直播事件
if (cmd is CommandBase<DanmuMsg> danmakuCmd)
{
    // 处理弹幕消息
    var danmaku = danmakuCmd.Info;
}
else if (cmd is CommandBase<SendGift> giftCmd)
{
    // 处理礼物消息
    var gift = giftCmd.Data;
}
```