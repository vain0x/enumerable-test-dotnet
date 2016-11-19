namespace EnumerableTest.Runner

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

  let notCompleted =
    {
      AssertionCount            = AssertionCount.ofNotCompleted 1
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
    }
