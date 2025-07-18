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
        ASHX,
        ASMX,
        JSP,
        JSPX,
        GO,
        PYTHON,
    }

    public enum PayloadType
    {
        RegularOneShell,
        CryptoOneShell,
    }

    #endregion

    #region Struct

    public struct stShellConfig
    {
        public string ID;
        public string szUrl;
        public string szPassword;

        public Language language;
        public PayloadType payloadType;

        public DateTime dtCreateDate;
        public DateTime dtLastModified;
        public DateTime dtLastAccessed;
    }

    #endregion
}