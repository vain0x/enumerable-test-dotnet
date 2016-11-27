namespace EnumerableTest.Runner

open System
open System.Diagnostics
open System.Reflection
open System.Threading
open EnumerableTest
open EnumerableTest.Sdk
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassType =
  let testMethodInfos (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && (m.GetParameters() |> Array.isEmpty)
        && m.ReturnType = typeof<seq<Test>>
      )

  let isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethodInfos |> Seq.isEmpty |> not

  let instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethodSchema =
  let ofMethodInfo (m: MethodInfo): TestMethodSchema =
    {
      MethodName                    = m.Name
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassSchema =
  let ofType (typ: Type): TestClassSchema =
    {
      TypeFullName                = typ.FullName
      Methods                     = 
        typ
        |> TestClassType.testMethodInfos
        |> Seq.map TestMethodSchema.ofMethodInfo
        |> Seq.toArray
    }

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
    let result = new GroupTest(name, [||], e)
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
    ofResult m.Name groupTest disposingError stopwatch.Elapsed

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
