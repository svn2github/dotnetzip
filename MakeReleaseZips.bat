@echo off
goto START

-------------------------------------------------------
 MakeReleaseZips.bat

 Makes the zips, msi's, and chm for the DotNetZip release content.

 Thu, 19 Jun 2008  22:17


-------------------------------------------------------


:START

setlocal

set zipit=c:\dinoch\bin\zipit.exe
set stamp=%DATE% %TIME%
set stamp=%stamp:/=-%
set stamp=%stamp: =-%
set stamp=%stamp::=%

@set tfile1=%TEMP%\makereleasezip-%RANDOM%-%stamp%.tmp

@REM get the version: 
type "Zip Partial DLL\Properties\AssemblyInfo.cs" | c:\cygwin\bin\grep AssemblyVersion | c:\cygwin\bin\sed -e 's/^.*"\(.*\)".*/\1 /' > %tfile1%

call c:\dinoch\bin\setz.bat type %tfile1%

set version=%setz:~0,3%
echo version is %version%


c:\.net3.5\msbuild.exe DotNetZip.sln /p:Configuration=Release
c:\.net3.5\msbuild.exe DotNetZip.sln /p:Configuration=Debug

mkdir ..\releases\v%version%-%stamp%

call :MakeHelpFile

call :MakeDevelopersRedist

call :MakeRuntimeRedist

call :MakeZipUtils

call :MakeUtilsMsi

call :MakeRuntimeMsi

c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe .\clean.ps1

call :MakeSrcZip


goto :END


--------------------------------------------
@REM MakeHelpFile subroutine
@REM example output zipfile name:  DotNetZipLib-v1.5.chm

:MakeHelpFile

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Invoking Sandcastle HFB to make the Compiled Help File
  echo.


  "C:\Program Files\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe" DotNetZip.shfb
  move Help\DotNetZipLib-v*.chm ..\releases\v%version%-%stamp%

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeDevelopersRedist subroutine
@REM example output zipfile name:  DotNetZipLib-DevKit-v1.5.zip

:MakeDevelopersRedist

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Developer's redistributable zip...
  echo.

  set zipfile=DotNetZipLib-DevKit-v%version%.zip

  echo zipfile is %zipfile%
  %zipit% ..\releases\v%version%-%stamp%\%zipfile%    -s Contents.txt "This is the Developer's Kit package for DotNetZip v%version%.  Packed %stamp%.  In this zip you will find a separate folder for each separate version of the DLL. In each folder there is a DLL, a PDB, and an XML file.  The DLL is the actual library, the PDB is the debug information, and the XML file is the intellisense doc for use within Visual Studio.  If you have any questions, please check the forums on http://www.codeplex.com/DotNetZip "   -s PleaseDonate.txt  "Don't forget: DotNetZip is donationware.  Please donate. It's for a good cause. http://cheeso.members.winisp.net/DotNetZipDonate.aspx"   Readme.txt License.txt
  cd "Zip Full DLL\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%   -s  Readme.txt "DotNetZip Library Developer's Kit package,  v%version% packed %stamp%.  This is the full version of the DotNetZip library.  It includes the classes in the Ionic.Zip namespace as well as the classes in the Ionic.Zlib namespace. Use this library if you want to manipulate ZIP files within .NET applications." Ionic.Zip.dll Ionic.Zip.XML Ionic.Zip.pdb
  cd ..\..\..
  cd "Zip Reduced\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d DotNetZip-Reduced-v%version%  -s  Readme.txt "DotNetZip Reduced Library, Developers' Kit package, v%version% packed %stamp%.   This is the reduced version of the DotNetZip library.  It includes the classes in the Ionic.Zip namespace as well as the classes in the Ionic.Zlib namespace.  The reduced library differs from the full library in that it lacks the ability to Save Self-Extracting archives, and is much smaller than the full library. " Ionic.Zip.Reduced.dll Ionic.Zip.Reduced.pdb Ionic.Zip.Reduced.XML
  cd ..\..\..
  cd "Zlib\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%  -s Readme.txt  "Zlib v%version% packed %stamp%.  This assembly includes only the classes in the Ionic.Zlib namespace. Use this library if you want to take advantage of ZLIB compression directly, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream." Ionic.Zlib.dll Ionic.Zlib.pdb Ionic.Zlib.XML
  cd ..\..\..
  cd "Zip CF Full DLL\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d DotNetZip-v%version%-CompactFramework  -s Readme.txt  "DotNetZip CF Library v%version% packed %stamp%. This assembly is built for the Compact Framework v2.0 or later, and includes all the classes in the Ionic.Zip namespace, as well as all the classes in the Ionic.Zlib namespace. Use this library if you want to manipulate ZIP files in smart-device applications, and if you want to use ZLIB compression directly, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream."  Ionic.Zip.CF.dll Ionic.Zip.CF.pdb Ionic.Zip.CF.XML
  cd ..\..\..
  cd "Zlib CF\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%-CompactFramework  -s Readme.txt  "Zlib CF v%version% packed %stamp%. This is the Ionic.Zlib library packaged for the .NET Compact Framework v2.0 or later.  Use this library if you want to take advantage of ZLIB compression directly from within Smart device applications, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream."   Ionic.Zlib.CF.dll Ionic.Zlib.CF.pdb Ionic.Zlib.CF.XML
  cd ..\..\..
  cd ..\releases\v%version%-%stamp%
  for %%V in ("*.chm") do   %zipit% %zipfile%  %%V
  cd ..\..\DotNetZip

