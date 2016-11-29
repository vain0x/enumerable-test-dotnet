namespace EnumerableTest.Runner.Wpf

open System
open Reactive.Bindings
open Basis.Core
open EnumerableTest.Runner

type TestResult =
  {
    TypeFullName:
      string
    /// Represents completion of a test method or an instantiation error.
    Result:
      Result<TestMethod, exn>
  }

type TestSuite =
  IObservable<TestResult>

type NotExecutedResult private () =
  static member val Instance =
    new NotExecutedResult()
