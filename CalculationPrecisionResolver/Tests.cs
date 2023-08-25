using CalculationPrecisionResolver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GeneratorTests.Tests
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void EntryPoint(string[] args)
        {
            Console.WriteLine(0.1 + 0.2 == 0.3);
            Console.WriteLine(0.1 + 0.3 >= 0.3);
            Console.WriteLine(0.1 * 2 <= 0.3);
        }
    }
}
");

            // directly create an instance of the generator
            ComparisonConverterGenerator generator = new ComparisonConverterGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic);
            }

            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators

            // Output the generated source code
            var generatedSyntaxTrees = driver.GetRunResult().GeneratedTrees;
            foreach (var syntaxTree in generatedSyntaxTrees)
            {
                Console.WriteLine(syntaxTree.ToString());
                Console.WriteLine("--------------------------------------");
            }
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

