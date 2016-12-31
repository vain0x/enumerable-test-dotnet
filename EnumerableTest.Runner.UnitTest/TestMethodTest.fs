namespace EnumerableTest.Runner.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module TestMethodTest =
  let ``test create TestClass.WithManyProperties1`` =
    let typ = typeof<TestClass.WithManyProperties>
    let instantiate = typ |> TestClassType.instantiate
    [
      test {
        let testMethod = TestMethod.create (instantiate ()) (typ.GetMethod("PassingTestMethod"))
        do! testMethod.Result |> assertSatisfies (fun r -> r.IsPassed)
        do! testMethod.Result.ExceptionOrNull |> assertEquals null
        do! testMethod.DisposingError |> assertEquals None
      }
      test {
        let testMethod = TestMethod.create (instantiate ()) (typ.GetMethod("ViolatingTestMethod"))
        do! testMethod.Result |> assertSatisfies (fun r -> r.IsPassed |> not)
        do! testMethod.Result.ExceptionOrNull |> assertEquals null
        do! testMethod.DisposingError |> assertEquals None
      }
      test {
        let testMethod = TestMethod.create (instantiate ()) (typ.GetMethod("ThrowingTestMethod"))
        do! testMethod.Result.ExceptionOrNull |> assertSatisfies (isNull >> not)
        do! testMethod.DisposingError |> assertEquals None
      }
    ]

  let ``test create TestClass.WithThrowingDispose`` =
    let typ = typeof<TestClass.WithThrowingDispose>
    let instantiate = typ |> TestClassType.instantiate
    [
      test {
        let testMethod = TestMethod.create (instantiate ()) (typ.GetMethod("PassingTest"))
        do! testMethod.DisposingError |> assertSatisfies Option.isSome
      }
    ]

  let ``test createManyAsync`` =
    [
      test {
        let (testMethods, instantiationError) =
          typeof<TestClass.WithManyProperties> |> TestMethod.createManyAsync
        do! instantiationError |> assertEquals None
        do! testMethods |> assertSatisfies (Array.length >> (=) 3)
      }
      test {
        let (testMethods, instantiationError) =
          typeof<TestClass.Uninstantiatable> |> TestMethod.createManyAsync
        do! instantiationError |> assertSatisfies Option.isSome
        do! testMethods |> assertSatisfies Array.isEmpty
      }
    ]
