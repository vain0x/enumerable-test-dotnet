namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let internal testMethodInfos (typ: Type) =
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
    && typ |> testMethodInfos |> Seq.isEmpty |> not

  let internal testMethod m =
    {
      Method                = m
      Run                   =
        fun this ->
          let tests = m.Invoke(this, [||]) :?> seq<Test>
          Test.OfTestGroup(m.Name, tests)
    }

  let internal testMethods (typ: Type) =
    typ
    |> testMethodInfos
    |> Seq.map testMethod

  let internal instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

  let tryCreate (typ: Type): option<TestClass> =
    if typ |> isTestClass then
      {
        Type                    = typ
        Create                  = typ |> instantiate
        Methods                 = typ |> testMethods
      } |> Some
    else
      None

  let internal tryInstantiate (testClass: TestClass) =
    Result.catch testClass.Create
    |> Result.mapFailure TestError.OfConstructor

  let internal tryDispose testMethod instance =
    Result.catch (fun () -> instance |> Disposable.dispose)
    |> Result.mapFailure (fun e -> TestError.OfDispose (testMethod, e))

  let internal tryRunTestMethod (testMethod: TestMethod) testClass =
    testClass |> tryInstantiate
    |> Result.bind
      (fun instance ->
        let methodResult =
          Result.catch (fun () -> instance |> testMethod.Run)
          |> Result.mapFailure (fun e -> TestError.OfMethod (testMethod, e))
        let disposeResult =
          instance |> tryDispose testMethod
        match (methodResult, disposeResult) with
        | Success _, Failure error ->
          Failure error
        | _ ->
          methodResult
      )

  let internal tryRunTestMethodsAsync (testClass: TestClass) =
    testClass.Methods
    |> Seq.map
      (fun testCase ->
        async { return testClass |> tryRunTestMethod testCase }
      )
    |> Async.Parallel

  /// We report up to one instantiation error
  /// because we don't want to see the same error for each test method.
  let internal unitfyInstantiationErrors results =
    let (failureList, successList) =
      results |> Array.partition
        (function
          | Failure ({ TestError.Method = TestErrorMethod.Constructor }) -> true
          | x -> false
        )
    Array.append
      (failureList |> Seq.tryHead |> Option.toArray)
      successList

  let runAsync testClass: Async<TestClassResult> =
    async {
      let! results = testClass |> tryRunTestMethodsAsync
      let results = results |> unitfyInstantiationErrors
      return (testClass, results)
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
