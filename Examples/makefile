# ==================================================================
# Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
# See the License.txt file for license information. 

_NETHOME=$(WINDIR)\Microsoft.NET\Framework\v2.0.50727
_NETSDK=c:\netsdk2.0\bin

_CS_DBG_FLAGS=/debug+
_CS_EXE_FLAGS=/t:exe $(_CS_DBG_FLAGS)

_CSC=$(_NETHOME)\csc.exe


default: UnZip.exe  ReadZip.exe  CreateZip.exe  ZipDir.exe  ZipIt.exe

Zip.dll: ..\Library\Zip.dll
	copy ..\Library\Zip.dll

UnZip.exe: Zip.dll unzip.cs
	$(_CSC) $(_CS_EXE_FLAGS) /out:UnZip.exe /r:Zip.dll UnZip.cs

ReadZip.exe: Zip.dll ReadZip.cs
	$(_CSC) $(_CS_EXE_FLAGS) /out:ReadZip.exe /r:Zip.dll ReadZip.cs

CreateZip.exe: Zip.dll CreateZip.cs
	$(_CSC) $(_CS_EXE_FLAGS) /out:CreateZip.exe /r:Zip.dll CreateZip.cs

ZipDir.exe: Zip.dll ZipDir.cs
	$(_CSC) $(_CS_EXE_FLAGS) /out:ZipDir.exe /r:Zip.dll ZipDir.cs

ZipIt.exe: Zip.dll ZipIt.cs
	$(_CSC) $(_CS_EXE_FLAGS) /out:ZipIt.exe /r:Zip.dll ZipIt.cs


clean:
	-del Zip.dll
	-del UnZip.exe UnZip.pdb
	-del ReadZip.exe ReadZip.pdb
	-del CreateZip.exe CreateZip.pdb
	-del ZipDir.exe ZipDir.pdb
	-del ZipIt.exe ZipIt.pdb



