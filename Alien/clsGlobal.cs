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

    public enum Language
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

    public enum PayloadType
    {
        OneShell,
        Crypto,
    }

    public enum enDatabaseType
    {
        MySQL,

    }

    public enum enLogMsgType
    {
        System,
        Error,
    }

    #endregion
    #region Struct

    public struct stShellConfig
    {
        public string ID;
        public string szUrl;
        public string szPassword;

        public string szGroupName;

        public Language language;
        public string szMethod;
        public PayloadType payloadType;

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