REM @echo off
goto START

-------------------------------------------------------
 PackResources.cmd

 Zip up all files in the Resources directory, into a zip file . 

 This script assumes it will be run by Visual Studio, as a prebuild script, starting with the 
 current directory of C:\dinoch\dev\dotnet\zip\DotNetZip\Zip Partial DLL\bin\{Debug,Release}

 Tue, 27 Oct 2009  05:28

-------------------------------------------------------


:START
setlocal

cd ..\..\Resources
del zippedResources.zip
c:\dinoch\bin\zipit.exe ZippedResources.zip   "name != *.zip and name != *.resx and name != *.*~" 

endlocal
:END



