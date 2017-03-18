rem Usage:
rem   1. Make a release build.
rem   2. $ pack version-number

".paket\paket" pack output nugets version %1 && ".paket\paket" push file nugets/EnumerableTest.Core.%1.nupkg && ".paket\paket" push file nugets/EnumerableTest.Runner.Console.%1.nupkg && ".paket\paket" push file nugets/EnumerableTest.Runner.Wpf.%1.nupkg && rmdir nugets /S /Q
