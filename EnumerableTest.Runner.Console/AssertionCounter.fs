namespace EnumerableTest.Runner.Console

open System
open Basis.Core
open EnumerableTest.Runner
open EnumerableTest.Sdk

type AssertionCounter() =
  let count = ref AssertionCount.zero
  
  member this.Current =
    !count

  member this.IsPassed =
    !count |> AssertionCount.isPassed

  interface IObserver<TestClassResult>  with
    override this.OnNext(testClassResult) =
      count := AssertionCount.add (!count) (testClassResult |> TestClassResult.assertionCount)

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
