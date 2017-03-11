namespace EnumerableTest.Runner

open System
open System.Diagnostics
open System.Reflection
open EnumerableTest
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethodResult =
  let ofResult name result disposingError duration =
    {
      MethodName =
        name
      Result =
        result
      DisposingError =
        disposingError
      Duration =
        duration
    }

  let ofInstantiationError (e: exn) =
    let name = "default constructor"
    let e = e |> MarshalValue.ofObjDeep
    let result = SerializableGroupTest(name, [||], Some e, SerializableEmptyTestData.Empty)
    ofResult name result None TimeSpan.Zero

  /// Creates an instance of TestMethodResult
  /// by executing a test method of an instance and disposing the instance.
  let create (instance: TestInstance) (m: MethodInfo) =
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
    ofResult m.Name groupTest disposingError duration

  /// Builds computations to create TestMethodResult instance
  /// for each test method from a test class type.
  /// NOTE: Execute all computations to dispose created instances.
  let createManyAsync (typ: Type) =
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
                return m |> create instance
              }
            (m, computation)
          )
        |> Seq.toArray
      (computations, None)
    with
    | e ->
      ([||], Some e)

  let isPassed (testMethodResult: TestMethodResult) =
    testMethodResult.Result.IsPassed
    && testMethodResult.DisposingError |> Option.isNone
