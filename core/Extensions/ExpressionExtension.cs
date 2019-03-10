using System.Linq.Expressions;

namespace core.Extensions
{
    public static class ExpressionExtensions {

        /// <summary>
        /// A nicely formatted ToString of an expression
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSimplifiedString(this Expression source)
        {
            return new ExpressionStringBuilder(source).ToString();
        }
    }
}