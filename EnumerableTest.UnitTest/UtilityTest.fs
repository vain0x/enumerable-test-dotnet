namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module TypeTest =
  open System
  open System.Collections
  open System.Collections.Generic

  let ``test isCollectionType true`` =
    let body (typ, expected) =
      test {
        do! typ |> assertSatisfies (Type.isCollectionType >> (=) expected)
      }
    parameterize {
      case (typeof<ICollection>, true)
      case (typeof<ICollection<int>>, true)
      case (typeof<IReadOnlyList<int>>, true)
      case (typeof<IList>, true)
      case (typeof<ResizeArray<int>>, true)
      case (typeof<IDictionary<int, string>>, true)
      case (typeof<array<int>>, true)
      case (typeof<IEnumerable>, false)
      case (typeof<IEnumerable<int>>, false)
      case (typeof<option<array<int>>>, false)
      run body
    }

  let ``test isKeyValuePairType`` =
    test {
      do! typeof<int> |> assertSatisfies (Type.isKeyValuePairType >> not)
      do! typeof<KeyValuePair<int, int>> |> assertSatisfies Type.isKeyValuePairType
    }

  let ``test tryMatchKeyedCollectionType`` =
    let body (typ, expected) =
      test {
        do! typ |> Type.tryMatchKeyedCollectionType |> assertEquals expected
      }
    parameterize {
      case
        ( typeof<IReadOnlyCollection<KeyValuePair<string, int>>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case
        ( typeof<ICollection<KeyValuePair<string, int>>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case
        ( typeof<Dictionary<string, int>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case (typeof<ICollection>, None)
      case (typeof<IEnumerable<KeyValuePair<string, int>>>, None)
      run body
    }

  let ``test prettyName`` =
    let body (typ, expected) =
      test {
        do! typ |> Type.prettyName |> assertEquals expected
      }
    parameterize {
      case (typeof<int>, "int")
      case (typeof<string>, "string")
      case (typeof<byte>, "Byte")
      case (typeof<Tuple<int, string>>, "Tuple<int, string>")
      case (typeof<Dictionary<int, Tuple<int, string>>>, "Dictionary<int, Tuple<int, string>>")
      run body
    }

module ObservableTest =
  open System
  open System.Reactive.Linq
  open System.Reactive.Subjects
  open System.Threading

  let ``test waitTimeout`` =
    [
      test {
        let observable = new Subject<_>()
        do!
          observable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(1.0))
          |> assertEquals false
      }
      test {
        let observable = [| 0; 1; 2 |].ToObservable()
        do!
         observable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(1.0))
         |> assertEquals true
      }
    ]
    
  let ``test ofParallel`` =
    test {
      let executionCount = ref 0
      let notificationCount = ref 0
      let n = 5
      let computations =
        seq {
          for i in 0..(n - 1) ->
            async {
              Interlocked.Increment(executionCount) |> ignore
              return i
            }
        }
      let connectable =
        computations |> Observable.ofParallel
      connectable
      |> Observable.subscribe (fun i -> notificationCount := !notificationCount + i)
      |> ignore
      connectable.Connect()
      connectable |> Observable.wait
      do! !executionCount |> assertEquals n
      do! !notificationCount |> assertEquals (seq { 0..(n - 1) } |> Seq.sum)
    }

  let ``test startParallel`` =
    test {
      let executionCount = ref 0
      let notificationCount = ref 0
      let n = 5
      let computations =
        seq {
          for i in 0..(n - 1) ->
            async {
              Interlocked.Increment(executionCount) |> ignore
              return i
            }
        }
      let connectable =
        computations |> Observable.startParallel
      connectable
      |> Observable.subscribe (fun i -> notificationCount := !notificationCount + i)
      |> ignore
      connectable.Connect()
      connectable |> Observable.wait
      do! !executionCount |> assertEquals n
      do! !notificationCount |> assertEquals (seq { 0..(n - 1) } |> Seq.sum)
    }

module StringTest =
  let ``test convertToLF`` =
    let body (source, expected) =
      test {
        do! source |> String.convertToLF |> assertEquals expected
      }
    parameterize {
      case ("a\r\nb", "a\nb")
      case ("a\n\r\n\rb", "a\n\n\nb")
      run body
    }

module DisposableTest =
  open System.Reactive.Disposables

  module test_dispose =
    let ofDisposable =
      test {
        let count = ref 0
        let disposable = Disposable.Create(fun () -> count |> incr)
        disposable |> Disposable.dispose
        do! !count |> assertEquals 1
      }

    let ofNonDisposable =
      test {
        let x = obj()
        x |> Disposable.dispose
        return ()
      }

module ReadOnlyListTest =
  open System.Collections.Generic

  let ``test symmetricDifferenceBy`` =
    let body (left, right, expected) =
      test {
        let difference =
          ReadOnlyList.symmetricDifferenceBy fst fst
            (left :> IReadOnlyList<_>)
            (right :> IReadOnlyList<_>)
        do!
          ( difference.Left |> Seq.toArray
          , difference.Intersect |> Seq.toArray
          , difference.Right |> Seq.toArray
          ) |> assertEquals expected
      }
    parameterize {
      case
        ( [| (0, "a"); (1, "b") |]
        , [||]
        , ( [| (0, "a"); (1, "b") |]
          , [||]
          , [||]
          ))
      case
        ( [||]
        , [| (0, "A"); (1, "B") |]
        , ( [||]
          , [||]
          , [| (0, "A"); (1, "B") |]
          ))
      case
        ( [| (1, "b"); (2, "c") |]
        , [| (0, "A"); (3, "D") |]
        , ( [| (1, "b"); (2, "c") |]
          , [||]
          , [| (0, "A"); (3, "D") |]
          ))
      case
        ( [| (0, "a"); (1, "b"); (2, "c") |]
        , [| (0, "A"); (2, "C") |]
        , ( [| (1, "b") |]
          , [| (0, (0, "a"), (0, "A")); (2, (2, "c"), (2, "C")) |]
          , [||]
          ))
      case
        ( [| (0, "a"); (2, "c") |]
        , [| (2, "C"); (1, "B") |]
        , ( [| (0, "a") |]
          , [| (2, (2, "c"), (2, "C")) |]
          , [| (1, "B") |]
          ))
      run body
    }
