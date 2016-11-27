namespace EnumerableTest.UnitTest

open System
open System.Reflection
open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner

module TestClassTypeAndTestMethodTest =
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
