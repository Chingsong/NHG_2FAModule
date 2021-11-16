using SP_2FAModule_Form.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SP_2FAModule_Form.Interface
{
    internal interface IActiveDirectoryConnection
    {
        List<UserEntityAD> GetUser(string path, string query = "");
    }
}
