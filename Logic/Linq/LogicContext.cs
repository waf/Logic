using IQToolkit;
using Logic.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Linq
{
    public class LogicContext
    {
        readonly QueryProvider provider = new QueryProvider();
        public Query<T> Variable<T>() => new Query<T>(provider);

        internal object Possibilities(int x)
        {
            return null;
        }

        internal State Possibilities()
        {
            return null;
        }
    }
}
