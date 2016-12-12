namespace EnumerableTest.Runner.Wpf

type NotExecutedResult private () =
  static member val Instance =
    new NotExecutedResult()
