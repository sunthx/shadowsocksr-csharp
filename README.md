ssr-lean-windows
=======================

> Fork From [shadowsocksr-csharp]

## 简介

常用代理服务器有时经常发神经，手里的常用的终端也挺多，为了维护世界的和平， 在 [shadowsocksr-csharp] 基础上，添加了同步代理服务器配置信息功能。


## 使用方法

同步功能后端使用 [LeanCloud] 提供的存储服务，所以你手里必须有 LeanCloud App 并且知道它的 AppId 与 AppKey，如果没有也请放心，它是可以免费申请的。

最后一步，替换源码中 AppId 与 AppKey，我知道这很非人类（反正也就我自己用）。

![](http://i.imgur.com/3j3Z4rM.png)

## 开源协议

**GPLv3**

[shadowsocksr-csharp]: https://github.com/shadowsocksr/shadowsocksr-csharp
[ssr-lean-windows]: https://github.com/sunthx/ssr-lean-win/tree/lean-sync
[LeanCloud]: https://leancloud.cn