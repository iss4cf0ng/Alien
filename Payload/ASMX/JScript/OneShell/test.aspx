<%

function Base64Decode(str) {
    var xml = new ActiveXObject("MSXML2.DOMDocument.3.0");
    var node = xml.createElement("b64");
    node.dataType = "bin.base64";
    node.text = str;

    var stream = new ActiveXObject("ADODB.Stream");
    stream.Type = 1; 
    stream.Open();
    stream.Write(node.nodeTypedValue);
    stream.Position = 0;
    stream.Type = 2; 
    stream.Charset = "utf-8";

    var decodedText = stream.ReadText();
    stream.Close();
    return decodedText;
}

var inputData = Request.Item["z0"]
if (inputData != "") {
    Response.Write(Base64Decode(inputData));
}

%>