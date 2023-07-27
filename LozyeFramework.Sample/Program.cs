using LozyeFramework.Common.Templates;

namespace LozyeFramework.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var expressions = new[] {
                    "(a+b*c)/2-a",
                    "a+b*c",
                    "a >= b",
                    "a != b",
                    "a + str",
                    "a == str",
                    "a < str",
                    "a ?? b-c",
                    "a..str.Length",
                    "a/(b)-c*n",
                    "a/(b*a^2)",
                    "(a+b*c)/2+a/(b*a^2)-a+b*cc*(v+a)",
                    "(a+b)/2+a/(b-a^2*5)",
                    "calc('1+2')  +   -5/(b*c)",
                    "'name=>'  .. \"\\\"a1111+   15*6(  a*6 )\"",
                    "(2*a + (3*b)) / (4*n)",
                    "!a && b && c || !c",
                    "add(calc('a+ b'),c) +  5*   dev(  a, b)",
                    "tem()",
            };
            foreach (var item in expressions)
            {
                var root = TemplateBinary.Instance.Interpret(item);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("expressin is {0}", item);               
                Console.WriteLine("root is {0}", TemplateBinary.Instance.Lex(root.BinOpr));
                Console.WriteLine(root.ToString());
                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}