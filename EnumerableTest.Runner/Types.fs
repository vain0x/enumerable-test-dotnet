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

type TestClassPath =
  {
    NamespacePath:
      array<string>
    ClassPath:
      array<string>
    Name:
      string
  }

type TestClassSchema =
  {
    Path:
      TestClassPath
    TypeFullName                : string
    Methods                     : array<TestMethodSchema>
  }

type TestSuiteSchema =
  array<TestClassSchema>

type TestMethod =
  {
    MethodName                  : string
    Result                      : SerializableGroupTest
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
    NotCompletedMethods         : array<TestMethodSchema>
  }

type AssertionCount =
  {
    TotalCount:
      int
    ViolatedCount:
      int
    ErrorCount:
      int
    NotCompletedCount:
      int
  }

type TestStatistic =
  {
    AssertionCount:
      AssertionCount
    Duration:
      TimeSpan
  }

type TestStatus =
  | NotCompleted
  | Passed
  | Violated
  | Error
