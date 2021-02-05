using System;
using System.Linq.Expressions;
using Exp = System.Linq.Expressions.Expression;

namespace Heliatek.Computation.Expression
{
    class Program
    {
        public static void Main()
        {
            Expression<Func<double, double>> Substratlänge = (Produktlänge) => (Math.Max(Produktlänge, 200) - 115);
            var exp = Substratlänge.Compile();
            
            // Parameter/Constants
            var parameter = Exp.Parameter(typeof(double), "Produktlänge");
            var constant1 = Exp.Constant(200.0, typeof(double));
            var constant2 = Exp.Constant(115.0, typeof(double));

            // Math.Max()
            var maxMethod = typeof(Math).GetMethod("Max", new Type[] { typeof(double), typeof(double) });
            var callExpression = Exp.Call(null, maxMethod, new Exp[] { parameter, constant1 });

            // Substract
            var substractExpression = Exp.Subtract(callExpression, constant2);

            // t.Total == 100.00M 
            var func = Exp.Lambda<Func<double, double>>(substractExpression, parameter).Compile();

            Console.WriteLine($"Expr => {exp(238.7)}");
            Console.WriteLine($"Lbda => {func(238.7)}");
        }
    }
}
