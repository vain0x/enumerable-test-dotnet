namespace EnumerableTest.Runner

open System

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatistic =
  let create assertionCount duration =
    {
      AssertionCount =
        assertionCount
      Duration =
        duration
    }

  let zero =
    create AssertionCount.zero TimeSpan.Zero

  let notCompleted =
    create (AssertionCount.ofNotCompleted 1) TimeSpan.Zero

  let add (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount =
        AssertionCount.add l.AssertionCount r.AssertionCount
      Duration =
        l.Duration + r.Duration
    }

  let subtract (l: TestStatistic) (r: TestStatistic) =
    {
      AssertionCount =
        AssertionCount.subtract l.AssertionCount r.AssertionCount
      Duration =
        l.Duration - r.Duration
    }

  let groupSig =
    { new GroupSig<_>() with
        override this.Unit = zero
        override this.Multiply(l, r) = add l r
        override this.Divide(l, r) = subtract l r
    }

  let ofGroupTest groupTest =
    {
      AssertionCount =
        groupTest |> AssertionCount.ofGroupTest
      Duration =
        TimeSpan.Zero
    }

  let ofTestMethod testMethod =
    {
      AssertionCount =
        testMethod |> AssertionCount.ofTestMethod
      Duration =
        testMethod.Duration
    }

  let isPassed (this: TestStatistic) =
    this.AssertionCount |> AssertionCount.isPassed
