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
        private readonly IDictionary<string, Expression> LogicVariableParameters = new Dictionary<string, Expression>();

        public Expression Translate(Expression input)
        {
            var methodCall = input as MethodCallExpression;
            // for simplicity, if we don't have a select call, add a passthrough select call.
            if(methodCall?.Method.Name != "Select")
            {
                var selectType = input.Type.GenericTypeArguments[0];
                var lambdaParam = ((LambdaExpression)StripQuotes(methodCall.Arguments[1])).Parameters[0].Name;
                var identityParam = Expression.Parameter(selectType, lambdaParam);
                var identity = Expression.Lambda(identityParam, identityParam);

                var wrappedInput = Expression.Call(
                    QueryableSelect.MakeGenericMethod(selectType, selectType),
                    input,
                    identity);
                return this.Visit(wrappedInput);
            }
            return this.Visit(input);
        }

        static MethodInfo CallFresh = typeof(LogicEngine).GetMethod(nameof(LogicEngine.CallFresh), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo Eq = typeof(LogicEngine).GetMethod(nameof(LogicEngine.Eq), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo Disj = typeof(LogicEngine).GetMethod(nameof(LogicEngine.Disj), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo Conj = typeof(LogicEngine).GetMethod(nameof(LogicEngine.Conj), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo ExtractVariable = typeof(State).GetMethod(nameof(State.Extract), BindingFlags.Public | BindingFlags.Static);
        static MethodInfo QueryableSelect = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Select");
        static MethodInfo EnumerableSelect = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Select");

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                var whereLambda = StripQuotes(m.Arguments[1]);
                var freshLambda = this.ConvertLambdaToCallFresh(whereLambda as LambdaExpression);
                return Expression.Call(CallFresh, freshLambda);
            }
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Select")
            {
                var args = Visit(m.Arguments[0]);
                var selectLambda = StripQuotes(m.Arguments[1]) as LambdaExpression;

                var stateParam = Expression.Parameter(typeof(State), "s");
                var selectorParams = selectLambda.Parameters
                    .Select(param => Expression.Convert(
                                        Expression.Call(ExtractVariable, stateParam, Expression.Constant(param.Name)),
                                        param.Type))
                    .ToArray();
                var selectorLambda = Expression.Lambda(Expression.Invoke(selectLambda, selectorParams), stateParam);

                var outerStateParam = Expression.Parameter(typeof(State), "initialState");
                var result = Expression.Lambda(
                    Expression.Call(EnumerableSelect.MakeGenericMethod(typeof(State), selectLambda.ReturnType), Expression.Invoke(args, outerStateParam), selectorLambda),
                    outerStateParam
                );
                return result;
            }
            return Visit(m.Arguments[0]);
        }

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
            Expression left, right;
            switch(b.NodeType)
            {
                case ExpressionType.Equal:
                    left = Expression.Convert(Visit(b.Left), typeof(object));
                    right = Expression.Convert(Visit(b.Right), typeof(object));
                    return Expression.Call(Eq, left, right);
                case ExpressionType.OrElse:
                    left = Expression.Convert(Visit(b.Left), typeof(Func<State, IEnumerable<State>>));
                    right = Expression.Convert(Visit(b.Right), typeof(Func<State, IEnumerable<State>>));
                    return Expression.Call(Disj, left, right);
                case ExpressionType.AndAlso:
                    left = Expression.Convert(Visit(b.Left), typeof(Func<State, IEnumerable<State>>));
                    right = Expression.Convert(Visit(b.Right), typeof(Func<State, IEnumerable<State>>));
                    return Expression.Call(Conj, left, right);
            }
            return base.VisitBinary(b);
        }
    }
}
