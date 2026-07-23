# Alien

[English](README.md) | [繁體中文](README.zh-TW.md) | [简体中文](README.zh-CN.md)

![status](https://img.shields.io/badge/status-development-orange)
![C#](https://img.shields.io/badge/language-C%23-blue)
![license](https://img.shields.io/badge/license-MIT-green)

Alien（外星人）是一套采用模块化设计的 WebShell 管理框架，主要用于渗透测试、网络安全研究与教学。

Alien 内置文件管理、虚拟终端、数据库管理等后渗透功能，并提供插件系统，方便用户根据需求扩展功能。

如果你喜欢这个项目，欢迎点一个 ⭐！你的支持将是我持续完善这个项目的最大动力！

# 免责声明

Alien 仅用于合法的渗透测试、CTF 竞赛以及网络安全学习。

未经授权，请勿将本项目用于攻击任何受保护的系统或设备。

作者不对因使用本项目所造成的任何非法行为或损失承担责任。

# 架构

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/architecture.png" width=1000/>
</p>

# 系统要求

运行 Alien 前，请确保满足以下要求：

- 已安装 **Python 3**
- `python` 命令已添加至系统 **PATH** 环境变量

如果不确定是否安装成功，可以执行以下命令进行验证：

```batch
python --version
```

## 核心组件

Alien 由以下几个核心模块组成：

| 模块 | 说明 |
| --- | --- |
| **OneShell** | 一句话木马，负责在目标服务器上提供 Arbitrary Code Execution（任意代码执行）能力。 |
| **NebulaPulsar** | Java 与 .NET 内存植入框架，支持 Reflective Loading。 |
| **DarkMatter** | 由 NebulaPulsar 加载并执行的 Payload。 |
| **Event Horizon** | HTTP 流量混淆框架。 |
| **DriftingComet** | WebShell 多级跳板（Pivoting）框架。 |

# 支持的目标环境

| 脚本 | 服务器 | 执行方式 | Payload 类型 |
| --- | --- | --- | --- |
| PHP | Linux/Windows | v5.X | OneShell |
| | | v7.X、v8.X | OneShell、ECDH (Elliptic Curve Diffie-Hellman) |
| ASP | Windows | Classic | OneShell |
| ASPX、ASHX、ASMX | Windows | JScript | OneShell |
| | Linux/Windows | NebulaPulsar | DarkMatter |
| JSP | Linux/Windows | Nashorn | OneShell |
| | | NebulaPulsar | DarkMatter |
| JSPX | Linux/Windows | Nashorn | OneShell |
| | | NebulaPulsar | DarkMatter |
| CFML | Linux/Windows | NebulaPulsar | DarkMatter |
| Perl | Linux/Windows | CGI | OneShell |
| Ruby | Linux/Windows | CGI | OneShell |

# 功能

- 文件管理
- 虚拟终端
- 数据库管理
- 任意代码执行
- SOCKS5 代理
- 插件系统
- WebShell 多级跳板（DriftingComet）
- HTTP 流量混淆（Event Horizon）
- Reflective Loading（.NET / Java）

# 截图

## 基本信息

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/control_panel.png" width=700/>
</p>

## 文件管理

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/file.png" width=700/>
</p>

## 虚拟终端

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/shell.png" width=700/>
</p>

## 数据库

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database.png" width=700/>
</p>

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database_sql.png" width=700/>
</p>

## 任意代码执行

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/code_exec_eval.png" width=700/>
</p>

## Void

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/void.png" width=700/>
</p>

## SOCKS5

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/socks5.png" width=700/>
</p>

## Linux / Windows

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/regedit.png" width=700/>
</p>

## 插件

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/plugins.png" width=700/>
</p>

## 笔记

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/note.png" width=700/>
</p>

## Event Horizon

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/eventhorizon.png" width=700/>
</p>

## DriftingComet

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/comet.png" width=700/>
</p>

# 致谢

Alien 是我大学二年级开始开发的项目，也是我第一个突破 100 Stars 的开源项目。

由于当时我在软件工程和网络安全方面的经验还比较有限，早期版本在架构设计、可维护性和可扩展性方面存在不少问题，也难以应对现代 WAF 与 IDS 的防护机制。

希望 Alien 不仅仅是另一个 WebShell 管理工具，更是一款能够陪伴大家学习、研究和探索 Web 安全技术的开源项目。

感谢每一位支持这个项目的人。