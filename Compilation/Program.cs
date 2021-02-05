using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Heliatek.Computation.Compilation
{
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
                new Tuple<string, string, string>("double?", @"(Color_of_MBs == ""schwarz"") ? 4 : 3", "Untergrenze_Bandgeschwindigkeit"),
                //new Tuple<Type, string, string>(@"var product_length = 7; return (product_length - 115);"
            };

            await CalculateSingleValueAsync(formulas[0], variables);
            await CalculateSingleValueAsync(formulas[1], variables);
            await CalculateAllValuesAsync(formulas, new[] { variables[0], variables[1], variables[2] });

            Console.ReadLine();
        }

        private static async Task CalculateAllValuesAsync(IEnumerable<Tuple<string, string, string>> formulas, IEnumerable<Tuple<Type, string, object>> variables)
        {

        }

        private static async Task CalculateSingleValueAsync(Tuple<string, string, string> formula, IEnumerable<Tuple<Type, string, object>> variables)
        {

        }

        private static string Test()
        {
            var compilation = CSharpCompilation.Create(
    "DynamicAssembly", new[] { CSharpSyntaxTree.ParseText(@"
     public class DynamicClass {
        public int DynamicMethod(int a, int b) {
            return a-b;
        }
     }") }, new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) },
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var cr = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                var createdType = assembly.ExportedTypes.FirstOrDefault(x => x.Name == "DynamicClass");
                var methodInfo = createdType.GetMethod("DynamicMethod", BindingFlags.Instance | BindingFlags.Public);
                var instance = Activator.CreateInstance(createdType);

                var result = methodInfo.Invoke(instance, new object[] { 5, 3 });
            }
        }
    }
}
