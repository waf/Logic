/*
 * This is a translation of http://webyrd.net/scheme-2013/papers/HemannMuKanren2013.pdf from scheme to C#.
 * All variable names, and most of the comments, are from that paper.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

// a logic variable is simply a label
using LogicVariable = System.String;
// a stream is a series of states
using Stream = System.Collections.Generic.IEnumerable<Logic.State>;

namespace Logic
{
    /*
     * Set up some types / aliases so our function signatures match what's in the whitepaper.
     */

    // substitution is our currently known variable/value associations. The value could be another logic variable.
    using Substitution = ImmutableDictionary<LogicVariable, object>;
    // a goal can either succeed or fail
    using Goal = Func<State, Stream>;
    class State
    {
        public Substitution Substitution { get; set; } = Substitution.Empty;
        public int FreshVariableCounter { get; set; }
    }

    static class LogicEngine
    {
        /// <summary>
        /// The walk operator searches for a variable's value in the substitution
        /// </summary>
        private static object Walk(object u, Substitution s)
        {
            // When a non-variable term is walked, the term itself is returned
            var uvar = u as LogicVariable;
            if(uvar == null || !s.ContainsKey(uvar))
            {
                return u;
            }
            return Walk(s[uvar], s);
        }

        /// <summary>
        /// The ext-s operator extends the substitution with a new binding.
        /// When extending the substitution, the first argument is always a
        /// variable, and the second is an arbitrary term. 
        /// </summary>
        private static Substitution ExtS(LogicVariable x, object v, Substitution s)
        {
            return s.Add(x, v);
        }

        /// <summary>
        /// Terms of the language are defined by the unify operator.
        /// </summary>
        private static Substitution Unify(object uorig, object vorig, Substitution s)
        {
            // To unify two terms in a substitution, both are walked in that substitution
            var u = Walk(uorig, s);
            var v = Walk(vorig, s);

            var uvar = u as LogicVariable;
            var vvar = v as LogicVariable;
            // If the two terms walk to the same variable, the original substitution is returned unchanged
            if (uvar != null && vvar != null && uvar == vvar)
            {
                return s;
            }
            // When one of the two terms walks to a variable, the substitution is extended, binding the
            // variable to which that term walks with the value to which the other term walks
            if(uvar != null)
            {
                return ExtS(uvar, v, s);
            }
            if(vvar != null)
            {
                return ExtS(vvar, u, s);
            }
            // If both terms walk to pairs, the cars and then cdrs are unified recursively, succeeding if
            // unification succeeds in the one and then the other.
            var uenumerable = u as IEnumerable<object>;
            var venumerable = v as IEnumerable<object>;
            if(uenumerable != null && venumerable != null)
            {
                var s1 = Unify(uenumerable.First(), venumerable.First(), s);
                if(s1 == null)
                {
                    return null;
                }
                return Unify(uenumerable.Skip(1), venumerable.Skip(1), s1);
            }

            // Finally, non-variable, non-pair terms unify if they are identical under eqv?, and unification fails otherwise.
            return u == v ? s : null;
        }

        /// <summary>
        /// Takes two terms as arguments and returns a goal that succeeds if those two terms unify in the received state.
        /// </summary>
        public static Goal Eq(object u, object v)
        {
            return sc =>
            {
                //If they unify, a substitution, possibly extended, is returned.
                var s = Unify(u, v, sc.Substitution);
                return s != null
                    // pass this new substitution, paired with the variable counter to comprise a state, to unit.
                    ? Unit(new State { Substitution = s, FreshVariableCounter = sc.FreshVariableCounter })
                    // If those two terms fail to unify in that state, the empty stream, mzero, is instead returned
                    : MZero;
            };
        }

        /// <summary>
        /// Unit lifts a state into a stream whose only element is that state.
        /// </summary>
        private static Stream Unit(State state) => new[] { state };

        /// <summary>
        /// An empty stream
        /// </summary>
        private static readonly Stream MZero = Enumerable.Empty<State>();

        /// <summary>
        /// The call/fresh goal constructor takes a unary function f whose body
        /// is a goal, and itself returns a goal
        /// </summary>
        public static Goal CallFresh(Func<LogicVariable, Goal> f)
        {
            // This returned goal, when provided a state s/c...
            return sc =>
            {
                var c = sc.FreshVariableCounter;
                return f
                    // binds the formal parameter of f to a new logic variable,
                    .Invoke(f.Method.GetParameters()[0].Name)
                    // and passes a state, with the substitution it originally received 
                    // and a newly incremented fresh-variable counter, c, to the goal
                    // that is the body of f.
                    .Invoke(new State
                    {
                        Substitution = sc.Substitution,
                        FreshVariableCounter = c + 1
                    });
            };
        }

        /// <summary>
        /// The disj goal constructor takes two goals as arguments and returns
        /// a goal that succeeds if either of the two subgoals succeed.
        /// </summary>
        public static Goal Disj(Goal g1, Goal g2)
        {
            return sc => MPlus(g1(sc), g2(sc));
        }

        /// <summary>
        /// The mplus operator is responsible for merging streams. In a goal constructed
        /// from disj, the resulting stream contains the states that result from success
        /// of either of the two goals. mplus simply appends the list returned as the result
        /// of the first call to that returned as the result of the second. In this form it
        /// is simply an implementation of append.
        /// </summary>
        private static Stream MPlus(Stream s1, Stream s2)
        {
            return s1 == null || !s1.Any()
                ? s2
                : s1.Take(1).Concat(MPlus(s1.Skip(1), s2));
        }
        
        /// <summary>
        /// The conj goal constructor takes two goals as arguments and returns a goal
        /// that succeeds if both goals succeed for that state.
        /// </summary>
        public static Goal Conj(Goal g1, Goal g2)
        {
            return sc => Bind(g1(sc), g2);
        }

        /// <summary>
        /// In bind the goal (g) is invoked on each element of the stream. 
        /// The bind operator is essentially an implemementation of append-map, though with
        /// its arguments reversed.
        /// </summary>
        private static Stream Bind(Stream s, Goal g)
        {
            return s == null || !s.Any()
                //If the stream of results of g is empty or becomes exhausted the empty stream is returned.
                ? MZero
                // If instead the stream contains a state and potentially more, then g is invoked on
                // the first state. The stream which is the result of that invocation is merged to a
                // stream containing the invocation of the rest of the $ passed in the second goal g.
                : MPlus(g(s.First()), Bind(s.Skip(1), g));
        }
    }
}
