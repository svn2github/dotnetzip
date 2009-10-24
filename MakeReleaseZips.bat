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


@REM get the version: 
for /f "delims==" %%I in ('type SolutionInfo.cs ^| c:\utils\grep AssemblyVersion ^| c:\utils\sed -e "s/^.*(.\(.*\).).*/\1 /"') do set longversion=%%I

set version=%longversion:~0,3%
echo version is %version%


c:\.net3.5\msbuild.exe DotNetZip.sln /p:Configuration=Debug
c:\.net3.5\msbuild.exe DotNetZip.sln /p:Configuration=Release

call :CheckSign
if ERRORLEVEL 1 (exit /b %ERRORLEVEL%)

echo making release dir ..\releases\v%version%-%stamp%
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
:CheckSign

  @REM check the digital sig on the various DLLs

  SETLOCAL EnableExtensions EnableDelayedExpansion
  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Checking signatures...
  echo.

    set verbose=1
    set rcode=0
    set ccount=0
    set okcount=0
    set notsigned=0
    for /R %%D in (*.dll) do (
        call :BACKTICK pubkey c:\netsdk2.0\bin\sn.exe -q -T "%%D"
        set /a ccount=!ccount!+1
        If "!pubkey:~-44!"=="does not represent a strongly named assembly" (
            set /a notsigned=!notsigned!+1
            if %verbose% GTR 0 (echo !pubkey!)
        ) else (
            if %verbose% GTR 0 (echo %%D  !pubkey!)
            If /i "!pubkey:~-16!"=="edbe51ad942a3f5c" (
                set /a okcount=!okcount!+1
            ) else (
                set /a rcode=!rcode!+1
            )
        )
    )

    if %verbose% GTR 0 (
      echo Checked !ccount! files 
      echo !notsigned! were not signed
      echo !okcount! were signed, with the correct key
      echo !rcode! were signed, with the wrong key
    )

    if !rcode! GTR 0 exit /b !rcode!
    if !okcount! LSS 67 exit /b !okcount!

  echo.
  echo.
  
  endlocal

goto :EOF
-------------------------------------------------------



--------------------------------------------
:MakeHelpFile

  @REM example output hedklp file name:  DotNetZipLib-v1.5.chm

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Invoking Sandcastle HFB to make the Compiled Help File
  echo.

  @REM "C:\Program Files\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe" DotNetZip.shfb

  c:\.net3.5\msbuild.exe  /p:Configuration=Release   Help\Dotnetzip.shfbproj
  move Help\out\DotNetZipLib-v*.chm ..\releases\v%version%-%stamp%

goto :EOF
--------------------------------------------




--------------------------------------------
:MakeDevelopersRedist

  @REM example output zipfile name:  DotNetZipLib-DevKit-v1.5.zip

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Developer's redistributable zip...
  echo.

  set zipfile=DotNetZipLib-DevKit-v%version%.zip

  echo zipfile is %zipfile%
  echo current dir is
  cd
  echo.

  %zipit% ..\releases\v%version%-%stamp%\%zipfile%    -s Contents.txt "This is the Developer's Kit package for DotNetZip v%version%.  This package was packed %stamp%.  In this zip you will find Debug and Release DLLs for the various versions of the Ionic.Zip class library and the Ionic.Zlib class library.  There is a separate top-level folder for each distinct version of the DLL, and within those top-level folders there are Debug and Release folders.  In the Debug folders you will find a DLL, a PDB, and an XML file for the given library, while the Release folder will have just a DLL.  The DLL is the actual library (either Debug or Release flavor), the PDB is the debug information, and the XML file is the intellisense doc for use within Visual Studio.  If you have any questions, please check the forums on http://www.codeplex.com/DotNetZip"  -s PleaseDonate.txt  "Don't forget: DotNetZip is donationware.  Please donate. It's for a good cause. http://cheeso.members.winisp.net/DotNetZipDonate.aspx"   Readme.txt License.txt

  %zipit% ..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%   -s Readme.txt "DotNetZip Library Developer's Kit package,  v%version% packed %stamp%.  This is the DotNetZip library.  It includes the classes in the Ionic.Zip namespace as well as the classes in the Ionic.Zlib namespace. Use this library if you want to manipulate ZIP files within .NET applications."

  cd "Zip Full DLL\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%\Debug    Ionic.Zip.dll Ionic.Zip.XML Ionic.Zip.pdb
  cd ..\Release
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%\Release  Ionic.Zip.dll

  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%-Reduced  -s Readme.txt "DotNetZip Reduced Library, Developer's Kit package, v%version% packed %stamp%.   This is the reduced version of the DotNetZip library.  It includes the classes in the Ionic.Zip namespace as well as the classes in the Ionic.Zlib namespace.  The reduced library differs from the full library in that it lacks the ability to save self-Extracting archives (aka SFX files), and is much smaller than the full library."
  cd ..\..\..
  cd "Zip Reduced\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%-Reduced\Debug     Ionic.Zip.Reduced.dll Ionic.Zip.Reduced.pdb Ionic.Zip.Reduced.XML
  cd ..\Release
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%   -d DotNetZip-v%version%-Reduced\Release   Ionic.Zip.Reduced.dll 


  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d DotNetZip-v%version%-CompactFramework  -s Readme.txt  "DotNetZip CF Library v%version% packed %stamp%. This assembly is built for the Compact Framework v2.0 or later, and includes all the classes in the Ionic.Zip namespace, as well as all the classes in the Ionic.Zlib namespace. Use this library if you want to manipulate ZIP files in smart-device applications, and if you want to use ZLIB compression directly, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream." 
  cd ..\..\..
  cd "Zip CF Full DLL\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d DotNetZip-v%version%-CompactFramework\Debug  Ionic.Zip.CF.dll Ionic.Zip.CF.pdb Ionic.Zip.CF.XML
  cd ..\Release
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d DotNetZip-v%version%-CompactFramework\Release  Ionic.Zip.CF.dll 


  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%  -s Readme.txt  "DotNetZlib v%version% packed %stamp%.  This is the Ionic.Zlib assembly; it includes only the classes in the Ionic.Zlib namespace. Use this library if you want to take advantage of ZLIB compression directly, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream."
  cd ..\..\..
  cd "Zlib\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%\Debug  Ionic.Zlib.dll Ionic.Zlib.pdb Ionic.Zlib.XML
  cd ..\Release
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%\Release  Ionic.Zlib.dll 


  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%-CompactFramework  -s Readme.txt  "DotNetZlib CF v%version% packed %stamp%. This is the Ionic.Zlib library packaged for the .NET Compact Framework v2.0 or later.  Use this library if you want to take advantage of ZLIB compression directly from within Smart device applications, or if you want to use the compressing stream classes like GZipStream, DeflateStream, or ZlibStream." 
  cd ..\..\..
  cd "Zlib CF\bin\Debug"
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%-CompactFramework\Debug   Ionic.Zlib.CF.dll Ionic.Zlib.CF.pdb Ionic.Zlib.CF.XML
  cd ..\Release
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%    -d Zlib-v%version%-CompactFramework\Release   Ionic.Zlib.CF.dll 

  cd ..\..\..
  cd ..\releases\v%version%-%stamp%
  for %%V in ("*.chm") do   %zipit% %zipfile%  %%V
  cd ..\..\DotNetZip

