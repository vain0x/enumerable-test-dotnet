namespace EnumerableTest.Runner

open System
open System.Collections.Generic
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

type TestClassSchemaDifference =
  {
    Added:
      IReadOnlyList<TestMethodSchema>
    Removed:
      IReadOnlyList<TestMethodSchema>
    Modified:
      Map<string, TestMethodSchema>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

type TestSuiteSchemaDifference =
  {
    Added:
      IReadOnlyList<TestClassSchema>
    Removed:
      IReadOnlyList<TestClassSchema>
    Modified:
      Map<list<string>, TestClassSchemaDifference>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

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