goto :EOF
@REM end subroutine
--------------------------------------------



--------------------------------------------
@REM MakeRuntimeRedist subroutine
@REM example output zipfile name:  DotNetZipLib-Runtime-v1.5.zip

:MakeRuntimeRedist

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the user's redistributable zip...
  echo.


  set zipfile=DotNetZipLib-Runtime-v%version%.zip

  echo zipfile is %zipfile%
  %zipit% ..\releases\v%version%-%stamp%\%zipfile%    -s Contents.txt "This is the redistributable package for DotNetZip v%version%.  Packed %stamp%. In this zip you will find a separate folder for each separate version of the DLL. In each folder there is a DLL, suitable for redistribution with your app. If you have any questions, please check the forums on http://www.codeplex.com/DotNetZip "   -s PleaseDonate.txt  "Don't forget: DotNetZip is donationware.  Please donate. It's for a good cause. http://cheeso.members.winisp.net/DotNetZipDonate.aspx"   Readme.txt License.txt
  cd "Zip Full DLL\bin\Release"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -d DotNetZip-v%version%  -s Readme.txt  "DotNetZip Redistributable Library v%version% packed %stamp%"  Ionic.Zip.dll 
  cd ..\..\..
  cd "Zip Reduced\bin\Release"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -d DotNetZip-Reduced-v%version%  -s Readme.txt  "DotNetZip Reduced Redistributable Library v%version% packed %stamp%"  Ionic.Zip.Reduced.dll
  cd ..\..\..
  cd "Zlib\bin\Release"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -d zlib-v%version%  -s Readme.txt  "DotNetZlib Redistributable Library v%version% packed %stamp%"  Ionic.Zlib.dll 
  cd ..\..\..
  cd "Zip CF Full DLL\bin\Release"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -d DotNetZip-v%version%-CompactFramework  -s Readme.txt "DotNetZip Library for .NET Compact Framework v%version% packed %stamp%"  Ionic.Zip.CF.dll 
  cd ..\..\..
  cd "Zlib CF\bin\Release"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d Zlib-v%version%-CompactFramework   -s Readme.txt  "DotNetZlib Library for .NET Compact Framework v%version% packed %stamp%"   Ionic.Zlib.CF.dll 
  cd ..\..\..

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeZipUtils subroutine

