

function ZipUp-Files ( $directory )
{
  $children = get-childitem -path $directory
  foreach ($o in $children) 
  {
    if (!$BaseDir -or ($BaseDir -eq "")) {
      $ix = $o.PSParentPath.IndexOf("::")
      $BaseDir = $o.PSParentPath.Substring($ix+2)
      $x = get-item $BaseDir
      $ix = $x.PSParentPath.IndexOf("::")
      $ParentOfBase = $x.PSParentPath.Substring($ix+2) + "\"
    }

    if ($o.Name -ne "TestResults" -and
      $o.Name -ne "obj"      -and
    $o.Name -ne "bin"      -and
    $o.Name -ne "_tfs"     -and
    $o.Name -ne "notused"     -and
    $o.Name -ne "AppNote.txt"     -and
    $o.Name -ne "CodePlex-Readme.txt"     -and
    $o.Name -ne "ReadThis.txt"     -and
    $o.Name -ne "Ionic.snk"     -and
    $o.Name -ne "Ionic.pfx"     -and
    $o.Name -ne "Debug"     -and
    $o.Name -ne "Release"  )
     # -and $o.Name -ne "Resources"  )
    {
      if ($o.PSIsContainer)
      {
        ZipUp-Files ( $o.FullName )
      }
      else 
      {
        #Write-output $o.FullName
        if ($o.Name -and 
          $o.Name -ne "" -and
        $o.Name -ne ".tfs-ignore" -and
        (!$o.Name.EndsWith("~")) -and
        (!$o.Name.EndsWith("#")) -and
        (!$o.Name.EndsWith(".vsp")) -and
        (!$o.Name.EndsWith(".cache")) 
        # -and (!$o.Name.EndsWith(".zip"))  # was eliminating test cases
        )
        {
          Write-output $o.FullName.Replace($ParentOfBase, "")
          $e= $zipfile.AddFile($o.FullName.Replace($ParentOfBase, ""))
        }
      }
    }
  }
}


[System.Reflection.Assembly]::LoadFrom("c:\\dinoch\\bin\\Ionic.Zip.dll");

$version = get-content -path 'DotNetZip\Zip Partial DLL\Properties\AssemblyInfo.cs' | select-string -pattern 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'  |  %{$_ -replace "[^0-9.]",""}

$ZipFileName = "DotNetZip-src-v$version.zip"

$zipfile =  new-object Ionic.Zip.ZipFile($ZipFileName);

ZipUp-Files "DotNetZip"

$zipfile.Save()

