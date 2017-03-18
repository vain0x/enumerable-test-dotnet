namespace EnumerableTest.Runner.Wpf.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open Basis.Core
open EnumerableTest.Runner.Wpf
open EnumerableTest.Runner.UnitTest

module ``test TestTreeNode`` =
  let empty () =
    FolderNode.CreateRoot()

  let seed () =
    test {
      let root = empty ()
      do root.FindOrAddFolderNode(["a"; "ax"; "ax1"]) |> ignore
      do root.FindOrAddFolderNode(["a"; "ax"; "ax2"]) |> ignore
      do root.FindOrAddFolderNode(["a"; "ay"]) |> ignore
      do root.FindOrAddFolderNode(["b"; "bx"]) |> ignore
      return root
    }

  module ``test RouteOrFailure`` =
    let ``find self`` =
      test {
        let root = empty ()
        let node = root.RouteOrFailure([])
        do! node |> assertEquals (root :> TestTreeNode |> Success)
      }

    let ``find a child`` =
      test {
        let! root = seed ()
        let node = root.RouteOrFailure(["a"]) |> Result.get
        do! node.Name |> assertEquals "a"
        do! root.Children |> assertSatisfies (Seq.contains node)
      }

    let ``find a descendant`` =
      test {
        let! root = seed ()
        let node = root.RouteOrFailure(["a"; "ax"; "ax2"]) |> Result.get
        do! node.Name |> assertEquals "ax2"
        do! root.Children
            |> assertSatisfies
              (Seq.exists
                (fun n ->
                  n.Name = "a" && n.Children |> Seq.exists
                    (fun n ->
                      n.Name = "ax" && n.Children |> Seq.contains node
                    )))
      }

    let ``find a descendant under an internal node`` =
      test {
        let! root = seed ()
        let a = root.Children.[0] :?> FolderNode
        let ax = a.RouteOrFailure(["ax"]) |> Result.get
        let ax1 = a.RouteOrFailure(["ax"; "ax1"]) |> Result.get
        do! ax.Name |> assertEquals "ax"
        do! ax1.Name |> assertEquals "ax1"
        do! ax.Children |> assertSatisfies (Seq.contains ax1)
      }

    let ``resolve no part of path`` =
      test {
        let! root = seed ()
        do! root.RouteOrFailure(["c"]) |> assertEquals (Failure (root :> TestTreeNode, "c", []))
      }

    let ``resolve a part of path`` =
      test {
        let! root = seed ()
        let a = root.TryRoute(["a"]) |> Option.get
        do! root.RouteOrFailure(["a"; "z"]) |> assertEquals (Failure (a, "z", []))
      }
