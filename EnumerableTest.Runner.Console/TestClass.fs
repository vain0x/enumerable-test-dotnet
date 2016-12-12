namespace EnumerableTest.Runner.Console

open System
open System.Collections.Concurrent
open System.Reflection
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.NotCompletedMethods |> Array.isEmpty
    && testClass.Result |> Seq.forall TestMethod.isPassed

  let assertionCount (testClass: TestClass) =
    seq {
      match testClass.InstantiationError with
      | Some e ->
        yield AssertionCount.oneError
      | None ->
        for testMethod in testClass.Result do
          yield AssertionCount.ofTestMethod testMethod
        yield AssertionCount.ofNotCompleted (testClass.NotCompletedMethods |> Array.length)
    }
    |> Seq.fold AssertionCount.add AssertionCount.zero
