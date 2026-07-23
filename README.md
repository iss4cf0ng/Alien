# Alien

![status](https://img.shields.io/badge/status-development-orange)
![C#](https://img.shields.io/badge/language-C%23-blue)
![license](https://img.shields.io/badge/license-MIT-green)

Alien is a modular webshell client developed for cybersecurity research and education.

It provides a unified post-exploitation framework for managing different web technologies through reusable modules.

Rather than embedding every feature inside the webshell itself, Alien treats webshells as lighteight execution layers. Once arbitrary code execution is obtained, advanced capabilities-including file management, database interaction, virtual shells, SOCKS5 proxying, plugins, and reflective loading, are provided by Alien.

If you find this project helpful or informative, I would truly appreciate a ⭐ on the repository. Your support would be a great motivation for me to continue improving this tool!

# Disclaimer

Alien is developed for cybersecurity research, education, and authorized penetration testing only.

Do **NOT** use this project against systems without explicit authorization.

The author is **not responsible** for any misuse or damage caused by this software.

# Architecture

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/architecture.png" width=1000/>
</p>

# Requirements

Before running Alien, please ensure the following requirements are met:

- **Python 3** must be installed.
- The Python executable (`python` or `python3`) must be accessible from the command line (added to the system `PATH` environment variable).

You can verify your installation by running:

```batch
python --version
```

## Core Components

Alien consists of several independent components.

| Component | Description |
| --- | --- |
| **OneShell** | A lightweight webshell responsible only for arbitrary code execution. |
| **NebulaPulsar** | An in-memory implant supporting Java and .NET reflective loading. |
| **DarkMatter** | Payload executed by NebulaPulsar. |
| **Event Horizon** | HTTP traffic obfuscation framework with wrapper injection and tamper scripts. |
| **DriftingComet** | Webshell pivoting mechanism for multi-hop communication. |

# Supported Web Technologies

| Script | Server | Execution Method | Payload Type |
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

# Features

- File Manager
- Virtual Shell
- Database Manager
- Arbitrary Code Execution
- SOCKS5 Proxy
- Plugins
- Webshell Pivoting (DriftingComet)
- HTTP Traffic Obfuscation (Event Horizon)
- Reflective Loading (.NET/Java NebulaPulsar & DarkMatter)

# Screenshots

## Information

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/control_panel.png" width=700/>
</p>

## File

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/file.png" width=700/>
</p>

## Shell

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/shell.png" width=700/>
</p>

## Database

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database.png" width=700/>
</p>

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/database_sql.png" width=700/>
</p>

## Code Exec

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

## Plugins

<p align="center">
  <img src="https://iss4cf0ng.github.io/Alien/images/plugins.png" width=700/>
</p>

## Note

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

# Acknowledgements

Alien began as my first webshell management tool during my second year at university.

Throughout its development, I have learned a tremendous amount from open-source projects, technical articles, research papers, and the cybersecurity community. Although this project has been completely redesigned multiple times, every iteration has been a valuable learning experience.

I sincerely thank everyone who shares their knowledge with the community. Without those contributions, projects like Alien would not have been possible.

