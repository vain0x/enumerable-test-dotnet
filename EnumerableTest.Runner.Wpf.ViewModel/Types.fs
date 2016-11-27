namespace EnumerableTest.Runner.Wpf

open System
open Reactive.Bindings
open EnumerableTest.Runner

type TestMethodResult =
  {
    TypeFullName                : string
    Method                      : TestMethod
  }

type TestSuite =
  IObservable<TestMethodResult>

type NotExecutedResult private () =
  static member val Instance =
    new NotExecutedResult()

type INodeViewModel =
  abstract member IsExpanded: IReadOnlyReactiveProperty<bool>
  