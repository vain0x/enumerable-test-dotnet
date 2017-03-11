namespace EnumerableTest.Runner.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module ``test TestMethodResult`` =
  let ``test create TestClass.WithManyProperties1`` =
    let typ = typeof<TestClass.WithManyProperties>
    let instantiate = typ |> TestClassType.instantiate
    [
      test {
        let result = TestMethodResult.create (instantiate ()) (typ.GetMethod("PassingTestMethod"))
        do! result.Result |> assertSatisfies (fun r -> r.IsPassed)
        do! result.Result.ExceptionOrNull |> assertEquals null
        do! result.DisposingError |> assertEquals None
      }
      test {
        let result = TestMethodResult.create (instantiate ()) (typ.GetMethod("ViolatingTestMethod"))
        do! result.Result |> assertSatisfies (fun r -> r.IsPassed |> not)
        do! result.Result.ExceptionOrNull |> assertEquals null
        do! result.DisposingError |> assertEquals None
      }
      test {
        let result = TestMethodResult.create (instantiate ()) (typ.GetMethod("ThrowingTestMethod"))
        do! result.Result.ExceptionOrNull |> assertSatisfies (isNull >> not)
        do! result.DisposingError |> assertEquals None
      }
    ]

  let ``test create TestClass.WithThrowingDispose`` =
    let typ = typeof<TestClass.WithThrowingDispose>
    let instantiate = typ |> TestClassType.instantiate
    [
      test {
        let result = TestMethodResult.create (instantiate ()) (typ.GetMethod("PassingTest"))
        do! result.DisposingError |> assertSatisfies Option.isSome
      }
    ]

  let ``test createManyAsync`` =
    [
      test {
        let (testMethods, instantiationError) =
          typeof<TestClass.WithManyProperties> |> TestMethodResult.createManyAsync
        do! instantiationError |> assertEquals None
        do! testMethods |> assertSatisfies (Array.length >> (=) 3)
      }
      test {
        let (testMethods, instantiationError) =
          typeof<TestClass.Uninstantiatable> |> TestMethodResult.createManyAsync
        do! instantiationError |> assertSatisfies Option.isSome
        do! testMethods |> assertSatisfies Array.isEmpty
      }
    ]
