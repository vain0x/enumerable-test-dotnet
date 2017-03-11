namespace EnumerableTest.Runner

open System
open System.Collections.Concurrent
open System.Reflection
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassResult =
  let isPassed (testClassResult: TestClassResult) =
    testClassResult.InstantiationError.IsNone
    && testClassResult.NotCompletedMethods |> Array.isEmpty
    && testClassResult.Result |> Seq.forall TestMethodResult.isPassed

  let assertionCount (testClassResult: TestClassResult) =
    seq {
      match testClassResult.InstantiationError with
      | Some e ->
        yield AssertionCount.oneError
      | None ->
        for testMethodResult in testClassResult.Result do
          yield AssertionCount.ofTestMethodResult testMethodResult
        yield AssertionCount.ofNotCompleted (testClassResult.NotCompletedMethods |> Array.length)
    }
    |> Seq.fold AssertionCount.add AssertionCount.zero
