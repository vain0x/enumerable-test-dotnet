namespace EnumerableTest.Runner

open System
open System.Collections.Concurrent
open System.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let create timeout (typ: Type): TestClass =
    let (methods, instantiationError) =
      TestMethod.createManyAsync typ
    let observable =
      methods |> Seq.map snd |> Observable.startParallel
    let results = ConcurrentQueue<_>()
    observable |> Observable.subscribe results.Enqueue |> ignore<IDisposable>
    observable.Connect()
    observable |> Observable.waitTimeout timeout |> ignore<bool>
    let results = results |> Seq.toArray
    let notCompletedMethods =
      methods |> Seq.map (fun (m, _) -> m.Name)
      |> Seq.except (results |> Seq.map (fun m -> m.MethodName))
      |> Seq.map (fun name -> { TestMethodSchema.MethodName = name })
      |> Seq.toArray
    let testClass =
      {
        TypeFullName                    = (typ: Type).FullName
        InstantiationError              = instantiationError
        Result                          = results
        NotCompletedMethods             = notCompletedMethods
      }
    testClass

  let assertions (testClass: TestClass) =
    testClass.Result
    |> Seq.collect (fun testMethod -> testMethod.Result.Assertions)

  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.NotCompletedMethods |> Array.isEmpty
    && testClass.Result |> Seq.forall TestMethod.isPassed
