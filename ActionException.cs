using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidationExe
{

    class ActionException : Exception
    {
        public ActionException(string s) : base(s)
        {
        }
    }
}
