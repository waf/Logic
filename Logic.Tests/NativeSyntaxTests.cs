using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Logic.Engine.LogicEngine;
using Logic.Engine;
using System.Linq;
using System.Collections.Immutable;

namespace Logic.Tests
{
    [TestClass]
    public class NativeSyntaxTests
    {
        [TestMethod]
        public void Conj_Eq_Test()
        {
            var facts = CallFresh(x =>
                CallFresh(y =>
                    Conj(
                        Eq(x, 5),
                        Eq(x, y)
                    )
                )
            );

            var result = facts(new State()).ToArray();
            Assert.AreEqual(1, result.Length);
            var substitution = result[0].Substitution;
            Assert.AreEqual(5, LookupValue(substitution, "x"));
            Assert.AreEqual(5, LookupValue(substitution, "y"));
        }

        private object LookupValue(ImmutableDictionary<LogicVariable, object> substitution, string name)
        {
            return substitution
                .Single(kvp => kvp.Key.Name == name)
                .Value;
        }
    }
}
