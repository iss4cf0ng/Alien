using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsLanguage
    {
        public clsLanguage()
        {

        }

        public class clsLanguageItem
        {
            public string Name { get; set; } = "";
            public string Culture { get; set; } = "";
        }

        public static class clsLanguageManager
        {
            public static List<clsLanguageItem> Languages { get; } =
            [
                new()
                {
                    Name = "English",
                    Culture = "Default",
                },
                new()
                {
                    Name = "繁體中文",
                    Culture = "zh-Hant",
                },
                new()
                {
                    Name = "简体中文",
                    Culture = "zh-Hans",
                },
                new()
                {
                    Name = "日本語",
                    Culture = "ja"
                },

                new()
                {
                    Name = "한국어",
                    Culture = "ko"
                }
            ];
        }
    }
}
