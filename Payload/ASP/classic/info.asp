<%
For Each item In Request.ServerVariables
    Response.Write item & " = " & Request.ServerVariables(item) & "<br>"
Next
%>