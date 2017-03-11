namespace EnumerableTest.Runner

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open Basis.Core
open EnumerableTest.Sdk

/// Represents an instance of a test class.
type TestInstance =
  obj

type TestMethodSchema =
  {
    MethodName: string
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
    TypeFullName:
      string
    Methods:
      array<TestMethodSchema>
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
    MethodName:
      string
    Result:
      SerializableGroupTest
    DisposingError:
      option<MarshalValue>
    Duration:
      TimeSpan
  }
with
  member this.DisposingErrorOrNull =
    match this.DisposingError with
    | Some e -> e :> obj
    | None -> null

type TestResult =
  {
    TypeFullName:
      string
    /// Represents completion of a test method or an instantiation error.
    Result:
      Result<TestMethod, exn>
  }

type TestClass =
  {
    TypeFullName:
      string
    InstantiationError:
      option<Exception>
    Result:
      array<TestMethod>
    NotCompletedMethods:
      array<TestMethodSchema>
  }

type TestSuite =
  IObservable<TestResult>

[<AbstractClass>]
type TestAssembly() =
  abstract TestResults: IObservable<TestResult>

  abstract Start: unit -> unit

  abstract Dispose: unit -> unit

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

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

type Warning =
  {
    Message:
      string
    Data:
      seq<KeyValuePair<string, obj>>
  }

type Notification =
  | Info
    of string
  | Warning
    of Warning

[<AbstractClass>]
type Notifier() =
  abstract Warnings: ObservableCollection<Warning>

  abstract NotifyInfo: string -> unit

  abstract NotifyWarning: string * seq<string * obj> -> unit

  abstract Subscribe: IObserver<Notification> -> IDisposable

  abstract Dispose: unit -> unit

  interface IObservable<Notification> with
    override this.Subscribe(observer) =
      this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
