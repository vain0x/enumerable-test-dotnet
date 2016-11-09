namespace EnumerableTest.Runner.Wpf

open System
open DotNetKit.Observing
open EnumerableTest.Runner

type TestMethodResult =
  {
    TypeFullName                : string
    Method                      : TestMethod
  }

type TestSuite =
  IObservable<TestMethodResult>

type INodeViewModel =
  abstract member IsExpanded: IReadOnlyUptodate<bool>
