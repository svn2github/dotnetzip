// ModifyMsiToReorderRegistryRestore.js
// 
// Performs a post-build fixup of an msi to allow restoration of
// registry values on uninstall.
// 
//
// First idea was to use a "custom action" for install and uninstall to preserve
// and restore, respectively, the associated application for .zip files.  This
// seemed simple enough.  There were two problems:
//
// 1. the script that ran during install, runs AFTER the registry has been updated
//    with the DotNetZip file association.  So it was trying to preserve "DotNetZip" as
//    the associated app.  Not useful.
//
// 2. The script that runs during uninstall, runs AFTER the DotNetZip  registry values 
//    have been deleted. So it had lost its state. 
//
// This script re-orders the custom actions so they run at the proper times.
//
// There's actually one more twist.  We cannot simply run the "Restore" script (on
// uninstall) after the registry entries for DotNetZip have been deleted.  This
// again would destroy its state.  So, we doctor the MSI to run the restore script
// twice.  In phase 1, it transfers the state to a "parking lot" in the registry.
// Then the DotNetZip Keys and Values get deleted, but the parking lot remains. In
// phase 2, the restore script retrieves the statre from the parking lot, restores
// the file association, and then deletes the parking lot,
//
// So it's a little more complicated than I originally thought but it works. 
//
// Mon, 31 Aug 2009  12:18
// 
// ==================================================================


function QueryAndEval(query, stmt)
{
    view = database.OpenView(query);
    view.Execute();
    if (stmt != null)
    {
        record = view.Fetch();
        var result = null;
        if (record!=null)
            var result = eval(stmt);
    }
    view.Close();
    return result;
}


function UpdateBannerText(formName, text)
{
    sql = "UPDATE `Control` SET `Control`.`Text` = '" + text + "' "  +
        "WHERE `Control`.`Dialog_`='" + formName + "' AND `Control`.`Control`='BannerText'"
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
}

// Constant values from Windows Installer
var msiOpenDatabaseModeTransact = 1;

var msiViewModifyInsert         = 1;
var msiViewModifyUpdate         = 2;
var msiViewModifyAssign         = 3;
var msiViewModifyReplace        = 4;
var msiViewModifyDelete         = 6;


if (WScript.Arguments.Length != 1)
{
    WScript.StdErr.WriteLine(WScript.ScriptName + ": Updates an MSI to move the custom action in sequence");
    WScript.StdErr.WriteLine("Usage: ");
    WScript.StdErr.WriteLine("  " + WScript.ScriptName + " <file>");
    WScript.Quit(1);
}

var filespec = WScript.Arguments(0);
WScript.Echo(WScript.ScriptName + " " + filespec);
var WshShell = new ActiveXObject("WScript.Shell");

var database = null;
try
{
    var installer = new ActiveXObject("WindowsInstaller.Installer");
    database = installer.OpenDatabase(filespec, msiOpenDatabaseModeTransact);
    // this will fail if Orca.exe has the same MSI already opened
}
catch (e1)
{
    WScript.Echo("Error: " + e1.message);
    for (var x in e1)
        WScript.Echo("e[" + x + "] = " + e1[x]);
}

if (database==null) 
{
    WScript.Quit(1);
}

var sql;
var view;
var record;

