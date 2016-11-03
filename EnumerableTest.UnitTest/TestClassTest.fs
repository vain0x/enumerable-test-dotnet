namespace EnumerableTest.UnitTest

open System
open System.Reflection
open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner

module TestClassTest =
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

  let ``test testMethodInfos`` =
    test {
      let typ = typeof<TestClass1>
      let expected =
        [
          typ.GetMethod("PassingTestMethod")
          typ.GetMethod("ViolatingTestMethod")
          typ.GetMethod("ThrowingTestMethod")
        ]
      do! typ |> TestClass.testMethodInfos |> assertSeqEquals expected
    }
