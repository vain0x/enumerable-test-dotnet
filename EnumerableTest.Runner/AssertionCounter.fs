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
          let totalCount = count.TotalCount + 1
          let (violatedCount, errorCount) =
            match result with
            | Success assertionResult ->
              match assertionResult with
              | Passed          -> (count.ViolatedCount, count.ErrorCount)
              | Violated _      -> (count.ViolatedCount + 1, count.ErrorCount)
            | Failure _         -> (count.ViolatedCount, count.ErrorCount + 1)
          {
            TotalCount          = totalCount
            ViolatedCount       = violatedCount
            ErrorCount          = errorCount
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
