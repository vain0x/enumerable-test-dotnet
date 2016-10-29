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
    Method                      : MethodInfo
    Run                         : TestInstance -> GroupTest
  }

type TestClass =
  {
    Type                        : Type
    Create                      : unit -> TestInstance
    Methods                     : seq<TestMethod>
  }

type TestSuite =
  seq<TestClass>

/// Denotes where an exception was thrown.
[<RequireQualifiedAccess>]
type TestErrorMethod =
  | Constructor
  | Method                      of TestMethod
  | Dispose                     of TestMethod

type TestError =
  {
    Method                      : TestErrorMethod
    Error                       : Exception
  }
with
  static member Create(errorMethod, error) =
    {
      Method                    = errorMethod
      Error                     = error
    }

  static member OfConstructor(error) =
    TestError.Create(TestErrorMethod.Constructor, error)

  static member OfDispose(testCase, error) =
    TestError.Create(TestErrorMethod.Dispose testCase, error)

  static member OfMethod(testCase, error) =
    TestError.Create(TestErrorMethod.Method testCase, error)

type TestMethodResult =
  Result<GroupTest, TestError>

type TestClassResult =
  TestClass * TestMethodResult []

type TestSuiteResult =
  seq<TestClassResult>
