using System;
using System.Linq;
using static Logic.LogicEngine;

namespace Logic
{
    static class Program
    {
        static void Main()
        {
            // set up logic relations
            var facts = CallFresh(x =>
                CallFresh(y =>
                    Conj(
                        Eq(x, 5),
                        Eq(x, y)
                    )
                )
            );

            // run the logic engine
            var result = facts(new State()).ToArray();

            PrettyPrint(result);
        }

        private static void PrettyPrint(State[] result)
        {
            for (int i = 0; i < result.Length; i++)
            {
                Console.WriteLine($"Result {i + 1}");
                Console.WriteLine(string.Join(Environment.NewLine,
                    result[i].Substitution.Select(kvp => $"{kvp.Key} : {kvp.Value}")
                ));
            }
            Console.ReadKey();
        }
    }
}
