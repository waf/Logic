using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Logic.Engine.LogicEngine;
using Logic.Engine;
using System.Linq;
using System.Collections.Immutable;
using Logic.Linq;

namespace Logic.Tests
{
    [TestClass]
    public class LinqSyntaxTests
    {
        [TestMethod]
        public void Statement_WithSimpleSelect()
        {
            var program = new LogicContext();
            var result = (from x in program.Variable<int>()
                          where x == 5
                          select x).ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, result[0]);
        }

        [TestMethod]
        public void Statement_WithAnonymousObjectSelect()
        {
            var program = new LogicContext();
            var result = (from x in program.Variable<int>()
                          where x == 5
                          select new { x }).ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, result[0].x);
        }
    }
}
