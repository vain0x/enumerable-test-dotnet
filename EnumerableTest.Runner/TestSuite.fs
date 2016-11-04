namespace EnumerableTest.Runner

open System
open System.Reflection
open System.Threading
open EnumerableTest
open EnumerableTest.Sdk
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethod =
  let internal ofResult name result disposingError =
    {
      MethodName                    = name
      Result                        = result
      DisposingError                = disposingError
    }

  let internal create (instance: TestInstance) (m: MethodInfo) =
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
    ofResult m.Name groupTest disposingError

  let isPassed (testMethod: TestMethod) =
    testMethod.Result.IsPassed
    && testMethod.DisposingError |> Option.isNone

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

  let internal instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

  let create (typ: Type): TestClass =
    let methodInfos = typ |> testMethodInfos
    let instantiate = typ |> instantiate
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
      [| TestMethod.ofResult name result None |]
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
  let ofAssemblyLazy (assembly: Assembly): seq<unit -> TestClass> =
    assembly.GetTypes()
    |> Seq.filter (fun typ -> typ |> TestClass.isTestClass)
    |> Seq.map (fun typ () -> typ |> TestClass.create)

  let ofAssembly (assembly: Assembly): TestSuite =
    assembly
    |> ofAssemblyLazy
    |> Seq.map (fun f -> f())
    |> Seq.toArray

  let empty: TestSuite =
    Array.empty
