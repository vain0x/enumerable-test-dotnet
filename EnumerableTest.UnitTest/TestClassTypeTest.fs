namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module TestClassTypeTest =
  let ``test testMethodInfos`` =
    test {
      let typ = typeof<TestClass.WithManyProperties>
      let expected =
        [
          typ.GetMethod("PassingTestMethod")
          typ.GetMethod("ViolatingTestMethod")
          typ.GetMethod("ThrowingTestMethod")
        ]
      do! typ |> TestClassType.testMethodInfos |> assertSeqEquals expected
    }

  let ``test isTestClass`` =
    let body (typ, expected) =
      test {
        do! typ |> TestClassType.isTestClass |> assertEquals expected
      }
    parameterize {
      case (typeof<TestClass.WithManyProperties>, true)
      case (typeof<TestClass.Uninstantiatable>, true)
      case (typeof<TestClass.WithThrowingDispose>, true)
      case (typeof<TestClass.NotTestClass>, false)
      run body
    }

  let ``test instantiate`` =
    [
      test {
        let instantiate = typeof<TestClass.Passing> |> TestClassType.instantiate
        let instance = instantiate ()
        do! instance.GetType() |> assertEquals typeof<TestClass.Passing>
      }
      test {
        let instantiate = typeof<TestClass.Uninstantiatable> |> TestClassType.instantiate
        let! (_: exn) = trap { it (instantiate ()) }
        return ()
      }
    ]
