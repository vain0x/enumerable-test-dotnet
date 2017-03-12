namespace EnumerableTest.Runner.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module ``test TestRunner`` =
  module ``test runTestMethod`` =
    let run = TestRunner.runTestMethod

    let ``test run TestClasses.WithManyProperties1`` =
      let typ = typeof<TestClasses.WithManyProperties>
      let instantiate = typ |> TestType.instantiate
      [
        test {
          let result = run (instantiate ()) (typ.GetMethod("PassingTestMethod"))
          do! result.Result |> assertSatisfies (fun r -> r.IsPassed)
          do! result.Result.ExceptionOrNull |> assertEquals null
          do! result.DisposingError |> assertEquals None
        }
        test {
          let result = run (instantiate ()) (typ.GetMethod("ViolatingTestMethod"))
          do! result.Result |> assertSatisfies (fun r -> r.IsPassed |> not)
          do! result.Result.ExceptionOrNull |> assertEquals null
          do! result.DisposingError |> assertEquals None
        }
        test {
          let result = run (instantiate ()) (typ.GetMethod("ThrowingTestMethod"))
          do! result.Result.ExceptionOrNull |> assertSatisfies (isNull >> not)
          do! result.DisposingError |> assertEquals None
        }
      ]

    let ``test run TestClasses.WithThrowingDispose`` =
      let typ = typeof<TestClasses.WithThrowingDispose>
      let instantiate = typ |> TestType.instantiate
      [
        test {
          let result = run (instantiate ()) (typ.GetMethod("PassingTest"))
          do! result.DisposingError |> assertSatisfies Option.isSome
        }
      ]

  let ``test runTestTypeAsyncCore`` =
    let run = TestRunner.runTestTypeAsyncCore
    [
      test {
        let (testMethods, instantiationError) =
          typeof<TestClasses.WithManyProperties> |> run
        do! instantiationError |> assertEquals None
        do! testMethods |> assertSatisfies (Array.length >> (=) 3)
      }
      test {
        let (testMethods, instantiationError) =
          typeof<TestClasses.Uninstantiatable> |> run
        do! instantiationError |> assertSatisfies Option.isSome
        do! testMethods |> assertSatisfies Array.isEmpty
      }
    ]
