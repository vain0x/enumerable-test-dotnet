namespace EnumerableTest.Runner

module Async =
  let result x =
    async {
      return x
    }

  let run f =
    async {
      return f ()
    }

  let map f a =
    async {
      let! x = a
      return f x
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SynchronizationContext =
  open System.Threading

  let immediate =
    { new SynchronizationContext() with
        override this.Post(f, state) =
          f.Invoke(state)
        override this.Send(f, state) =
          f.Invoke(state)
    }

  let current () =
    SynchronizationContext.Current |> Option.ofObj

  let capture () =
    match current () with
    | Some c -> c
    | None -> immediate

  let send f (this: SynchronizationContext) =
    this.Send((fun _ -> f ()), ())
