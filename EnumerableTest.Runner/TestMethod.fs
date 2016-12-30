namespace EnumerableTest.Runner

open System
open System.Diagnostics
open System.Reflection
open EnumerableTest
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethod =
  let ofResult name result disposingError duration =
    {
      MethodName                    = name
      Result                        = result
      DisposingError                = disposingError
      Duration                      = duration
    }

  let ofInstantiationError (e: exn) =
    let name = "default constructor"
    let result = SerializableGroupTest(name, [||], Some e, SerializableEmptyTestData.Empty)
    ofResult name result None TimeSpan.Zero

  /// Creates an instance of TestMethod
  /// by executing a test method of an instance and disposing the instance.
  let create (instance: TestInstance) (m: MethodInfo) =
    let stopwatch = Stopwatch.StartNew()
    let tests =
      m.Invoke(instance, [||]) :?> seq<Test>
    let groupTest =
      tests.ToTestGroup(m.Name)
    let disposingError =
      Option.tryCatch (fun () -> instance |> Disposable.dispose)
    let duration = stopwatch.Elapsed
    // Convert the result to be serializable.
    let groupTest =
      groupTest |> SerializableTest.ofGroupTest
    ofResult m.Name groupTest disposingError duration

  /// Builds computations to create TestMethod instance
  /// for each test method from a test class type.
  /// NOTE: Execute all computations to dispose created instances.
  let createManyAsync (typ: Type) =
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
                return m |> create instance
              }
            (m, computation)
          )
        |> Seq.toArray
      (computations, None)
    with
    | e ->
      ([||], Some e)

  let isPassed (testMethod: TestMethod) =
    testMethod.Result.IsPassed
    && testMethod.DisposingError |> Option.isNone
