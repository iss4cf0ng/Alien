using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsInfoSpyder : clsWeb
    {
        public clsInfoSpyder(clsVictim victim) : base(victim)
        {
            m_victim = victim;
        }

        public string fnszGetInfo()
        {
            try
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"ERROR://{ex.Message}";
            }
        }
    }
}
