namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

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

  let runAsync: TestClass -> Async<TestClassResult> =
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

  let runAsync (testSuite: TestSuite) =
    let subject = Observable.Subject()
    let start () =
      async {
        let! units =
          testSuite
          |> Seq.map
            (fun testClass ->
              async {
                let! result = testClass |> TestClass.runAsync
                (subject :> IObserver<_>).OnNext(result)
              }
            )
          |> Async.Parallel
        (subject :> IObserver<_>).OnCompleted()
      } |> Async.Start
    { new Observable.IConnectableObservable<_> with
        override this.Connect() =
          start ()
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }
