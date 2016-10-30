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

  let ``test tryCreate TestClass1`` =
    test {
      let typ = typeof<TestClass1>
      match typ |> TestClass.tryCreate with
      | Some testClass ->
        do! testClass.Type |> assertEquals typ
        do! testClass.Create () |> assertSatisfies (fun it -> it.GetType() = typ)
      | None ->
        do! fail ""
    }

  let ``test tryCreate NotTestClass`` =
    test {
      do! typeof<NotTestClass> |> TestClass.tryCreate |> assertSatisfies Option.isNone
    }

  let ``test tryRunTestMethod TestClass1`` =
    test {
      let typ = typeof<TestClass1>
      let testClass             = TestClass.tryCreate typ |> Option.get
      let passingTestMethod     = typ.GetMethod("PassingTestMethod") |> TestClass.testMethod
      let violatingTestMethod   = typ.GetMethod("ViolatingTestMethod") |> TestClass.testMethod
      let throwingTestMethod    = typ.GetMethod("ThrowingTestMethod") |> TestClass.testMethod

      do! testClass |> TestClass.tryRunTestMethod passingTestMethod
          |> assertSatisfies
            (function
              | Success test -> test.IsPassed
              | Failure _ -> false
            )

      do! testClass |> TestClass.tryRunTestMethod violatingTestMethod
          |> assertSatisfies
            (function
              | Success test -> test.IsPassed |> not
              | Failure _ -> false
            )

      do! testClass |> TestClass.tryRunTestMethod throwingTestMethod
          |> assertSatisfies
            (function
              | Failure { Method = TestErrorMethod.Method _ } -> true
              | _ -> false
            )
    }

  let ``test tryRunTestMethod Uninstantiatable`` =
    test {
      let typ                   = typeof<Uninstantiatable>
      let testClass             = typ |> TestClass.tryCreate |> Option.get
      let testMethod            = typ.GetMethod("PassingTest") |> TestClass.testMethod
      do! TestClass.tryRunTestMethod testMethod testClass
          |> assertSatisfies
            (function
              | Failure { Method = TestErrorMethod.Constructor } -> true
              | _ -> false
            )
    }

  let ``test tryRunTestMethod TestClassWithThrowingDispose`` =
    test {
      let typ                   = typeof<TestClassWithThrowingDispose>
      let testClass             = typ |> TestClass.tryCreate |> Option.get
      let passingTestMethod     = typ.GetMethod("PassingTest") |> TestClass.testMethod
      let throwingTestMethod    = typ.GetMethod("ThrowingTest") |> TestClass.testMethod
      do! testClass |> TestClass.tryRunTestMethod passingTestMethod
          |> assertSatisfies
            (function
              | Failure { Method = TestErrorMethod.Dispose _ } -> true
              | _ -> false
            )
      do! testClass |> TestClass.tryRunTestMethod throwingTestMethod
          |> assertSatisfies
            (function
              | Failure { Method = TestErrorMethod.Method _ } -> true
              | _ -> false
            )
    }

  let ``test unitfyInstantiationErrors`` =
    test {
      let typ                 = typeof<TestClass1>
      let testMethod          = typ.GetMethod("PassingTestMethod") |> TestClass.testMethod
      let results =
        [|
          TestError.OfConstructor(Exception()) |> Failure
          TestError.OfConstructor(Exception()) |> Failure
          TestError.OfDispose(Exception()) |> Failure
          Success ()
        |]
        |> Array.map (fun r -> (testMethod, r))
      do! results |> TestClass.unifyInstantiationErrors
          |> assertSatisfies
            (fun results ->
              let instantiationErrors =
                results |> Seq.filter
                  (function
                    | (_, Failure { Method = TestErrorMethod.Constructor }) -> true
                    | _ -> false
                  )
              instantiationErrors |> Seq.length = 1
            )
    }
