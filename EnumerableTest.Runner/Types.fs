namespace EnumerableTest.Runner

open System
open System.Reflection
open Basis.Core
open EnumerableTest.Sdk

/// Represents an instance of a test class.
type TestInstance =
  obj

type TestMethod =
  {
    MethodName                  : string
    Result                      : GroupTest
    DisposingError              : option<Exception>
  }
with
  member this.DisposingErrorOrNull =
    this.DisposingError |> Option.getOr null

type TestClass =
  {
    TypeFullName                : string
    InstantiationError          : option<Exception>
    Result                      : array<TestMethod>
  }

type TestSuite =
  array<TestClass>
