namespace EnumerableTest.Runner.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module ``test TestType`` =
  let ``test testMethodInfos`` =
    test {
      let typ = typeof<TestClasses.WithManyProperties>
      let expected =
        [
          typ.GetMethod("PassingTestMethod")
          typ.GetMethod("ViolatingTestMethod")
          typ.GetMethod("ThrowingTestMethod")
        ]
      do! typ |> TestType.testMethodInfos |> assertSeqEquals expected
    }

  let ``test isTestClass`` =
    let body (typ, expected) =
      test {
        do! typ |> TestType.isTestClass |> assertEquals expected
      }
    parameterize {
      case (typeof<TestClasses.WithManyProperties>, true)
      case (typeof<TestClasses.Uninstantiatable>, true)
      case (typeof<TestClasses.WithThrowingDispose>, true)
      case (typeof<TestClasses.NotTestClass>, false)
      run body
    }

  let ``test instantiate`` =
    [
      test {
        let instantiate = typeof<TestClasses.Passing> |> TestType.instantiate
        let instance = instantiate ()
        do! instance.GetType() |> assertEquals typeof<TestClasses.Passing>
      }
      test {
        let instantiate = typeof<TestClasses.Uninstantiatable> |> TestType.instantiate
        let! (_: exn) = trap { it (instantiate ()) }
        return ()
      }
    ]
