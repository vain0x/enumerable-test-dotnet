namespace EnumerableTest.Runner.Wpf

open System
open Argu
open Reactive.Bindings
open EnumerableTest.Runner

type AppArgument =
  | [<MainCommand>]
    Files of list<string>
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Files _ ->
        "Paths to test assembly"

type TestMethodResult =
  {
    TypeFullName                : string
    Method                      : TestMethod
  }

type TestSuite =
  IObservable<TestMethodResult>

type INodeViewModel =
  abstract member IsExpanded: IReadOnlyReactiveProperty<bool>