:MakeZipUtils

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Zip Utils zip...
  echo.

    set zipfile=DotNetZipUtils-v%version%.zip
    %zipit% ..\releases\v%version%-%stamp%\%zipfile%    -s Contents.txt "These are the DotNetZip utilities and tools, for DotNetZip v%version%.  Packed %stamp%."   -s PleaseDonate.txt  "Don't forget: DotNetZip is donationware.  Please donate. It's for a good cause. http://cheeso.members.winisp.net/DotNetZipDonate.aspx"   License.txt
    cd Examples
    cd ZipIt\bin\Release
    %zipit% ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -zc "Zip utilities v%version% packed %stamp%"  Zipit.exe Ionic.Zip.dll 
    cd ..\..\..\Unzip\bin\Release
    %zipit%  ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  Unzip.exe
    cd ..\..\..\SelfExtracting\bin\Release
    %zipit%  ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  ConvertZipToSfx.exe
    cd ..\..\..\WinFormsApp\bin\Release
    %zipit%  ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  DotNetZip-WinFormsTool.exe
    cd ..\..\..\..

goto :EOF
@REM end subroutine
--------------------------------------------



--------------------------------------------
@REM MakeUtilsMsi subroutine
@REM example output zipfile name:   DotNetZipUtils-v1.8.msi

:MakeUtilsMsi

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Utils MSI...
  echo.

  c:\vs2008\Common7\ide\devenv.exe DotNetZip.sln /build release /project "Zip Utilities MSI"
  echo waiting for Setup\release\DotNetZipUtils.msi
  c:\dinoch\dev\dotnet\AwaitFile Setup\Release\DotNetZipUtils.msi
  move Setup\Release\DotNetZipUtils.msi ..\releases\v%version%-%stamp%\DotNetZipUtils-v%version%.msi

goto :EOF
@REM end subroutine
--------------------------------------------


--------------------------------------------
@REM MakeRuntimeMsi subroutine
@REM example output zipfile name:   DotNetZip-Runtime-v1.8.msi

:MakeRuntimeMsi

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Runtime MSI...
  echo.

  c:\vs2008\Common7\ide\devenv.exe DotNetZip.sln /build release /project "RuntimeSetup"
  echo waiting for RuntimeSetup\release\DotNetZip-Runtime.msi
  c:\dinoch\dev\dotnet\AwaitFile RuntimeSetup\release\DotNetZip-Runtime.msi
  move RuntimeSetup\release\DotNetZip-Runtime.msi ..\releases\v%version%-%stamp%\DotNetZip-Runtime-v%version%.msi

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeSrcZip subroutine
:MakeSrcZip

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Source Zip...
  echo.

    set zipfile=DotNetZip-src-v%version%.zip

    cd..
    c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe  DotNetZip\ZipSrc.ps1

    move DotNetZip-src-v*.zip  releases\v%version%-%stamp%
    cd DotNetZip

@REM    del /q Library\Resources\*.*
@REM
@REM    c:\cygwin\bin\find.exe .  -type f | grep -v  _tfs | grep -v notused | grep -v -i setup.exe | grep -v \~ | grep -v \#  | grep -v Documentation | grep -v CodePlex-Readme.txt | grep -v semantic.cache | grep -v sln.cache | grep -v TestResults | grep -v .suo > %tfile1%
@REM
@REM
@REM    @for /f "usebackq" %%W in (%tfile1%) do call :ZIPONE %%W

goto :EOF
@REM end subroutine
--------------------------------------------


@REM 
@REM --------------------------------------------
@REM @REM ZIPONE subroutine
@REM 
@REM :ZIPONE
@REM %zipit% ..\%zipfile%  %1
@REM goto :EOF
@REM @REM end subroutine
@REM --------------------------------------------
@REM 



:END
if exist %tfile1% @del %tfile1%

echo release zips are in releases\v%version%-%stamp%

endlocal



