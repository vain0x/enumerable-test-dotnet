namespace EnumerableTest.Runner

module FileSystemInfo =
  open System
  open System.IO

  /// Enumerates ancestors in descending order.
  let ancestors =
    let rec loop (directory: DirectoryInfo) =
      seq {
        yield directory
        match directory.Parent with
        | null -> ()
        | parent ->
          yield! loop parent
      }
    fun fs ->
      match (fs: FileSystemInfo) with
      | :? DirectoryInfo as directory ->
        loop directory
      | :? FileInfo as file ->
        loop file.Directory
      | _ ->
        Seq.empty

  let findTestAssemblies (thisFile: FileInfo) =
    seq {
      for packagesDirectory in thisFile |> ancestors |> Seq.filter (fun d -> d.Name = "packages") do
      for solutionDirectory in packagesDirectory.Parent |> Option.ofObj |> Option.toArray do
      for suffix in [".UnitTest"; ".Tests"; ".UnitTests"] do
      for projectDirectory in solutionDirectory.GetDirectories(sprintf "*%s" suffix) do
      for binDirectory in projectDirectory.GetDirectories("bin") do
      for debugDirectory in binDirectory.GetDirectories("Debug") do
      for extension in [".dll"; ".exe"] do
      yield! debugDirectory.GetFiles(sprintf "*%s%s" suffix extension)
    }

  let getTestAssemblies (thisFile: FileInfo) =
    seq {
#if DEBUG
      yield FileInfo(@"..\..\..\EnumerableTest.Sandbox\bin\Debug\EnumerableTest.Sandbox.dll")

#endif
      yield! findTestAssemblies thisFile
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FileInfo =
  open System
  open System.IO
  open System.Reactive.Disposables
  open FSharp.Control.Reactive

  let observeChanged (file: FileInfo) =
    { new IObservable<FileSystemEventArgs> with
        override this.Subscribe(observer) =
          let watcher =
            new FileSystemWatcher(file.DirectoryName, file.Name)
          let subscription =
            watcher.Changed |> Observable.subscribeObserver observer
          watcher.NotifyFilter <- NotifyFilters.LastWrite
          watcher.EnableRaisingEvents <- true
          StableCompositeDisposable.Create(watcher, subscription) :> IDisposable
    }
