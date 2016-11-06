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

  let create (instance: TestInstance) (m: MethodInfo) =
    let stopwatch = Stopwatch.StartNew()
    let tests =
      m.Invoke(instance, [||]) :?> seq<Test>
    let groupTest =
      tests.ToTestGroup(m.Name)
    let disposingError =
      try
        instance |> Disposable.dispose
        None
      with
      | e -> Some e
    ofResult m.Name groupTest disposingError stopwatch.Elapsed

  let isPassed (testMethod: TestMethod) =
    testMethod.Result.IsPassed
    && testMethod.DisposingError |> Option.isNone

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let create (typ: Type): TestClass =
    let methodInfos = typ |> TestClassType.testMethodInfos
    let instantiate = typ |> TestClassType.instantiate
    let (result, instantiationError) =
      try
        let result =
          methodInfos
          |> Seq.map (fun m -> m |> TestMethod.create (instantiate ()))
          |> Seq.toArray
        (result, None)
      with
      | e ->
        ([||], Some e)
    let testClass =
      {
        TypeFullName                    = (typ: Type).FullName
        InstantiationError              = instantiationError
        Result                          = result
      }
    testClass

  let testMethods (testClass: TestClass): array<TestMethod> =
    match testClass.InstantiationError with
    | Some e ->
      let name = "default constructor"
      let result = new GroupTest(name, [||], e)
      [| TestMethod.ofResult name result None TimeSpan.Zero |]
    | None ->
      testClass.Result

  let assertions (testClass: TestClass) =
    testClass.Result
    |> Seq.collect (fun testMethod -> testMethod.Result.Assertions)

  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.Result |> Seq.forall TestMethod.isPassed

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let ofAssemblyLazy (assembly: Assembly) =
    assembly.GetTypes()
    |> Seq.filter (fun typ -> typ |> TestClassType.isTestClass)
    |> Seq.map (fun typ -> (typ, fun () -> typ |> TestClass.create))

  let ofAssemblyAsObservable (assembly: Assembly) =
    let (types, fs) = 
      assembly
      |> ofAssemblyLazy
      |> Seq.toArray
      |> Array.unzip
    let (schema: TestSuiteSchema) =
      types
      |> Array.map TestClassSchema.ofType
    let connectable =
      fs
      |> Seq.map Async.run
      |> Observable.ofParallel
    (schema, connectable)

  let empty: TestSuite =
    Array.empty
