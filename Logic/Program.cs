using Logic.Engine;
using Logic.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Logic.Engine.LogicEngine;

namespace Logic
{
    static class Program
    {
        static void Main()
        {
            // non linq-style query
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

            // linq style query
            var program = new LogicContext();
            var linqResults = from x in program.Variable<int>()
                              where x == 5
                              select program.Possibilities();


            PrettyPrint(linqResults.ToArray());

            Console.ReadKey();
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
        }
    }
}
