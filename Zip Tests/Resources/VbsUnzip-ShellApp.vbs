' VbsUnzip.vbs
' ------------------------------------------------------------------
'
' Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
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
' Time-stamp: <2009-May-29 18:00:30>
'
' ------------------------------------------------------------------
'
' This is a script file that unzips a specified zip file to a specified directory. 
' It uses the Shell.Application object to do the unzipping.
' This script is used for compatibility testing of the DotNetZip output.
'
' created Fri, 29 May 2009  17:07
'
' ------------------------------------------------------------------



Sub UnpackZip(pathToZipFile, extractLocation)

    WScript.Echo "Unpacking zip  (" & pathToZipFile & ") to (" & extractLocation & ")"
    
    dim sa
    set sa = CreateObject("Shell.Application") 

    Dim zip
    Set zip = sa.NameSpace(pathToZipFile)

    Dim fso
    Set fso= CreateObject("Scripting.FileSystemObject")
    If Not fso.FolderExists(extractLocation) Then fso.CreateFolder(extractLocation)
    
    Dim ex
    Set ex = sa.NameSpace(extractLocation)

    ' http://msdn.microsoft.com/en-us/library/bb787866(VS.85).aspx
    ' ===============================================================
    ' 4 = do not display a progress box
    ' 16 = Respond with "Yes to All" for any dialog box that is displayed.
    ' 128 = Perform the operation on files only if a wildcard file name (*.*) is specified. 
    ' 256 = Display a progress dialog box but do not show the file names.
    ' 2048 = Version 4.71. Do not copy the security attributes of the file.
    ' 4096 = Only operate in the local directory. Don't operate recursively into subdirectories.
    
    ex.CopyHere zip.items, 20

End Sub



Sub Main()
    
    dim args

    set args = WScript.Arguments 

    If (args.Length = 2) Then
        
        UnpackZip args(0), args(1)
        
    Else
        WScript.Echo "VbsUnzip.vbs - unzip a zip file using the Shell.Application object."
        WScript.Echo "  usage: VbsUnzip.vbs  <pathToZip>  <extractLocation>"
    End If
    
End Sub

Call Main
