using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        DarkMatter,
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

        public string szDescription;
        public string szUserAgent;

        // EventHorizon
        public bool bRaw;
        public bool bEHEnable;
        public string szEventHorizonScript;
        public string szEventHorizonConfig; // JSON

        // DriftingComet
        public bool bDCEnable { get { return lsCometShellID.Count > 0; } }
        public string szDriftingComet;
        public List<string> lsCometShellID { get { return string.IsNullOrEmpty(szDriftingComet) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(szDriftingComet) ?? new List<string>(); } }

        public int nTimeout;
        public string szCookie;
        public string szExtraPost;
        public int nExtraPostPosition;
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