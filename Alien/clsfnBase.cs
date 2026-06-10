using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnBase
    {
        public clsfnBase()
        {

        }

        protected const string INIT = "init";
        protected const string TEST = "test";

        protected const string INFO = "info";

        protected const string DB_INIT = "db_init";
        protected const string DB_QUERY = "db_query";

        protected const string FILE_INIT = "file_init";
        protected const string FILE_CURRENTDIR = "file_cd";
        protected const string FILE_COPY = "file_copy";
        protected const string FILE_MOVE = "file_move";
        protected const string FILE_DELETE = "file_delete";
        protected const string FILE_DOWNLOAD = "file_download";
        protected const string FILE_UPLOAD = "file_upload";

        protected const string SHELL_VIRTUAL = "shell_virtual";

    }
}
