<%

var servletPath = request.getServletPath();
var fullFilePath = String(request.getSession().getServletContext().getRealPath(servletPath));
var currentFileObj = new java.io.File(fullFilePath);
var szCurrentDir = String(currentFileObj.getParent());
var bUnixLike = java.io.File.separator.equals("/");

echo(szCurrentDir);
echo("|");

if (bUnixLike) {
    echo("/");
} else {
    var roots = java.io.File.listRoots();
    var aResult = [];
    for (var i = 0; i < roots.length; i++) {
        var path = roots[i].getAbsolutePath();
        if (path.endsWith("\\")) {
            path = path.substring(0, path.length() - 1);
        }
        aResult.push(path);
    }
    
    echo(aResult.join(","));
}

%>