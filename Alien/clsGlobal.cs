using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsGlobal
    {

    }

    #region Enume

    public enum enLanguage
    {
        PHP,
        ASP,
        ASPX,
        ASMX,
        ASHX,
        JSP,
        JSPX,
        CGI,
        Perl,
        Python,
    }

    public enum enPayloadType
    {
        OneShell,
        Crypto,
    }

    public enum enLogMsgType
    {
        System,
        Error,
    }

    public enum enDatabase
    {
        DSN,
        MySQL,
        MySQLi,
        Access,
        SqlServer,
        Sqlite,
    }

    #endregion
    #region Struct

    public struct stShellConfig
    {
        public string ID;
        public string szUrl;
        public string szPassword;

        public string szGroupName;

        public enLanguage language;
        public string szMethod;
        public enPayloadType payloadType;

        public string szEncoding;

        public DateTime dtCreateDate;
        public DateTime dtLastModified;
        public DateTime dtLastAccessed;
    }

    public struct stFileEntry
    {
        public string szFilePath;
        public int nFileSize;
        public string szPriviledge;

        public DateTime dtCreationDate;
        public DateTime dtLastModifiedDate;
        public DateTime dtLastAccessedDate;
    }

    #endregion
}