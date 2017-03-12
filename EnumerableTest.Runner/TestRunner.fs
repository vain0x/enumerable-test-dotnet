namespace EnumerableTest.Runner

open System
open System.Diagnostics
open System.Reflection
open System.Threading.Tasks
open Basis.Core
open EnumerableTest
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestRunner =
  /// Creates an instance of TestMethodResult
  /// by executing a test method of an instance and disposing the instance.
  let runTestMethod (instance: TestInstance) (m: MethodInfo) =
    let stopwatch = Stopwatch.StartNew()
    let tests =
      m.Invoke(instance, [||]) :?> seq<Test>
    let groupTest =
      tests.ToTestGroup(m.Name)
    let disposingError =
      Option.tryCatch (fun () -> instance |> Disposable.dispose)
      |> Option.map MarshalValue.ofObjDeep
    let duration = stopwatch.Elapsed
    // Convert the result to be serializable.
    let groupTest =
      groupTest |> SerializableTest.ofGroupTest
    TestMethodResult.ofResult m.Name groupTest disposingError duration

  /// Builds computations to run tests
  /// for each test method from a test type.
  /// NOTE: Execute all computations to dispose created instances.
  let runTestTypeAsyncCore (typ: Type) =
    let methodInfos = typ |> TestType.testMethodInfos
    let instantiate = typ |> TestType.instantiate
    try
      let computations =
        methodInfos
        |> Seq.map
          (fun m ->
            let instance = instantiate ()
            let computation =
              async {
                return m |> runTestMethod instance
              }
            (m, computation)
          )
        |> Seq.toArray
      (computations, None)
    with
    | e ->
      ([||], Some e)

  let runTestTypeAsync (typ: Type) =
    match runTestTypeAsyncCore typ with
    | (_, Some e) ->
      let result = TestResult.create typ (Failure e)
      [| async { return result } |]
    | (methods, None) ->
      methods |> Array.map (snd >> Async.map (Success >> TestResult.create typ))

  let runTestTypes types =
    types
    |> Seq.filter TestType.isTestClass
    |> Seq.collect runTestTypeAsync
    |> Observable.startParallel

  let runTestAssembly (assembly: Assembly) =
    assembly.GetTypes()
    |> runTestTypes
