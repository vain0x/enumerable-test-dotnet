﻿namespace EnumerableTest.Runner.Wpf

open Basis.Core
open EnumerableTest.Sdk
open EnumerableTest.Runner

type TestStatus =
  | NotCompleted
  | Passed
  | Violated
  | Error

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatus =
  let ofAssertion (assertion: SerializableAssertion) =
    if assertion.IsPassed
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
