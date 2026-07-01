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
        Ruby,
        Perl,
        Python,
        CFM,
    }

    public enum enPayloadType
    {
        // Plaintext or obfuscated
        OneShell,

        // Protected via cryptographic algorithm
        ECDH,
        NebulaPulsar,
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
        Access,
        SQLServer,
        PostgreSQL,
        SQLite,
        ODBC,
        Oracle,
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

        public string szUserAgent;

        // EventHorizon
        public bool bRaw;
        public bool bEHEnable;
        public string szEventHorizonScript;
        public string szEventHorizonConfig; // JSON

        // Wormhole
        public bool bWHEnable;
        public string szWormhole;
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