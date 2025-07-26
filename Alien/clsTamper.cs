using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTamper
    {
        private string m_szPayloadData { get; set; }

        public clsTamper(string szPayloadData)
        {
            m_szPayloadData = szPayloadData;
        }
    }
}