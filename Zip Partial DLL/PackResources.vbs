' PackResources.vbs
' ------------------------------------------------------------------
'
' Copyright (c) 2010 Dino Chiesa
' All rights reserved.
'
' This code module is part of DotNetZip, a zipfile class library.
'
' ------------------------------------------------------------------
'
' This code is licensed under the Microsoft Public License.
' See the file License.txt for the license details.
' More info on: http://dotnetzip.codeplex.com
'
' ------------------------------------------------------------------
'
' last saved (in emacs):
' Time-stamp: <2010-February-10 15:06:09>
'
' ------------------------------------------------------------------
'
' This is a script file that packs the resources files into a zip,
' for inclusion into the zip dll.
'
' This script assumes it will be run by Visual Studio, as a prebuild
' script, starting with the current directory of
' {DotNetZip}\Zip Partial DLL\bin\{Debug,Release}
'
' Wed, 10 Feb 2010  12:24
'
' ------------------------------------------------------------------


Sub NewZip(pathToZipFile)

    WScript.Echo "Newing up a zip file (" & pathToZipFile & ") "

    Dim fso
    Set fso = CreateObject("Scripting.FileSystemObject")
    Dim file
    Set file = fso.CreateTextFile(pathToZipFile)

    '' this is the content for an empty zip file
    file.Write Chr(80) & Chr(75) & Chr(5) & Chr(6) & String(18, 0)

    file.Close
    Set fso = Nothing
    Set file = Nothing

    WScript.Sleep 500

End Sub



Sub CreateZip(pathToZipFile, dirToZip)

    Dim fso
    Set fso= Wscript.CreateObject("Scripting.FileSystemObject")

    Dim fullPathToZipFile
    fullPathToZipFile = fso.GetAbsolutePathName(pathToZipFile)

    Dim fullDirToZip
    fullDirToZip = fso.GetAbsolutePathName(dirToZip)

    If Not fso.FolderExists(fullDirToZip) Then
        WScript.Echo "The directory to zip does not exist."
        Exit Sub
    End If

    WScript.Echo "Creating zip  (" & fullPathToZipFile & ") from (" & fullDirToZip & ")"

    If fso.FileExists(fullPathToZipFile) Then
        WScript.Echo "That zip file already exists - deleting it."
        fso.DeleteFile fullPathToZipFile
    End If

    NewZip fullPathToZipFile

    dim sa
    set sa = CreateObject("Shell.Application")

    Dim zip
    Set zip = sa.NameSpace(fullPathToZipFile)

    WScript.Echo "zipping files in dir  (" & fullDirToZip & ")"

    ' http://msdn.microsoft.com/en-us/library/bb787866(VS.85).aspx
    ' ===============================================================
    ' 4 = do not display a progress box
    ' 16 = Respond with "Yes to All" for any dialog box that is displayed.
    ' 128 = Perform the operation on files only if a wildcard file name (*.*) is specified.
    ' 256 = Display a progress dialog box but do not show the file names.
    ' 2048 = Version 4.71. Do not copy the security attributes of the file.
    ' 4096 = Only operate in the local directory. Don't operate recursively into subdirectories.

    Dim fcount
    fcount = 0

    Dim folder, file, builtpath
    Set folder = fso.GetFolder(fullDirToZip)
    For Each file in folder.Files
        '' zip any file that is not .zip, not .resx and not ending in ~ (emacs backup file)
        If (Right(file.name,4) <> ".zip" AND Right(file.name,5) <> ".resx" AND Right(file.name,1) <> "~") Then
            builtpath = fso.BuildPath(fullDirToZip, file.Name)
            'WScript.Echo file.name
            'zip.CopyHere file.name, 0
            WScript.Echo builtpath
            zip.CopyHere builtpath, 0
            fcount = fcount + 1
            '' with no delay in-between, the zip fails with "file not found"
            '' or some other spurious error.
            Wscript.Sleep(50)
        End If
    Next

    '' the zip process is asynchronous. wait for completion.
    Dim sLoop
    sLoop = 0
    Do Until fcount <= zip.Items.Count
        Wscript.Sleep(400)
        sLoop = sLoop + 1
        If (sLoop = 6) Then
            WScript.Echo "/ items so far = " & zip.items.Count
            WScript.Echo "(looking for " & fcount & " items)"
            sLoop = 0
        End IF
    Loop

    Set fso = Nothing
    Set sa = Nothing
    Set zip = Nothing
    Set folder = Nothing

End Sub



CreateZip "..\..\Resources\zippedResources.zip", "..\..\Resources"
