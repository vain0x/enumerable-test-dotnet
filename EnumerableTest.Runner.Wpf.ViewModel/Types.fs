namespace EnumerableTest.Runner.Wpf

open System
open EnumerableTest.Runner

type TestMethodResult =
  {
    TypeFullName                : string
    Method                      : TestMethod
  }

type TestSuite =
  IObservable<TestMethodResult>
