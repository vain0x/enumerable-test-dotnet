namespace EnumerableTest.Runner.UnitTest

open System
open EnumerableTest

module TestClass =
  let passingTest = 
    seq {
      yield (0).Is(0)
    }

  let violatingTest =
    seq {
      yield (0).Is(1)
    }

  let throwingTest =
    seq {
      yield (0).Is(0)
      yield (0).Is(1)
      exn() |> raise
    }

  let neverTest: seq<Test> =
    seq {
      yield (0).Is(0)
      yield (0).Is(1)
      while true do
        ()
    }

  type Passing() =
    member this.PassingTest() =
      passingTest

  type Violating() =
    member this.PassingTest() =
      passingTest

    member this.ViolatingTest() =
      violatingTest

  type Never() =
    member this.PassingTest() =
      passingTest

    member this.ViolatedTest() =
      violatingTest

    member this.NeverTest() =
      neverTest

  type Uninstantiatable() =
    do Exception() |> raise

    member this.PassingTest() =
      passingTest

    member this.ViolatingTest() =
      violatingTest

  type WithThrowingDispose() =
    member this.PassingTest() =
      passingTest

    member this.ThrowingTest() =
      throwingTest

    interface IDisposable with
      override this.Dispose() =
        exn() |> raise

  type WithManyProperties() =
    member this.PassingTestMethod() =
      passingTest

    member this.ViolatingTestMethod() =
      violatingTest

    member this.ThrowingTestMethod() =
      throwingTest

    member this.NotTestMethodBecauseOfBeingProperty
      with get () =
        passingTest

    member this.NotTestMethodBecauseOfReturnType() =
      Test.Equal(1, 1)

    member this.NotTestMethodBecauseOfTypeParameters<'x>() =
      seq {
        yield Test.Equal((exn() |> raise |> ignore<'x>), ())
      }

    member this.NotTestMethodBecauseOfParameters(i: int) =
      passingTest

    static member NotTestMethodBecauseOfStatic =
      passingTest

  type NotTestClass() =
    member this.X() = 0
