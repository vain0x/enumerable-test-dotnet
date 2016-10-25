namespace EnumerableTest.Runner

open System
open Basis.Core

type AssertionCount =
  {
    TotalCount                  : int
    ViolatedCount               : int
    ErrorCount                  : int
  }
with
  static member Zero =
    {
      TotalCount                = 0
      ViolatedCount             = 0
      ErrorCount                = 0
    }

type AssertionCounter() =
  let count = ref AssertionCount.Zero

  member this.Current =
    !count

  member this.Add(testClassResult: TestClassResult) =
    let newCount =
      testClassResult
      |> TestClassResult.allAssertionResults
      |> Seq.fold
        (fun count result ->
          let (violatedCountIncrement, errorCountIncrement) =
            match result with
            | Success assertionResult ->
              match assertionResult with
              | Passed          -> (0, 0)
              | Violated _      -> (1, 0)
            | Failure _         -> (0, 1)
          {
            TotalCount          = count.TotalCount + 1
            ViolatedCount       = count.ViolatedCount + violatedCountIncrement
            ErrorCount          = count.ErrorCount + errorCountIncrement
          }
        ) (!count)
    count := newCount

  member this.IsAllGreen =
    let count = !count
    count.ViolatedCount = 0 && count.ErrorCount = 0

  interface IObserver<TestClassResult>  with
    override this.OnNext(testClassResult) =
      this.Add(testClassResult)

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
