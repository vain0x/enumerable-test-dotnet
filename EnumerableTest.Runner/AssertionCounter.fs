namespace EnumerableTest.Runner

open System
open Basis.Core

type AssertionCount =
  {
    TotalCount                  : int
    ViolatedCount               : int
    ErrorCount                  : int
  }

type AssertionCounter() =
  let zero =
    {
      TotalCount                = 0
      ViolatedCount             = 0
      ErrorCount                = 0
    }

  let addAssertionResult result (count: AssertionCount) =
    let (violatedCountIncrement, errorCountIncrement) =
      match result with
      | Success assertionResult ->
        match assertionResult with
        | Passed                -> (0, 0)
        | Violated _            -> (1, 0)
      | Failure _               -> (0, 1)
    {
      TotalCount                = count.TotalCount + 1
      ViolatedCount             = count.ViolatedCount + violatedCountIncrement
      ErrorCount                = count.ErrorCount + errorCountIncrement
    }

  let addTestClassResult testClassResult count =
    testClassResult
    |> TestClassResult.allAssertionResults
    |> Seq.fold (fun count result -> count |> addAssertionResult result) count

  let isAllGreen (count: AssertionCount) =
    count.ViolatedCount = 0 && count.ErrorCount = 0

  let count = ref zero
  
  member this.Current =
    !count

  member this.IsAllGreen =
    !count |> isAllGreen

  interface IObserver<TestClassResult>  with
    override this.OnNext(testClassResult) =
      count := !count |> addTestClassResult testClassResult

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
