# Alien

[English](README.md) | [繁體中文](README.zh-TW.md) | [简体中文](README.zh-CN.md)

![status](https://img.shields.io/badge/status-development-orange)
![C#](https://img.shields.io/badge/language-C%23-blue)
![license](https://img.shields.io/badge/license-MIT-green)

Alien（外星人）是一套以模組化設計為核心的 WebShell 管理框架，主要作為滲透測試、資安研究與教育用途。

Alien 內建檔案管理、虛擬終端、資料庫管理等後滲透功能，並提供外掛系統，讓使用者能依需求自行擴充功能。

要是你喜歡這項目，請按一個星吧⭐！ 你的支持是我更新這工具的最大動力！

# 免責聲明

編寫 Alien 的目的是為了合法滲透測試、CTF 和學習用途。使用者不得在未授權的情況下攻擊受保護的設備。

作者對於一切的非法用途和因為這工具而導致的破壞一概不負責。

# 架構

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/architecture.png" width=1000/>
</p>

# 系統要求

Alien 需要安裝 Python 3，並確保 `python` 指令已加入系統的 PATH 環境變數。

如果你不確定是否安裝成功，你可以使用以下的指令來驗證：

```batch
python --version
```

## 核心模組

Alien 包括了以下的模組：

| 模組 | 說明 |
| --- | --- |
| **OneShell** | 一句話木馬，負責在目標伺服器上提供 Arbitrary Code Execution（任意程式碼執行）能力。 |
| **NebulaPulsar** | Java 與 .NET 記憶體植入框架，支援 Reflective Loading。 |
| **DarkMatter** | 被 NebulaPulsar 執行的 Payload |
| **Event Horizon** | HTTP 流量混淆框架。 |
| **DriftingComet** | WebShell 多重跳板（Pivoting）框架。 |

# 支援目標環境

| 腳本 | 伺服器 | 執行方式 | Payload 類型 |
| --- | --- | --- | --- |
| PHP | Linux/Windows | v5.X | OneShell |
| | | v7.X, v8.X | OneShell, ECDH (Elliptic Curve Diffie-Hellman) |
| ASP | Windows | Classic | OneShell |
| ASPX, ASHX, ASMX | Windows | JScript | OneShell |
| | Linux/Windows | NebulaPulsar | DarkMatter |
| JSP | Linux/Windows | Nashorn | OneShell |
| | | NebulaPulsar | DarkMatter |
| JSPX | Linux/Windows | Nashorn | OneShell |
| | | NebulaPulsar | DarkMatter |
| CFML | Linux/Windows | NebulaPulsar | DarkMatter |
| Perl | Linux/Windows | CGI | OneShell |
| Ruby | Linux/Windows | CGI | OneShell |

# 功能

- 檔案管理
- 虛擬終端
- 資料庫管理
- 任意程式執行
- SOCKS5 代理
- 插件
- Webshell 跳板 (DriftingComet)
- HTTP 流量混淆 (Event Horizon)
- Reflective Loading（.NET／Java）

# 螢幕截圖

## 基本資料

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/control_panel.png" width=700/>
</p>

## 檔案管理

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/file.png" width=700/>
</p>

## 終端

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/shell.png" width=700/>
</p>

## 資料庫

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database.png" width=700/>
</p>

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database_sql.png" width=700/>
</p>

## 任意程式執行

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

## Linux/Windows

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/regedit.png" width=700/>
</p>

## 插件

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/plugins.png" width=700/>
</p>

## 筆記

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

# 感謝

Alien 是我在大學二年級時開始開發的，也是我第一個突破 100 Stars 的開源專案。

由於當時在軟體工程與資安領域的經驗仍然有限，早期版本在架構設計、可維護性與擴充性方面都存在不少不足，也難以因應現代 WAF 與 IDS 的防護機制。

希望 Alien 不只是另一個 WebShell 管理工具，而是一個能陪伴大家學習、研究與探索 Web 安全技術的開源專案。

謝謝每一位支持這個專案的人。