try
{
    // First, get the script that stores the state.
    // We need to make sure this runs before registry values are updated.
    //
    // Do the query looking for a particular Target.  The query will retrieve the
    // .SetProperty action on the Store Javascript, which precedes the execution
    // of the Javascript.  The "Store" is just a marker added to the Custom Action
    // in the VS Designer, to enable this script to work.

    WScript.Echo("Updating sequence numbers for the Store script...");

    sql = "SELECT `Action`, `Type`, `Target` FROM `CustomAction` Where Target='[TARGETDIR]|[ASSOCIATEZIPEXTENSION]|Store'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var setPropId = record.StringData(1);
    view.Close();

    var parts = setPropId.split(".");
    var execScriptId = parts[0];

    // this gets the sequence when the registry values are updated
    sql = "SELECT `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='WriteRegistryValues'";
    var sequence1 = QueryAndEval(sql, "record.IntegerData(1)");
    //WScript.Echo("sequence2 = " + sequence2);

    // First set the sequence number of the .SetProperty action.
    // It must precede the writing of registry values. 
    sql = "SELECT `Action`, `Condition`, `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='" + setPropId + "'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    record.IntegerData(3) = sequence1-3;
    view.Modify(msiViewModifyReplace, record);
    view.Close();

    // Now set the sequence number of the script itself.  
    // It must immediately follow .SetProperty in sequence.
    sql = "SELECT `Action`, `Condition`, `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='" + execScriptId + "'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    record.IntegerData(3) = sequence1-2;
    view.Modify(msiViewModifyReplace, record);
    view.Close();



    // Ok, now find the CustomAction with the Target = "Reset".  
    // This is like the above.
    WScript.Echo("Finding the Reset script...");

    sql = "SELECT `Action`, `Type`, `Target` FROM `CustomAction` Where Target='Reset'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    setPropId = record.StringData(1);
    var setPropType = record.IntegerData(2); 
    view.Close();

    parts = setPropId.split(".");
    //WScript.Echo("id = " + parts[0]);
    execScriptId = parts[0];

    sql = "SELECT `Source`, `Type`  FROM `CustomAction` Where Action='" + execScriptId + "'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var scriptSource = record.StringData(1);
    var scriptType = record.IntegerData(2); 
    view.Close();


    sql = "SELECT `Condition`, `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='" + setPropId + "'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var setPropCondition = record.StringData(1);
    var setPropSequence = record.StringData(2); 
    view.Close();
    //WScript.Echo("setPropSequence = " + setPropSequence);

    sql = "SELECT `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='RemoveRegistryValues'";
    var sequence2 = QueryAndEval(sql, "record.IntegerData(1)");
    //WScript.Echo("sequence2 = " + sequence2);
 
    var phaseTwoSequence = sequence2 + 20;


    // don't update twice
    var execScriptId2 = execScriptId + "-xx";
    sql = "SELECT `Sequence` FROM `InstallExecuteSequence` WHERE `Action`='" + execScriptId2 + "'";
    var exists = QueryAndEval(sql, "record.IntegerData(1)");
    //WScript.Echo("exists = " + exists);
 
    if (exists == null)
    {
        // Create new records in the InstallExecuteSequence table to call the custom action again. 
        // The Action must be unique, so we need to use copies.
        WScript.Echo("Inserting new records in the InstallExecuteSequence table...");
        sql = "INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) VALUES ('" + 
            execScriptId2 + "', '" + setPropCondition + "', " + phaseTwoSequence + ")";
        //WScript.Echo("insert1: " + sql);
        view = database.OpenView(sql);
        view.Execute();
        view.Close();

        phaseTwoSequence--;

        var setPropId2 = execScriptId2 + ".SetProperty";
        sql = "INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) VALUES ('" + 
            setPropId2 + "', '" + setPropCondition + "', " + phaseTwoSequence + ")";
        //WScript.Echo("insert2: " + sql);
        view = database.OpenView(sql);
        view.Execute();
        view.Close();

        // Now, "clone" the records in the CustomAction table.
        WScript.Echo("Cloning records in the CustomAction table...");
        sql = "INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('" + 
            execScriptId2 + "', '" + scriptType + "', '" + scriptSource + "', '')";
        //WScript.Echo("insert3: " + sql);
        view = database.OpenView(sql);
        view.Execute();
        view.Close();

        sql = "INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('" + 
            setPropId2 + "', '" + setPropType + "', '" + execScriptId2 + "', 'Reset-phase2')";
        //WScript.Echo("insert4: " + sql);
        view = database.OpenView(sql);
        view.Execute();
        view.Close();

        // The result is that the "Reset" action is run twice:  once before the registry values
        // are deleted, and once after.  It behaves differently depending on when it is run. 
    }


    // Last thing, aesthetics really.
    WScript.Echo("Updating the BannerText on the wizard...");

    // A. fix the title of the install Wizard. 
    UpdateBannerText("WelcomeForm", "{\\VSI_MS_Sans_Serif16.0_1_0}Welcome to the Setup for\r\n[ProductName]");

    // B. make the confirm installation text nicer
    UpdateBannerText("ConfirmInstallForm", "{\\VSI_MS_Sans_Serif16.0_1_0}Ready to install...");


    // C. make the user exit text nicer
    UpdateBannerText("UserExitForm", "{\\VSI_MS_Sans_Serif16.0_1_0}You interrupted the installation...");

    // D. EULA
    UpdateBannerText("EulaForm", "{\\VSI_MS_Sans_Serif16.0_1_0}Here`s the DotNetZip License.\r\nYou have to accept it to proceed.");

    // E. Select Folder
    UpdateBannerText("FolderForm", "{\\VSI_MS_Sans_Serif16.0_1_0}Select the Installation Folder...");

    // F. Do you want to associate zip files to DotNetZip?
    UpdateBannerText("CustomCheckA", "{\\VSI_MS_Sans_Serif16.0_1_0}Do you want to associate zip files\r\nto DotNetZip?");

    // G. Complete  
    UpdateBannerText("FinishedForm", "{\\VSI_MS_Sans_Serif16.0_1_0}You have installed DotNetZip.");


    database.Commit();
    WScript.Echo("done.");
}
catch(e)
{
    WScript.StdErr.WriteLine("Exception");
    for (var x in e)
        WScript.StdErr.WriteLine("e[" + x + "] = " + e[x]);
    WScript.Quit(1);
}


