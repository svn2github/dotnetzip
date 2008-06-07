REM @echo off
goto START

-------------------------------------------------------
 GetResources.bat

 Copy resources from the CreateSelfExtractor Example project to be used in the Zip library.
 This script assumes it will be run by Visual Studio, starting with the 
 current directory of C:\dinoch\dev\dotnet\zip\DotNetZip\Library\bin\Debug

 Sat, 07 Jun 2008  10:39

-------------------------------------------------------


:START
setlocal
cd ..\..\
copy /y ..\Examples\SelfExtracting\CommandLineSelfExtractorStub.cs        Resources
copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.cs           Resources
copy /y ..\Examples\SelfExtracting\PasswordDialog.cs                      Resources
copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.Designer.cs  Resources
copy /y ..\Examples\SelfExtracting\PasswordDialog.Designer.cs             Resources

copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.resx         Resources
copy /y ..\Examples\SelfExtracting\PasswordDialog.resx                    Resources

endlocal
:END



