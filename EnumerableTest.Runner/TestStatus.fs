namespace EnumerableTest.Runner

open Basis.Core
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatus =
  let ofTest (test: SerializableTest) =
    if test.IsPassed
      then Passed
      else Violated

  let ofTestStatistic (testStatistic: TestStatistic) =
    let k = testStatistic.AssertionCount
    if k.ErrorCount > 0 then
      Error
    elif k.ViolatedCount > 0 then
      Violated
    elif k.NotCompletedCount > 0 then
      NotCompleted
    else
      Passed
