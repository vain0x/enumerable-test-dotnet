namespace EnumerableTest.Runner

open System
open System.Collections.Concurrent
open System.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let runAsync (typ: Type) =
    let methodInfos = typ |> TestClassType.testMethodInfos
    let instantiate = typ |> TestClassType.instantiate
    try
      let computations =
        methodInfos
        |> Seq.map
          (fun m ->
            let instance = instantiate ()
            let computation =
              async {
                return m |> TestMethod.create instance
              }
            (m, computation)
          )
        |> Seq.toArray
      (computations, None)
    with
    | e ->
      ([||], Some e)

  let create timeout (typ: Type): TestClass =
    let (methods, instantiationError) =
      runAsync typ
    let observable =
      methods |> Seq.map snd |> Observable.startParallel
    let results = ConcurrentQueue<_>()
    observable |> Observable.subscribe results.Enqueue |> ignore<IDisposable>
    observable.Connect()
    observable |> Observable.waitTimeout timeout |> ignore<bool>
    let results = results |> Seq.toArray
    let skippedMethods =
      methods |> Seq.map (fun (m, _) -> m.Name)
      |> Seq.except (results |> Seq.map (fun m -> m.MethodName))
      |> Seq.map (fun name -> { TestMethodSchema.MethodName = name })
      |> Seq.toArray
    let testClass =
      {
        TypeFullName                    = (typ: Type).FullName
        InstantiationError              = instantiationError
        Result                          = results
        SkippedMethods                  = skippedMethods
      }
    testClass

  let assertions (testClass: TestClass) =
    testClass.Result
    |> Seq.collect (fun testMethod -> testMethod.Result.Assertions)

  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.SkippedMethods |> Array.isEmpty
    && testClass.Result |> Seq.forall TestMethod.isPassed
