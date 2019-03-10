using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace core
{
    public sealed class ExpressionStringBuilder : ExpressionVisitor
    {
        // ReSharper disable InconsistentNaming
        private readonly StringBuilder builder = new StringBuilder();

        private bool skipDot;

        public ExpressionStringBuilder(Expression expression)
        {
            // Start the visit ...
            Visit(expression);
        }

        /// <summary>
        /// Visit lambda
        /// </summary>
        /// <param name="node"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Any())
            {
                Out("(");
                Out(string.Join(", ", node.Parameters.Select(n => n.Name)));
                Out(") => ");
            }

            Visit(node.Body);
            return node;
        }

        /// <summary>
        /// Method call expression
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Visit(node.Expression);

            if (node.Arguments.Any())
            {
                Out("(");
                foreach (var expression in node.Arguments)
                {
                    Visit(expression);
                }

                Out(")");
            }

            return node;
        }

        /// <summary>
        /// Binary expression
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Out("(");
            Visit(node.Left);
            Out(" ");
            Out(ToString(node.NodeType));
            Out(" ");
            Visit(node.Right);
            Out(")");
            return node;
        }

        /// <summary>
        /// Arguments visit
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Out(node.Name);
            return node;
        }

        /// <summary>
        /// Visit property access
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == null)
            {
                Out(node.Member.DeclaringType == null
                    ? node.Member.Name
                    : $"{SimplifyType(node.Member.DeclaringType)}.{node.Member.Name}");
            }
            else if (node.Expression.NodeType == ExpressionType.Constant)
            {
                Visit(node.Expression);
                if (skipDot)
                {
                    skipDot = false;
                    Out(node.Member.Name);
                }
                else
                    Out("." + node.Member.Name);
            }
            else
            {
                Visit(node.Expression);
                Out("." + node.Member.Name);
            }

            return node;
        }
        
        /// <summary>
        /// Simplfies the type names
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string SimplifyType(Type type)
        {
            return type.FullName.Replace("System", "");
        }

        /// <summary>
        /// Checks whether type is anonymous type or not
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool CheckIfAnonymousType(Type type)
        {
            // Hack: the only way to detect anonymous types right now
            var isDefined = type.IsDefined(typeof(CompilerGeneratedAttribute), false);
            return isDefined
                   && (type.IsGenericType && type.Name.Contains("AnonymousType") || type.Name.Contains("DisplayClass"))
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"));
        }

        /// <summary>
        /// Visit constant literal expressions
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (CheckIfAnonymousType(node.Type))
            {
                skipDot = true;
                return node;
            }

            switch (node.Value)
            {
                case null:
                    Out("null");
                    break;
                case string stringValue:
                    Out("\"" + stringValue + "\"");
                    break;
                case bool _:
                    Out(node.Value.ToString().ToLower());
                    break;
                default:
                {
                    var valueToString = node.Value.ToString();
                    var type = node.Value.GetType();
                    if (type.FullName != valueToString)
                    {
                        Out(valueToString);
                    }
                    else
                    {
                        skipDot = true;
                    }

                    break;
                }
            }

            return node;
        }

        /// <summary>
        /// Visit unary expression (i.e. expression with one child node)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                    Visit(node.Operand);
                    return node;
                case ExpressionType.Not:
                    Out("!");
                    Visit(node.Operand);
                    return node;
                case ExpressionType.TypeAs:
                    Out("(");
                    Visit(node.Operand);
                    Out(" as " + node.Type.Name + ")");
                    return node;
                default:
                    return base.VisitUnary(node);
            }
        }

        /// <summary>
        /// Visit new invocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            Out("new " + node.Type.Name + "(");
            VisitArguments(node.Arguments.ToArray());
            Out(")");
            return node;
        }

        /// <summary>
        /// Visits static or instance method calls
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Visit(node.Object);

            if (!skipDot && !node.Method.IsStatic)
            {
                Out(".");
                skipDot = false;
            }
            else if (node.Method.IsStatic)
            {
                Out(node.Type.ToString());
                Out(".");
            }

            Out(node.Method.Name + "(");

            var args = node.Arguments.ToArray();

            VisitArguments(args);

            Out(")");
            return node;
        }

        /// <summary>
        /// Visit list of arguments
        /// </summary>
        /// <param name="arguments"></param>
        private void VisitArguments(IReadOnlyList<Expression> arguments)
        {
            var argindex = 0;
            while (argindex < arguments.Count)
            {
                Visit(arguments[argindex]);
                argindex++;

                if (argindex < arguments.Count)
                {
                    Out(", ");
                }
            }
        }

        /// <summary>
        /// Visit conditionals in-lines
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Visit(node.Test);
            Out(" ? ");
            Visit(node.IfTrue);
            Out(" : ");
            Visit(node.IfFalse);
            return node;
        }

        /// <summary>
        /// ToString definition of knowns expression types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static string ToString(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Negate:
                    return "-";
                case ExpressionType.Not:
                    return "!";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Coalesce:
                    return "??";
                case ExpressionType.ExclusiveOr:
                    return "^";
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Appends to the builder
        /// </summary>
        /// <param name="s"></param>
        private void Out(string s)
        {
            builder.Append(s);
        }

        /// <summary>
        /// ToString override of visitor class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return builder.ToString();
        }
    }
}