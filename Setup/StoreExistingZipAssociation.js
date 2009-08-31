// StoreExistingZipAssociation.js
//
// Store the existing default file association for .zip files, if it exists. 
// This is called from within the context of a Windows installer session.
//
// Sun, 30 Aug 2009  18:36
//


// get and store the existing association for zip files, if any
var WSHShell = new ActiveXObject("WScript.Shell");
var regValue1 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Dino Chiesa\\DotNetZip Tools v1.9\\PriorZipAssociation";
var regValue2 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\CLASSES\\.zip\\";
var selfId = "DotNetZip.zip.1";
try 
{
    var association = WSHShell.RegRead(regValue2);
    if (association != "")
    {
        if (association != selfId)
        {
            // there is an association, and it is not DotNetZip
            WSHShell.RegWrite(regValue1, association);
        }
        else
        {
            // the existing association is for DotNetZip
            try
            {
                var existing = WSHShell.RegRead(regValue1);
                if (existing == "" || existing == selfId)
                {
                    WSHShell.RegWrite(regValue1, "CompressedFolder");
                }
                else
                {
                    // there already is a stored prior association.
                    // don't change it. 
                }
            }
            catch (e1a)
            {
                WSHShell.RegWrite(regValue1, "CompressedFolder");
            }
        }
    }
    else
    {
        // there is no default association for .zip files
        WSHShell.RegWrite(regValue1, "CompressedFolder");
    }
}
catch (e1)
{
    // the key doesn't exist (no app for .zip files at all)
    WSHShell.RegWrite(regValue1, "CompressedFolder");
}




// all done - try to delete myself.
try 
{
    var scriptName = targetDir + "storeExistingZipAssociation.js";
    if (fso.FileExists(scriptName))
    {
        fso.DeleteFile(scriptName);
    }
}
catch (e2)
{
}


