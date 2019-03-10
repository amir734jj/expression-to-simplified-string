using System;
using System.Globalization;
using System.Linq.Expressions;
using core.Extensions;
using Xunit;

namespace core.Tests
{
    public class ExpressionStringBuilderTest
    {
        [Fact]
        public void Test__ComplexExpr()
        {
            // Arrange
            Expression<Func<int, decimal, bool>> expression = (x, y) =>
                (decimal) x > y ? decimal.Parse("123") > y : y.ToString(CultureInfo.InvariantCulture) != null;

            // Act
            var str = expression.ToSimplifiedString();

            // Assert
            Assert.NotEmpty(str);
        }
    }
}