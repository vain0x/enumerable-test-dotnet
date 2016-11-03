namespace EnumerableTest.Runner

open System
open Basis.Core
open EnumerableTest.Sdk

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

  let onePassed =
    {
      TotalCount                = 1
      ViolatedCount             = 0
      ErrorCount                = 0
    }

  let oneViolated =
    {
      TotalCount                = 1
      ViolatedCount             = 1
      ErrorCount                = 0
    }

  let oneError =
    {
      TotalCount                = 1
      ViolatedCount             = 0
      ErrorCount                = 1
    }

  let add (l: AssertionCount) (r: AssertionCount) =
    {
      TotalCount                = l.TotalCount + r.TotalCount
      ViolatedCount             = l.ViolatedCount + r.ViolatedCount
      ErrorCount                = l.ErrorCount + r.ErrorCount
    }

  let ofAssertion (assertion: Assertion) =
    if assertion.IsPassed
      then onePassed
      else oneViolated

  let ofGroupTest (groupTest: GroupTest) =
    seq {
      for assertion in groupTest.Assertions do
        yield assertion |> ofAssertion
      if groupTest.ExceptionOrNull |> isNull |> not then
        yield oneError
    }

  let addTestClass (testClass: TestClass) count =
    seq {
      match testClass.InstantiationError with
      | Some e ->
        yield oneError
      | None ->
        for testMethod in testClass.Result do
          yield! testMethod.Result |> ofGroupTest
          match testMethod.DisposingError with
          | Some _ ->
            yield oneError
          | None ->
            ()
    }
    |> Seq.fold add count

  let isAllGreen (count: AssertionCount) =
    count.ViolatedCount = 0 && count.ErrorCount = 0

  let count = ref zero
  
  member this.Current =
    !count

  member this.IsAllGreen =
    !count |> isAllGreen

  interface IObserver<TestClass>  with
    override this.OnNext(testClass) =
      count := !count |> addTestClass testClass

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
