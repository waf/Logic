using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using IQToolkit;
using Logic.Engine;

namespace Logic.Linq
{
    class QueryProvider : IQToolkit.QueryProvider
    {
        public override object Execute(Expression expression)
        {
            var logicExpression = new LinqToLogicVisitor().Translate(expression);
            var logicFuncExpression = Expression.Lambda<Func<Func<State, IEnumerable<State>>>>(logicExpression);
            var compiled = logicFuncExpression.Compile();
            var logicProgram = compiled();
            var answers = logicProgram(new State());
            return answers;
        }

        public override string GetQueryText(Expression expression)
        {
            return "TODO";
        }
    }
}
