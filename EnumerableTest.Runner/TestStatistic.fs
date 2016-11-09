namespace EnumerableTest.Runner

open System

type TestStatistic =
  {
    AssertionCount              : AssertionCount
    Duration                    : TimeSpan
    NotCompletedTestCount       : int
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatistic =
  let zero =
    {
      AssertionCount            = AssertionCount.zero
      Duration                  = TimeSpan.Zero
      NotCompletedTestCount     = 0
    }

  let notCompleted =
    { zero with NotCompletedTestCount = 1 }

  let add (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount            = AssertionCount.add l.AssertionCount r.AssertionCount
      Duration                  = l.Duration + r.Duration
      NotCompletedTestCount     = l.NotCompletedTestCount + r.NotCompletedTestCount
    }

  let subtract (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount            = AssertionCount.subtract l.AssertionCount r.AssertionCount
      Duration                  = l.Duration - r.Duration
      NotCompletedTestCount     = l.NotCompletedTestCount - r.NotCompletedTestCount
    }

  let groupSig =
    { new GroupSig<_>() with
        override this.Unit = zero
        override this.Multiply(l, r) = add l r
        override this.Divide(l, r) = subtract l r
    }

  let ofTestMethod testMethod =
    {
      AssertionCount            = testMethod |> AssertionCount.ofTestMethod
      Duration                  = testMethod.Duration
      NotCompletedTestCount     = 0
    }
