<%

var fso, drives, d, currentDir, result;

fso = Server.CreateObject("Scripting.FileSystemObject");

currentDir = Server.MapPath(".");

result = currentDir + "|";

drives = fso.Drives;

var first = true;

for (var e = new Enumerator(drives); !e.atEnd(); e.moveNext()) {
    d = e.item();
    if (first == false) {
        result = result + ",";
    }

    result = result + d.DriveLetter + ":";
    first = false;
}

Response.Write(result);

%>