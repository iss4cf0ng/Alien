using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTool
    {
        public clsTool() { }

        public static List<ListViewItem> fnExtractListViewSelectedItems(ListView lv) => lv.SelectedItems.Cast<ListViewItem>().ToList();
    }
}
