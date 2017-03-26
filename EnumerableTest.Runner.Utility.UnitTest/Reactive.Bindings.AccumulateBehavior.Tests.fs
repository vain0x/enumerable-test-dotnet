namespace EnumerableTest.Runner

open System
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open Reactive.Bindings

module ``test AccumulateBehavior`` =
  let ``test Add(behavior)`` =
    test {
      let accumulate = AccumulateBehavior.create GroupSig.ofInt32
      let assert' expected =
        accumulate.Accumulation.Value |> assertEquals expected

      let behavior = ReactiveProperty.create 1
      accumulate.Add(behavior)
      do! assert' 1

      behavior.Value <- 2
      do! assert' 2

      do! accumulate.Remove(behavior) |> assertEquals true
      do! assert' 0
    }

  let ``test Add(behavior) many case`` =
    test {
      let accumulate = AccumulateBehavior.create GroupSig.ofInt32
      let assert' expected =
        accumulate.Accumulation.Value |> assertEquals expected

      let behavior1 = ReactiveProperty.create 1
      let behavior2 = ReactiveProperty.create 10
      accumulate.Add(behavior1)
      accumulate.Add(behavior2)
      do! assert' 11

      behavior1.Value <- 2
      do! assert' 12

      behavior2.Value <- 20
      do! assert' 22

      accumulate.Remove(behavior1) |> ignore
      do! assert' 20
      
      accumulate.Remove(behavior2) |> ignore
      do! assert' 0
    }

  let ``test Add(ObservableCollection)`` =
    test {
      let accumulate = AccumulateBehavior.create GroupSig.ofInt32
      let mutable expected = 0
      let assert' () =
        accumulate.Accumulation.Value |> assertEquals expected

      let initialValues = seq { 1..3 }
      let collection = ObservableCollection<_>(initialValues)
      expected <- initialValues |> Seq.sum

      accumulate.Add(collection)
      do! assert' ()

      collection.Add(4)
      expected <- expected + 4
      do! assert' ()

      collection.Remove(2) |> ignore
      expected <- expected - 2
      do! assert' ()

      collection.[0] <- 100
      expected <- expected - 1 + 100
      do! assert' ()

      collection.Move(0, 1)
      do! assert' ()

      collection.Clear()
      expected <- 0
      do! assert' ()
    }
