namespace EnumerableTest.Runner.Console

open System
open Basis.Core
open EnumerableTest.Runner
open EnumerableTest.Sdk

type AssertionCounter() =
  let addTestClass (testClass: TestClass) count =
    seq {
      match testClass.InstantiationError with
      | Some e ->
        yield AssertionCount.oneError
      | None ->
        for testMethod in testClass.Result do
          yield! testMethod.Result |> AssertionCount.ofGroupTest
          match testMethod.DisposingError with
          | Some _ ->
            yield AssertionCount.oneError
          | None ->
            ()
    }
    |> Seq.fold AssertionCount.add count

  let isAllGreen (count: AssertionCount) =
    count.ViolatedCount = 0 && count.ErrorCount = 0

  let count = ref AssertionCount.zero
  
  member this.Current =
    !count

  member this.IsAllGreen =
    !count |> isAllGreen

  interface IObserver<TestClass>  with
    override this.OnNext(testClass) =
      count := !count |> addTestClass testClass

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
