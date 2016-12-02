using Logic.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Linq
{
    class LinqToLogicVisitor : IQToolkit.ExpressionVisitor
    {
        Expression result = null;

        public Expression Translate(Expression input)
        {
            return this.Visit(input);
        }

        static MethodInfo CallFresh = typeof(LogicEngine).GetMethod(nameof(LogicEngine.CallFresh), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo Eq = typeof(LogicEngine).GetMethod(nameof(LogicEngine.Eq), BindingFlags.Public | BindingFlags.Static);

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                var whereLambda = StripQuotes(m.Arguments[1]);
                var freshLambda = this.ConvertLambdaToCallFresh(whereLambda as LambdaExpression);
                return Expression.Call(CallFresh, freshLambda);
            }
            return Visit(m.Arguments[0]);
        }

        private readonly IDictionary<string, Expression> LogicVariableParameters = new Dictionary<string, Expression>();

        private Expression ConvertLambdaToCallFresh(LambdaExpression lambda)
        {
            string parameterName = lambda.Parameters.Single().Name;
            var param = Expression.Parameter(typeof(LogicVariable), parameterName);
            LogicVariableParameters[parameterName] = param;

            var body = this.Visit(lambda.Body);

            return Expression.Lambda<
                Func<LogicVariable,
                     Func<State, IEnumerable<State>>>>(body, param);
        }

        private static Expression StripQuotes(Expression e) {
            while (e.NodeType == ExpressionType.Quote) {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }
        protected override Expression VisitParameter(ParameterExpression p)
        {
            return LogicVariableParameters.ContainsKey(p.Name)
                ? LogicVariableParameters[p.Name]
                : p;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            switch(b.NodeType)
            {
                case ExpressionType.Equal:
                    var left = Expression.Convert(Visit(b.Left), typeof(object));
                    var right = Expression.Convert(Visit(b.Right), typeof(object));
                    return Expression.Call(Eq, left, right);
            }
            return base.VisitBinary(b);
        }
    }
}
