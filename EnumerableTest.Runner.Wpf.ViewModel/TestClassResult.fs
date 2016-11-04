namespace EnumerableTest.Runner.Wpf

open EnumerableTest.Runner

module TestClassResult =
  let toSerializable (testClassResult: TestClassResult) =
    let (testClass, methodResults) =
      testClassResult
    let methodResults =
      methodResults |> Seq.map
        (fun (testMethod, result) ->
          (testMethod.MethodName, result |> Result.toObj)
        )
      |> Seq.toArray
    (testClass.TypeFullName, methodResults)
