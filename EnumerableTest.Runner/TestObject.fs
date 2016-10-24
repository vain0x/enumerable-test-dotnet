namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

type GroupTest =
  Test.GroupTest

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

/// Where the exception was thrown.
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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let internal testMethods (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && (m.GetParameters() |> Array.isEmpty)
        && m.ReturnType = typeof<seq<Test>>
      )

  let internal isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethods |> Seq.isEmpty |> not

  let internal testCases (typ: Type) =
    typ
    |> testMethods
    |> Seq.map
      (fun m ->
        {
          Method                = m
          Run                   =
            fun this ->
              let tests = m.Invoke(this, [||]) :?> seq<Test>
              Test.OfTestGroup(m.Name, tests)
        }
      )

  let instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

  let tryCreate (typ: Type): option<TestClass> =
    if typ |> isTestClass then
      {
        Type                    = typ
        Create                  = typ |> instantiate
        Methods                 = typ |> testCases
      } |> Some
    else
      None

  let runAsync =
    let tryInstantiate (testObject: TestClass) =
      Result.catch testObject.Create
      |> Result.mapFailure TestError.OfConstructor

    let tryDispose testCase instance =
      Result.catch (fun () -> instance |> Disposable.dispose)
      |> Result.mapFailure (fun e -> TestError.OfDispose (testCase, e))

    let tryRunTestMethod (testCase: TestMethod) testClass =
      testClass |> tryInstantiate
      |> Result.bind
        (fun instance ->
          let methodResult =
            Result.catch (fun () -> instance |> testCase.Run)
            |> Result.mapFailure (fun e -> TestError.OfMethod (testCase, e))
          let disposeResult =
            instance |> tryDispose testCase
          match (methodResult, disposeResult) with
          | Success _, Failure error ->
            Failure error
          | _ ->
            methodResult
        )

    let tryRunCasesAsync (testClass: TestClass) =
      let results =
        testClass.Methods
        |> Seq.map
          (fun testCase ->
            async { return testClass |> tryRunTestMethod testCase }
          )
        |> Async.Parallel
      (testClass, results)

    /// We report up to one instantiation error
    /// because we don't want to see the same error for each test method.
    let unitfyInstantiationErrors results =
      let (failureList, successList) =
        results |> Array.partition
          (function
            | Failure ({ TestError.Method = TestErrorMethod.Constructor }) -> true
            | x -> false
          )
      Array.append
        (failureList |> Seq.tryHead |> Option.toArray)
        successList

    fun testObject ->
      async {
        let (testObject, a) = testObject |> tryRunCasesAsync
        let! results = a
        let results = results |> unitfyInstantiationErrors
        return (testObject, results)
      }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let ofAssembly (assembly: Assembly): TestSuite =
    assembly.GetTypes() |> Seq.choose TestClass.tryCreate

  let runAsync (testSuite: TestSuite): Async<TestSuiteResult> =
    testSuite
    |> Seq.map TestClass.runAsync
    |> Async.Parallel
    |> Async.map (fun x -> x :> seq<_>)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestError =
  let methodName (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor               -> "constructor"
    | TestErrorMethod.Method testCase           -> testCase.Method.Name
    | TestErrorMethod.Dispose _                 -> "Dispose"

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassResult =
  let allAssertionResults (testClassResult: TestClassResult) =
    testClassResult
    |> snd
    |> Seq.collect
      (function
        | Success test -> test.InnerResults |> Seq.map Success
        | Failure error -> seq { yield Failure error }
      )

  let isAllPassed testClassResult =
    testClassResult
    |> allAssertionResults
    |> Seq.forall
      (function
        | Success test when test.IsPassed -> true
        | _ -> false
      )

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuiteResult =
  let allAssertionResults (testSuiteResult: TestSuiteResult) =
    testSuiteResult |> Seq.collect TestClassResult.allAssertionResults

  let countResults testSuiteResult =
    let results = testSuiteResult |> allAssertionResults
    results |> Seq.fold
      (fun (count, violateCount, errorCount) (result: Result<AssertionResult, TestError>) ->
        let count = count + 1
        match result with
        | Success assertionResult ->
          match assertionResult with
          | Passed              -> (count, violateCount, errorCount)
          | Violated _          -> (count, violateCount + 1, errorCount)
        | Failure _             -> (count, violateCount, errorCount + 1)
      ) (0, 0, 0)

  let isAllPassed testSuiteResult =
    testSuiteResult |> Seq.forall TestClassResult.isAllPassed
