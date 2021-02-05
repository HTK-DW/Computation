using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliatek.Computation.Script
{
    public class ScriptHost
    {
        public string Substrat { get; } = "Zulieferer A";
    }

    class Program
    {
        public static async Task Main(string[] args)
        {
            var variables = new[]
            {
                new Tuple<Type, string, object>(typeof(double), "Produktlänge", "237.8" ),
                new Tuple<Type, string, object>(typeof(string), "Color_of_MBs", "null" ),
                //new Tuple<Type, string, object>(typeof(string), "Color_of_MBs", "\"schwarz\"" ),
                new Tuple<Type, string, object>(typeof(string), "Substrat", "\"Zulieferer A\"" ),
                new Tuple<Type, string, object>(typeof(double), "Substratlänge", "122.8" ),
                new Tuple<Type, string, object>(typeof(double), "Segmentanzahl", "2" ),
                new Tuple<Type, string, object>(typeof(double), "Segmentlänge", "57.4" ),
            };
            var formulas = new[] {
                new Tuple<string, string, string>("double", "(MAX(Produktlänge, 200) - 115)", "Substratlänge"),
                new Tuple<string, string, string>("double", "ceil((Substratlänge - 8) / 100)", "Segmentanzahl"),
                new Tuple<string, string, string>("double", "floor(Substratlänge - 8) / MIN(Segmentanzahl, 5)", "Segmentlänge"),
                new Tuple<string, string, string>("double?", @"(Color_of_MBs == ""schwarz"") ? 70 : (Color_of_MBs == ""weiß"") ? 80 : null;", "Heizertemperatur"),
                new Tuple<string, string, string>("double?", @"(Substrat == ""Zulieferer A"") ? 85 : (Substrat == ""Zulieferer B"") ? 60 : null", "Laser_Leistung"),
                new Tuple<string, string, string>("double", @"(Color_of_MBs == ""schwarz"") ? 4 : 3", "Untergrenze_Bandgeschwindigkeit"),
                //new Tuple<Type, string, string>(@"var product_length = 7; return (product_length - 115);"
            };

            await CalculateSingleValueAsync(formulas[0], variables);
            await CalculateSingleValueAsync(formulas[1], variables);
            await CalculateAllValuesAsync(formulas, new[] { variables[0], variables[1], variables[2] });

            Console.ReadLine();
        }

        private static async Task CalculateAllValuesAsync(IEnumerable<Tuple<string, string, string>> formulas, IEnumerable<Tuple<Type, string, object>> variables)
        {
            var builder = new StringBuilder();
            foreach (var variable in variables)
            {
                builder.AppendLine($"{variable.Item1.Name} {variable.Item2} = {variable.Item3};");
            }

            foreach (var formula in formulas)
            {
                builder.Append($"{ formula.Item1} { formula.Item3} = ");
                builder.Append(ReplaceFunctions(formula.Item2));
                if (!formula.Item2.EndsWith(";"))
                {
                    builder.Append(';');
                }
                builder.AppendLine();
            }

            var state = await ExecuteScript(builder.ToString());
            if (state is ScriptState)
            {
                foreach (var value in state.Variables)
                {
                    Console.WriteLine($"{value.Type} {value.Name} = {value.Value};");
                }
                Console.WriteLine();
            }
        }

        private static async Task CalculateSingleValueAsync(Tuple<string, string, string> formula, IEnumerable<Tuple<Type, string, object>> variables)
        {
            var builder = new StringBuilder();
            foreach (var variable in variables)
            {
                builder.AppendLine($"{variable.Item1.Name} {variable.Item2} = {variable.Item3};");
            }

            builder.Append($"{formula.Item1} result = {ReplaceFunctions(formula.Item2)};");

            var state = await ExecuteScript(builder.ToString());
            if (state is ScriptState)
            {
                var result = state.GetVariable("result");
                Console.WriteLine($"{result.Type} {result.Name} = {result.Value};");
                Console.WriteLine();
            }
        }

        private static string ReplaceFunctions(string script)
        {
            return script
                .Replace("ceil", "Math.Ceiling", StringComparison.InvariantCultureIgnoreCase)
                .Replace("floor", "Math.Floor", StringComparison.InvariantCultureIgnoreCase)
                .Replace("min", "Math.Min", StringComparison.InvariantCultureIgnoreCase)
                .Replace("max", "Math.Max", StringComparison.InvariantCultureIgnoreCase);
        }

        private static async Task<ScriptState<object>> ExecuteScript(string script)
        {
            return await ExecuteScript<object>(script);
        }

        private static async Task<ScriptState<T>> ExecuteScript<T>(string script)
        {
            ScriptOptions scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.AddImports("System");

            Console.WriteLine("Execute Script:");
            Console.WriteLine(script);
            try
            {
                return await CSharpScript.RunAsync<T>(script, scriptOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=> " + ex.Message);
                return null;
            }
            Console.WriteLine();
        }
    }
}
