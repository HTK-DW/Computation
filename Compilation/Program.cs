using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Heliatek.Computation.Compilation
{
    class Program
    {
        public static void Main()
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
                new Tuple<Type, string, string>(typeof(double), "(MAX(Produktlänge, 200) - 115)", "Substratlänge"),
                new Tuple<Type, string, string>(typeof(double), "ceil((Substratlänge - 8) / 100)", "Segmentanzahl"),
                new Tuple<Type, string, string>(typeof(double), "floor(Substratlänge - 8) / MIN(Segmentanzahl, 5)", "Segmentlänge"),
                new Tuple<Type, string, string>(typeof(double?), @"(Color_of_MBs == ""schwarz"") ? 70 : (Color_of_MBs == ""weiß"") ? 80 : null", "Heizertemperatur"),
                new Tuple<Type, string, string>(typeof(double?), @"(Substrat == ""Zulieferer A"") ? 85 : (Substrat == ""Zulieferer B"") ? 60 : null", "Laser_Leistung"),
                new Tuple<Type, string, string>(typeof(double), @"(Color_of_MBs == ""schwarz"") ? 4 : 3", "Untergrenze_Bandgeschwindigkeit"),
                //new Tuple<Type, string, string>(@"var product_length = 7; return (product_length - 115);"
            };

            CalculateSingleValue(formulas[0], variables.Take(3));
            CalculateSingleValue(formulas[1], variables.Take(4));
            CalculateAllValues(formulas, variables.Take(3));

            Console.ReadLine();
        }

        private static void CalculateAllValues(IEnumerable<Tuple<Type, string, string>> formulas, IEnumerable<Tuple<Type, string, object>> variables)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("public class POR {");

            foreach (var variable in variables)
            {
                var variableName = variable.Item1.Name;
                if (variable.Item1.IsGenericType)
                {
                    variableName = variable.Item1.ToString();
                }
                stringBuilder.AppendLine($"    private static {variableName} {variable.Item2} = {variable.Item3};");
            }

            foreach (var formula in formulas)
            {
                var variableName = formula.Item1.Name;
                if (formula.Item1.IsGenericType)
                {
                    variableName = $"{formula.Item1.Name.Replace("`1", "")}<{string.Join(',', formula.Item1.GetGenericArguments().Select(t => t.Name))}>";
                }
                stringBuilder.AppendLine($"    public static {variableName} {formula.Item3} = {ReplaceFunctions(formula.Item2)};");
            }

            stringBuilder.AppendLine("}");

            try
            {
                var result = Execute(stringBuilder.ToString(), "Segmentlänge");
                Console.WriteLine($"=> {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=> " + ex.Message);
            }
            Console.WriteLine();
        }

        private static void CalculateSingleValue(Tuple<Type, string, string> formula, IEnumerable<Tuple<Type, string, object>> variables)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("public class POR {");

            foreach (var variable in variables)
            {
                stringBuilder.AppendLine($"    private static {variable.Item1.Name} {variable.Item2} = {variable.Item3};");
            }

            stringBuilder.AppendLine($"    public static {formula.Item1.Name} {formula.Item3} = {ReplaceFunctions(formula.Item2)};");

            stringBuilder.AppendLine("}");

            try
            {
                var result = Execute(stringBuilder.ToString(), formula.Item3);
                Console.WriteLine($"=> {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=> " + ex.Message);
            }
            Console.WriteLine();
        }

        private static object Execute(string code, string method)
        {
            Console.WriteLine("Execute Code:");
            Console.WriteLine(code);

            var compilation = CSharpCompilation.Create(
                Guid.NewGuid().ToString(),
                new[] {
                    CSharpSyntaxTree.ParseText(code)
                },
                new[] {
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var cr = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                var createdType = assembly.ExportedTypes.FirstOrDefault();
                var fieldInfo = createdType.GetField(method, BindingFlags.Static | BindingFlags.Public);
                    
                return fieldInfo.GetValue(null);
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
    }
}
