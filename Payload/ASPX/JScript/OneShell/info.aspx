<%

function TestObj(progId)
{
    try
    {
        var o = System.Web.HttpContext.Current.Server.CreateObject(progId);
        o = null;
        return "AVAILABLE";
    }
    catch(e)
    {
        return "NOT INSTALLED";
    }
}

function GetArchitecture()
{
    var arch = "";

    try
    {
        arch = String(System.Web.HttpContext.Current.Request.ServerVariables.Item("PROCESSOR_ARCHITECTURE"));
    }
    catch(e) {}

    if (arch == null || arch == "" || arch == "undefined")
    {
        try
        {
            var shell = System.Web.HttpContext.Current.Server.CreateObject("WScript.Shell");
            arch = String(shell.ExpandEnvironmentStrings("%PROCESSOR_ARCHITECTURE%"));
            shell = null;
        }
        catch(e) {}
    }

    if (arch == null || arch == "" || arch == "undefined")
    {
        try
        {
            var fso = System.Web.HttpContext.Current.Server.CreateObject("Scripting.FileSystemObject");
            if (fso.FolderExists("C:\\Program Files (x86)"))
                arch = "AMD64 (Simulated)";
            else
                arch = "x86 (Simulated)";
            fso = null;
        }
        catch(e) {}
    }

    if (arch == null || arch == "" || arch == "undefined")
        arch = "UNKNOWN";

    return arch;
}

Response.Buffer = true;
Response.ContentType = "text/html";

Response.Write("<table border='1' cellpadding='5' cellspacing='0'>");

// ASP.NET & SYSTEM
Response.Write("<tr><th colspan='2' align='left'>ASP.NET & SYSTEM</th></tr>");

try
{
    Response.Write("<tr><td>.NET Version</td><td>" + System.Environment.Version.ToString() + "</td></tr>");
}
catch(e) {}

try
{
    Response.Write("<tr><td>Timeout</td><td>" + System.Web.HttpContext.Current.Server.ScriptTimeout + "</td></tr>");
}
catch(e) {}

try
{
    Response.Write("<tr><td>SessionID</td><td>" + Session.SessionID + "</td></tr>");
}
catch(e) {}

Response.Write("<tr><td>Architecture</td><td>" + GetArchitecture() + "</td></tr>");

// CORE COM COMPONENTS
Response.Write("<tr><th colspan='2' align='left'>CORE COM COMPONENTS</th></tr>");
Response.Write("<tr><td>Scripting.FileSystemObject</td><td>" + TestObj("Scripting.FileSystemObject") + "</td></tr>");
Response.Write("<tr><td>Scripting.Dictionary</td><td>" + TestObj("Scripting.Dictionary") + "</td></tr>");
Response.Write("<tr><td>ADODB.Connection</td><td>" + TestObj("ADODB.Connection") + "</td></tr>");
Response.Write("<tr><td>ADODB.Recordset</td><td>" + TestObj("ADODB.Recordset") + "</td></tr>");
Response.Write("<tr><td>ADODB.Stream</td><td>" + TestObj("ADODB.Stream") + "</td></tr>");
Response.Write("<tr><td>MSXML2.DOMDocument.6.0</td><td>" + TestObj("MSXML2.DOMDocument.6.0") + "</td></tr>");
Response.Write("<tr><td>MSXML2.DOMDocument.3.0</td><td>" + TestObj("MSXML2.DOMDocument.3.0") + "</td></tr>");
Response.Write("<tr><td>MSXML2.ServerXMLHTTP.6.0</td><td>" + TestObj("MSXML2.ServerXMLHTTP.6.0") + "</td></tr>");
Response.Write("<tr><td>Microsoft.XMLHTTP</td><td>" + TestObj("Microsoft.XMLHTTP") + "</td></tr>");
Response.Write("<tr><td>WScript.Shell</td><td>" + TestObj("WScript.Shell") + "</td></tr>");
Response.Write("<tr><td>Shell.Application</td><td>" + TestObj("Shell.Application") + "</td></tr>");
Response.Write("<tr><td>CDO.Message</td><td>" + TestObj("CDO.Message") + "</td></tr>");

// SERVER VARIABLES
Response.Write("<tr><th colspan='2' align='left'>SERVER VARIABLES</th></tr>");

try
{
    var varsCount = System.Web.HttpContext.Current.Request.ServerVariables.Count;

    for (var i = 0; i < varsCount; i++)
    {
        var key = String(System.Web.HttpContext.Current.Request.ServerVariables.GetKey(i));
        var val = String(System.Web.HttpContext.Current.Request.ServerVariables.Item(i));

        if (key != "null" && key != "" && val != "null" && val != "")
        {
            Response.Write(
                "<tr><td>" +
                System.Web.HttpContext.Current.Server.HtmlEncode(key) +
                "</td><td>" +
                System.Web.HttpContext.Current.Server.HtmlEncode(val) +
                "</td></tr>"
            );
        }
    }
}
catch(e)
{
    Response.Write("<tr><td colspan='2'>Unable to loop ServerVariables: " + e.message + "</td></tr>");
}

Response.Write("</table>");
%>