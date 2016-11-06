namespace EnumerableTest.Runner.Wpf

open System
open EnumerableTest.Runner

type TestClass =
  {
    TypeFullName                : string
    InstantiationError          : option<Exception>
    Result                      : array<TestMethod>
  }

type TestSuite =
  array<TestClass>
