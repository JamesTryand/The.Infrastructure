using System;
using System.Linq;

namespace Infrastructure {
	
	// Unfortunately, C#'s standard Tuple type is invariant, so this is necessary.
    public interface Pair<out A, out B>
    {
        public A V1 { get; }
        public B V2 { get; }
    }
    public static class Pair
    {
        public static Pair<A, B> Create<A, B>(A a, B b)
        {
            return new PairImpl<A, B>(a, b);
        }
        private class PairImpl<A, B> : Pair<A, B>
        {
            private readonly A v1;
            private readonly B v2;
            public PairImpl(A a, B b)
            {
                v1 = a;
                v2 = b;
            }
            public A V1
            {
                get { return v1; }
            }
            public A V2
            {
                get { return v2; }
            }
        }
    }    
	
	public delegate Pair<A, O> IState<in I, out O, out A>(I i);
	
    public static class IState
    {
        public static Pair<A, O> Run<I, O, A>(this IState<I, O, A> st, I i)
        {
            return st(i);
        }
        public static A Eval<I, O, A>(this IState<I, O, A> st, I i)
        {
            return st(i).V1;
        }
        public static O Exec<I, O, A>(this IState<I, O, A> st, I i)
        {
            return st(i).V2;
        }
		
		 // unit
        public static IState<S, S, A> ToIState<S, A>(this A a)
        {
            return (s => Pair.Create<A, S>(a, s));
        }

        // map
        public static IState<I, O, B> Select<I, O, A, B>(this IState<I, O, A> st, Func<A, B> func)
        {
            return (i =>
            {
                var ao = st.Run(i);
                return Pair.Create<B, O>(func(ao.V1), ao.V2);
            });
        }

        // join
        public static IState<I, O, A> Flatten<I, M, O, A>(this IState<I, M, IState<M, O, A>> st)
        {
            return (i =>
            {
                var qm = st.Run(i);
                return qm.V1.Run(qm.V2);
            });
        }

        // bind
        public static IState<I, O, B> SelectMany<I, M, O, A, B>(this IState<I, M, A> st, Func<A, IState<M, O, B>> func)
        {
            return (i =>
            {
                var am = st.Run(i);
                return func(am.V1).Run(am.V2);
            });
        }

        // bindMap
        public static IState<I, O, C> SelectMany<I, M, O, A, B, C>(this IState<I, M, A> st, Func<A, IState<M, O, B>> func, Func<A, B, C> selector)
        {
            return (i =>
            {
                var am = st.Run(i);
                var a = am.V1;
                var bo = func(a).Run(am.V2);
                return Pair.Create<C, O>(selector(a, bo.V1), bo.V2);
            });
        }
		
		        public static IState<S, S, S> Get<S>()
        {
            return (s => Pair.Create<S, S>(s, s));
        }
        public static IState<I, O, Unit> Put<I, O>(O o)
        {
            return (_ => Pair.Create<Unit, O>(Unit.Default, o));
        }
        public static IState<I, O, Unit> Modify<I, O>(Func<I, O> func)
        {
            return (i => Pair.Create<Unit, O>(Unit.Default, func(i)));
        }
    }

}