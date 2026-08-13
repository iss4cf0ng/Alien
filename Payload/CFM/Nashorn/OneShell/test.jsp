<%

var b64Data = request.getParameter("z0");
if (b64Data != null) {
    var decodedBytes = java.util.Base64.getDecoder().decode(b64Data);
    var szPattern = new java.lang.String(decodedBytes, "UTF-8");
    
    echo(szPattern);

} else {
    echo("Error: z0 parameter is missing.");
}

%>