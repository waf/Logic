using System;
using Logic.Engine;
using IQToolkit;

namespace Logic.Linq
{
    public class LogicContext
    {
        readonly QueryProvider provider = new QueryProvider();
        public Query<T> Variable<T>() => new Query<T>(provider);

        internal State Results()
        {
            return new State();
        }
    }
}
