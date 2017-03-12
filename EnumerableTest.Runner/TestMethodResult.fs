namespace EnumerableTest.Runner

open System

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

  let isPassed (testMethodResult: TestMethodResult) =
    testMethodResult.Result.IsPassed
    && testMethodResult.DisposingError |> Option.isNone
