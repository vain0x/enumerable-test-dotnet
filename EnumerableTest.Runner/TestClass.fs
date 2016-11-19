namespace EnumerableTest.Runner

open System
open System.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let private runAsync (typ: Type) =
    let methodInfos = typ |> TestClassType.testMethodInfos
    let instantiate = typ |> TestClassType.instantiate
    try
      let result =
        methodInfos
        |> Seq.map
          (fun m ->
            let instance = instantiate ()
            async {
              return m |> TestMethod.create instance
            }
          )
        |> Seq.toArray
      (result, None)
    with
    | e ->
      ([||], Some e)

  let create (typ: Type): TestClass =
    let (result, instantiationError) =
      runAsync typ
    let testClass =
      {
        TypeFullName                    = (typ: Type).FullName
        InstantiationError              = instantiationError
        Result                          = result |> Array.map Async.RunSynchronously
      }
    testClass

  let assertions (testClass: TestClass) =
    testClass.Result
    |> Seq.collect (fun testMethod -> testMethod.Result.Assertions)

  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.Result |> Seq.forall TestMethod.isPassed
