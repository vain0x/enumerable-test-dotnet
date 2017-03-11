namespace EnumerableTest.Runner

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection

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
