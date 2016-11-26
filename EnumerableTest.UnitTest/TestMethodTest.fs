namespace EnumerableTest.UnitTest

open System
open System.Reflection
open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner

module TestClassTypeAndTestMethodTest =
  let passingTest = 
    seq {
      yield Test.Equal(0, 0)
    }

  let violatingTest =
    seq {
      yield Test.Equal(1, 2)
    }

  type TestClass1() =
    member this.PassingTestMethod() =
      passingTest

    member this.ViolatingTestMethod() =
      violatingTest

    member this.ThrowingTestMethod() =
      seq {
        yield Test.Equal(0, 0)
        Exception() |> raise
      }

    member this.NotTestMethodBecauseOfBeingProperty
      with get () =
        passingTest

    member this.NotTestMethodBecauseOfReturnType() =
      Test.Equal(1, 1)

    member this.NotTestMethodBecauseOfTypeParameters<'x>() =
      seq {
        yield Test.Equal((Exception() |> raise |> ignore<'x>), ())
      }

    member this.NotTestMethodBecauseOfParameters(i: int) =
      passingTest

    static member NotTestMethodBecauseOfStatic =
      passingTest

  type NotTestClass() =
    member this.X() = 0

  type Uninstantiatable() =
    do Exception() |> raise

    member this.PassingTest() =
      passingTest

    member this.ViolatingTest() =
      violatingTest

  type TestClassWithThrowingDispose() =
    member this.PassingTest() =
      passingTest

    member this.ThrowingTest() =
      Exception() |> raise

    interface IDisposable with
      override this.Dispose() =
        Exception() |> raise

  module TestClassTypeTest =
    let ``test testMethodInfos`` =
      test {
        let typ = typeof<TestClass1>
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
        case (typeof<TestClass1>, true)
        case (typeof<Uninstantiatable>, true)
        case (typeof<TestClassWithThrowingDispose>, true)
        case (typeof<NotTestClass>, false)
        run body
      }

    let ``test instantiate`` =
      [
        test {
          let instantiate = typeof<TestClass1> |> TestClassType.instantiate
          let instance = instantiate ()
          do! instance.GetType() |> assertEquals typeof<TestClass1>
        }
        test {
          let instantiate = typeof<Uninstantiatable> |> TestClassType.instantiate
          let! (_: exn) = trap { it (instantiate ()) }
          return ()
        }
      ]

  module TestMethodTest =
    let ``test create TestClass1`` =
      let typ = typeof<TestClass1>
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

    let ``test create TestClassWithThrowingDispose`` =
      let typ = typeof<TestClassWithThrowingDispose>
      let instantiate = typ |> TestClassType.instantiate
      [
        test {
          let testMethod = TestMethod.create (instantiate ()) (typ.GetMethod("PassingTest"))
          do! testMethod.DisposingError |> assertSatisfies Option.isSome
        }
      ]
