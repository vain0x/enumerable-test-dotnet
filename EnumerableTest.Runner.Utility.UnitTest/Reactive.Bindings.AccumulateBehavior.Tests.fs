namespace EnumerableTest.Runner

open System
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open Reactive.Bindings

module ``test AccumulateBehavior`` =
  let ``test Add`` =
    test {
      let accumulate = AccumulateBehavior.create GroupSig.ofInt32
      let assert' expected =
        accumulate.Accumulation.Value |> assertEquals expected

      let behavior = ReactiveProperty.create 1
      let subscription = accumulate.Add(behavior)
      do! assert' 1

      behavior.Value <- 2
      do! assert' 2

      subscription.Dispose()
      do! assert' 0
    }

  let ``test Add many case`` =
    test {
      let accumulate = AccumulateBehavior.create GroupSig.ofInt32
      let assert' expected =
        accumulate.Accumulation.Value |> assertEquals expected

      let behavior1 = ReactiveProperty.create 1
      let behavior2 = ReactiveProperty.create 10
      let subscription1 = accumulate.Add(behavior1)
      let subscription2 = accumulate.Add(behavior2)
      do! assert' 11

      behavior1.Value <- 2
      do! assert' 12

      behavior2.Value <- 20
      do! assert' 22

      subscription1.Dispose()
      do! assert' 20

      subscription2.Dispose()
      do! assert' 0
    }
