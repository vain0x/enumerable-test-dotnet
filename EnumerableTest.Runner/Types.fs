namespace EnumerableTest.Runner

open System
open System.Reflection
open Basis.Core
open EnumerableTest.Sdk

/// Represents an instance of a test class.
type TestInstance =
  obj

type TestMethodSchema =
  {
    MethodName                  : string
  }

type TestClassSchema =
  {
    TypeFullName                : string
    Methods                     : array<TestMethodSchema>
  }

type TestSuiteSchema =
  array<TestClassSchema>

type TestMethod =
  {
    MethodName                  : string
    Result                      : GroupTest
    DisposingError              : option<Exception>
    Duration                    : TimeSpan
  }
with
  member this.DisposingErrorOrNull =
    this.DisposingError |> Option.getOr null

type TestClass =
  {
    TypeFullName                : string
    InstantiationError          : option<Exception>
    Result                      : array<TestMethod>
    SkippedMethods              : array<TestMethodSchema>
  }
