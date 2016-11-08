﻿namespace EnumerableTest.Runner

open System

type TestStatistic =
  {
    AssertionCount              : AssertionCount
    Duration                    : TimeSpan
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatistic =
  let zero =
    {
      AssertionCount            = AssertionCount.zero
      Duration                  = TimeSpan.Zero
    }

  let add (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount            = AssertionCount.add l.AssertionCount r.AssertionCount
      Duration                  = l.Duration + r.Duration
    }

  let subtract (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount            = AssertionCount.subtract l.AssertionCount r.AssertionCount
      Duration                  = l.Duration - r.Duration
    }

  let ofTestMethod testMethod =
    {
      AssertionCount            = testMethod |> AssertionCount.ofTestMethod
      Duration                  = testMethod.Duration
    }
