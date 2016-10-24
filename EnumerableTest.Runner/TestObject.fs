namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

type GroupTest =
  Test.GroupTest

type TestClassInstance =
  obj

type TestMethod =
  {
    Method                      : MethodInfo
    Run                         : TestClassInstance -> GroupTest
  }

type TestClass =
  {
    Type                        : Type
    Create                      : unit -> TestClassInstance
    Cases                       : seq<TestMethod>
  }

type TestSuite =
  seq<TestClass>

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

type TestObjectResult =
  TestClass * TestMethodResult []

type TestSuiteResult =
  seq<TestObjectResult>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestObject =
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

  let instantiate (typ: Type): unit -> TestClassInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

  let tryCreate (typ: Type): option<TestClass> =
    if typ |> isTestClass then
      {
        Type                    = typ
        Create                  = typ |> instantiate
        Cases                   = typ |> testCases
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

    let tryRunCase (testCase: TestMethod) testObject =
      testObject |> tryInstantiate
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

    let tryRunCasesAsync (testObject: TestClass) =
      let results =
        testObject.Cases
        |> Seq.map
          (fun testCase ->
            async { return testObject |> tryRunCase testCase }
          )
        |> Async.Parallel
      (testObject, results)

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
    assembly.GetTypes() |> Seq.choose TestObject.tryCreate

  let runAsync (testSuite: TestSuite): Async<TestSuiteResult> =
    testSuite
    |> Seq.map TestObject.runAsync
    |> Async.Parallel
    |> Async.map (fun x -> x :> seq<_>)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestError =
  let methodName (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor               -> "constructor"
    | TestErrorMethod.Method testCase           -> testCase.Method.Name
    | TestErrorMethod.Dispose _                 -> "Dispose"

[<AutoOpen>]
module TestResultExtension =
  let (|Passed|Violated|Error|) =
    function
    | Success (assertionResult: AssertionResult) ->
      match assertionResult.Match(Choice1Of2, Choice2Of2) with
      | Choice1Of2 () -> Passed
      | Choice2Of2 message -> Violated message
    | Failure error ->
      Error error

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestObjectResult =
  let allTestResult (testObjectResult: TestObjectResult) =
    testObjectResult
    |> snd
    |> Seq.collect
      (function
        | Success test -> test.InnerResults |> Seq.map Success
        | Failure error -> seq { yield Failure error }
      )

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuiteResult =
  let allTestResult (testSuiteResult: TestSuiteResult) =
    testSuiteResult |> Seq.collect TestObjectResult.allTestResult

  let countResults testSuiteResult =
    let results = testSuiteResult |> allTestResult
    results |> Seq.fold
      (fun (count, violateCount, errorCount) (result: Result<AssertionResult, TestError>) ->
        let count = count + 1
        match result with
        | Passed _              -> (count, violateCount, errorCount)
        | Violated _            -> (count, violateCount + 1, errorCount)
        | Error _               -> (count, violateCount, errorCount + 1)
      ) (0, 0, 0)