goto :EOF
--------------------------------------------



--------------------------------------------
:MakeRuntimeRedist

  @REM example output zipfile name:  DotNetZipLib-Runtime-v1.5.zip

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the user's redistributable zip...
  echo.


  set zipfile=DotNetZipLib-Runtime-v%version%.zip

  echo zipfile is %zipfile%
  %zipit% ..\releases\v%version%-%stamp%\%zipfile%    -s Contents.txt "This is the redistributable package for DotNetZip v%version%.  Packed %stamp%. In this zip you will find a separate folder for each separate version of the DLL. In each folder there is a RELEASE build DLL, suitable for redistribution with your app. If you have any questions, please check the forums on http://www.codeplex.com/DotNetZip "   -s PleaseDonate.txt  "Don't forget: DotNetZip is donationware.  Please donate. It's for a good cause. http://cheeso.members.winisp.net/DotNetZipDonate.aspx"   Readme.txt License.txt
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
--------------------------------------------




--------------------------------------------
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
--------------------------------------------



--------------------------------------------
:MakeUtilsMsi

  @REM example output zipfile name:   DotNetZipUtils-v1.8.msi

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
--------------------------------------------



--------------------------------------------
:MakeRuntimeMsi

  @REM example output zipfile name:   DotNetZip-Runtime-v1.8.msi

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Runtime MSI...
  echo.

  c:\vs2008\Common7\ide\devenv.exe DotNetZip.sln /build release /project "RuntimeSetup MSI"
  echo waiting for RuntimeSetup\release\DotNetZipLib-Runtime.msi
  c:\dinoch\dev\dotnet\AwaitFile RuntimeSetup\release\DotNetZipLib-Runtime.msi
  move RuntimeSetup\release\DotNetZipLib-Runtime.msi ..\releases\v%version%-%stamp%\DotNetZipLib-Runtime-v%version%.msi

goto :EOF
--------------------------------------------




--------------------------------------------
:MakeSrcZip

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Source Zip...
  echo.

    @REM set zipfile=DotNetZip-src-v%version%.zip

    cd..
    @REM Delete any existing files 
    for %%f in (DotNetZip-src-v*.zip) do del %%f

    c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe  DotNetZip\ZipSrc.ps1

    @REM edit in place to remove Ionic.pfx and Ionic.snk from the csproj files

    for %%f in (DotNetZip-src-v*.zip) do set actualFilename=%%f

    DotNetZip\EditCsproj.exe -z %actualFileName%

    @REM move DotNetZip-src-v*.zip  releases\v%version%-%stamp%
    move %actualFileName%  releases\v%version%-%stamp%

    cd DotNetZip

goto :EOF
--------------------------------------------


--------------------------------------------
:BACKTICK
    call :GET_CMDLINE %*
    set varspec=%1
    setlocal EnableDelayedExpansion
    for /f "usebackq delims==" %%I in (`%CMDLINE%`) do set output=%%I
    endlocal & set %varspec%=%output%
    goto :EOF

--------------------------------------------

--------------------------------------------
:GET_CMDLINE
    @REM given a set of params [0..n], sets CMDLINE to 
    @REM the join of params[1..n]
    setlocal enableextensions EnableDelayedExpansion
    set PRIOR=
    set PARAMS=
    shift
    :GET_PARAMs_LOOP
    if [%1]==[] goto GET_PARAMS_DONE
    set PARAMS=%PARAMS% %1
    shift
    goto GET_PARAMS_LOOP
    :GET_PARAMS_DONE
    REM strip the first space
    set PARAMS=%PARAMS:~1%
    endlocal & set CMDLINE=%PARAMS%
    goto :EOF
--------------------------------------------





:END
@if exist c:\dinoch\dev\dotnet\pronounceword.exe (c:\dinoch\dev\dotnet\pronounceword.exe All Done > nul)
echo release zips are in releases\v%version%-%stamp%

endlocal



