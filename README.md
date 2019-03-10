# expression-to-simplified-string
Converts c# expression to simplified string using visitor pattern

Given:

```csharp
Expression<Func<int, decimal, bool>> expression = (x, y) =>
    (decimal) x > y ? decimal.Parse("123") > y : y.ToString(CultureInfo.InvariantCulture) != null;


string str = expression.ToSimplifiedString();
```

Code returns:
```
// (x, y) => (x > y) ? (System.Decimal.Parse("123") > y) : (y.ToString(System.Globalization.CultureInfo.InvariantCulture) != null)
```
