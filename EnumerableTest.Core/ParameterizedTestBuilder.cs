using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnumerableTest.Sdk;

namespace EnumerableTest
{
    /// <summary>
    /// Represents a builder to create a parameterized test.
    /// <para lang="ja">
    /// パラメーター化されたテストを生成するためのものを表す。
    /// </para>
    /// </summary>
    /// <typeparam name="TParameter"></typeparam>
    public struct ParameterizedTestBuilder<TParameter>
    {
        List<TParameter> Parameters { get; }

        internal ParameterizedTestBuilder(List<TParameter> parameters)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public ParameterizedTestBuilder<TParameter>
            Case(TParameter parameter)
        {
            Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        public IEnumerable<Test> Run(Func<TParameter, IEnumerable<Test>> run)
        {
            foreach (var parameter in Parameters)
            {
                var data =
                    DictionaryTestData.Build()
                    .Add("Parameter", parameter)
                    .MakeReadOnly();
                yield return run(parameter).ToTestGroup(nameof(Case), data);
            }
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        public IEnumerable<Test> Run(Func<TParameter, Test> run)
        {
            return Run(x => new[] { run(x) });
        }
    }

    /// <summary>
    /// Provides static methods and extension methods
    /// related to <see cref="ParameterizedTestBuilder"/>.
    /// </summary>
    public static class ParameterizedTestBuilder
    {
        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<X>
            Case<X>(X parameter)
        {
            return new ParameterizedTestBuilder<X>(new List<X>()).Case(parameter);
        }

#if false
    for n in 2..7 do
        @"
        /// <summary>
        /// Adds a test case.
        /// <para lang=''ja''>
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<TS>>
            Case<TS>(this ParameterizedTestBuilder<Tuple<TS>> @this, PS)
        {
            return @this.Case(Tuple.Create(XS));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang=''ja''>
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<TS>>
            Case<TS>(PS)
        {
            return new ParameterizedTestBuilder<Tuple<TS>>(new List<Tuple<TS>>()).Case(XS);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang=''ja''>
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<TS>(this ParameterizedTestBuilder<Tuple<TS>> @this, Func<TS, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(IS));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang=''ja''>
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<TS>(this ParameterizedTestBuilder<Tuple<TS>> @this, Func<TS, Test> run)
        {
            return @this.Run(t => run(IS));
        }
        " .Replace("''", string '"')
          .Replace("TS", [for i in 0..(n - 1) -> sprintf "X%d" i] |> String.concat ", ")
          .Replace("PS", [for i in 0..(n - 1) -> sprintf "X%d x%d" i i] |> String.concat ", ")
          .Replace("XS", [for i in 0..(n - 1) -> sprintf "x%d" i] |> String.concat ", ")
          .Replace("IS", [for i in 0..(n - 1) -> sprintf "t.Item%d" (i + 1)] |> String.concat ", ")
        |> printf "%s"
    ;;
#endif

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1>>
            Case<X0, X1>(this ParameterizedTestBuilder<Tuple<X0, X1>> @this, X0 x0, X1 x1)
        {
            return @this.Case(Tuple.Create(x0, x1));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1>>
            Case<X0, X1>(X0 x0, X1 x1)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1>>(new List<Tuple<X0, X1>>()).Case(x0, x1);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1>(this ParameterizedTestBuilder<Tuple<X0, X1>> @this, Func<X0, X1, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1>(this ParameterizedTestBuilder<Tuple<X0, X1>> @this, Func<X0, X1, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2>>
            Case<X0, X1, X2>(this ParameterizedTestBuilder<Tuple<X0, X1, X2>> @this, X0 x0, X1 x1, X2 x2)
        {
            return @this.Case(Tuple.Create(x0, x1, x2));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2>>
            Case<X0, X1, X2>(X0 x0, X1 x1, X2 x2)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1, X2>>(new List<Tuple<X0, X1, X2>>()).Case(x0, x1, x2);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2>(this ParameterizedTestBuilder<Tuple<X0, X1, X2>> @this, Func<X0, X1, X2, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2>(this ParameterizedTestBuilder<Tuple<X0, X1, X2>> @this, Func<X0, X1, X2, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>>
            Case<X0, X1, X2, X3>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>> @this, X0 x0, X1 x1, X2 x2, X3 x3)
        {
            return @this.Case(Tuple.Create(x0, x1, x2, x3));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>>
            Case<X0, X1, X2, X3>(X0 x0, X1 x1, X2 x2, X3 x3)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>>(new List<Tuple<X0, X1, X2, X3>>()).Case(x0, x1, x2, x3);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>> @this, Func<X0, X1, X2, X3, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3>> @this, Func<X0, X1, X2, X3, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>>
            Case<X0, X1, X2, X3, X4>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>> @this, X0 x0, X1 x1, X2 x2, X3 x3, X4 x4)
        {
            return @this.Case(Tuple.Create(x0, x1, x2, x3, x4));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>>
            Case<X0, X1, X2, X3, X4>(X0 x0, X1 x1, X2 x2, X3 x3, X4 x4)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>>(new List<Tuple<X0, X1, X2, X3, X4>>()).Case(x0, x1, x2, x3, x4);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>> @this, Func<X0, X1, X2, X3, X4, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4>> @this, Func<X0, X1, X2, X3, X4, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>>
            Case<X0, X1, X2, X3, X4, X5>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>> @this, X0 x0, X1 x1, X2 x2, X3 x3, X4 x4, X5 x5)
        {
            return @this.Case(Tuple.Create(x0, x1, x2, x3, x4, x5));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>>
            Case<X0, X1, X2, X3, X4, X5>(X0 x0, X1 x1, X2 x2, X3 x3, X4 x4, X5 x5)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>>(new List<Tuple<X0, X1, X2, X3, X4, X5>>()).Case(x0, x1, x2, x3, x4, x5);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4, X5>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>> @this, Func<X0, X1, X2, X3, X4, X5, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4, X5>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5>> @this, Func<X0, X1, X2, X3, X4, X5, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>>
            Case<X0, X1, X2, X3, X4, X5, X6>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>> @this, X0 x0, X1 x1, X2 x2, X3 x3, X4 x4, X5 x5, X6 x6)
        {
            return @this.Case(Tuple.Create(x0, x1, x2, x3, x4, x5, x6));
        }

        /// <summary>
        /// Adds a test case.
        /// <para lang="ja">
        /// テストケースを追加する。
        /// </para>
        /// </summary>
        public static ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>>
            Case<X0, X1, X2, X3, X4, X5, X6>(X0 x0, X1 x1, X2 x2, X3 x3, X4 x4, X5 x5, X6 x6)
        {
            return new ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>>(new List<Tuple<X0, X1, X2, X3, X4, X5, X6>>()).Case(x0, x1, x2, x3, x4, x5, x6);
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4, X5, X6>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>> @this, Func<X0, X1, X2, X3, X4, X5, X6, IEnumerable<Test>> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7));
        }

        /// <summary>
        /// Executes a parameterized test.
        /// <para lang="ja">
        /// パラメーター化されたテストを実行する。
        /// </para>
        /// </summary>
        public static IEnumerable<Test>
            Run<X0, X1, X2, X3, X4, X5, X6>(this ParameterizedTestBuilder<Tuple<X0, X1, X2, X3, X4, X5, X6>> @this, Func<X0, X1, X2, X3, X4, X5, X6, Test> run)
        {
            return @this.Run(t => run(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7));
        }
    }
}
