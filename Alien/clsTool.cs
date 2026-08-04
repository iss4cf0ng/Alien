using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTool
    {
        public clsTool() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lv"></param>
        /// <returns></returns>
        public static List<ListViewItem> fnExtractListViewSelectedItems(ListView lv) => lv.SelectedItems.Cast<ListViewItem>().ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="web"></param>
        /// <returns></returns>
        public static T? fnFindForm<T>(clsWeb web) where T : Form => fnFindForm<T>(web.m_victim);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="victim"></param>
        /// <returns></returns>
        public static T? fnFindForm<T>(clsVictim victim) where T : Form
        {
            foreach (T form in Application.OpenForms.OfType<T>())
            {
                var prop = form.GetType().GetProperty("m_victim", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (prop == null)
                    continue;

                var value = prop.GetValue(form);
                if (value is clsVictim && ((clsVictim)value).ShellID == victim.ShellID)
                    return form;
            }

            return null;
        }

        /// <summary>
        /// Normalize bytes length.
        /// </summary>
        /// <param name="nSize">Bytes length</param>
        /// <returns>String value of normalized bytes length.</returns>
        public static string fnSizeNormalization(long nSize)
        {
            if (nSize < 0)
                return "[INVALID_SIZE]";

            string[] asSize = { "B", "KB", "MB", "GB", "TB" };
            double dSize = nSize;
            int nIndex = 0;

            while (dSize >= 1024 && nIndex < asSize.Length - 1)
            {
                dSize /= 1024;
                nIndex++;
            }

            return string.Format("{0:0.##} {1}", dSize, asSize[nIndex]);
        }

        /// <summary>
        /// Convert base64 string into Image object.
        /// </summary>
        /// <param name="szB64str">Image base64 string value.</param>
        /// <returns>Image object.</returns>
        public static Image? fnimgB64strToImage(string szB64str)
        {
            try
            {
                byte[] abBuffer = Convert.FromBase64String(szB64str);
                using (MemoryStream ms = new MemoryStream(abBuffer))
                {
                    return Image.FromStream(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szExt"></param>
        /// <returns></returns>
        public static string fnszGenerateFileNameWithDateTime(string szExt = "txt")
        {
            if (szExt.StartsWith("."))
                szExt = szExt.Replace(".", string.Empty);

            DateTime date = DateTime.Now;
            return string.Join(string.Empty, new int[]
            {
                date.Year,
                date.Month,
                date.Day,
                date.Hour,
                date.Minute,
                date.Second,
                date.Millisecond,
            }) + $".{szExt}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="szQuery"></param>
        /// <returns></returns>
        public static DataTable fnSqlQuery(SQLiteConnection conn, string szQuery)
        {
            DataTable dt = new DataTable();

            try
            {
                using (var data_adapter = new SQLiteDataAdapter(szQuery, conn))
                {
                    data_adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }
    }
}
