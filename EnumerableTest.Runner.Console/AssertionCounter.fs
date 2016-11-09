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
          yield AssertionCount.ofTestMethod testMethod
    }
    |> Seq.fold AssertionCount.add count

  let count = ref AssertionCount.zero
  
  member this.Current =
    !count

  member this.IsAllGreen =
    !count |> AssertionCount.isAllGreen

  interface IObserver<TestClass>  with
    override this.OnNext(testClass) =
      count := !count |> addTestClass testClass

    override this.OnError(_) = ()

    override this.OnCompleted() = ()
