// PostProcessMSi.js
//
// Post-process the generated MSI to move the (optional) checkbox
// control that is presented on the exit dialog.  WIX produces a
// checkbox control with a greay background; I don't know how to reset
// that, but I do know how to move it.  Moving it to the lower part of
// the form - where the Finish button is presented, allows the grey to
// match the existing background.
//
//
// Copyright (c) 2011 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code, like all of DotNetZip, is licensed under the Microsoft
// Public License.  See the file License.txt accompanying this source
// modeule for the license details.  More info on:
// http://dotnetzip.codeplex.com
//
// Created: Thu, 14 Jul 2011  17:31
// Last saved: <2011-July-16 17:48:25>
//

// Constant values from Windows Installer
var msiOpenDatabaseMode = {
    Transact : 1
};
var msiViewModify = {
    Insert  : 1,
    Update  : 2,
    Assign  : 3,
    Replace : 4,
    Delete  : 6
};


if (WScript.Arguments.Length != 1){
    WScript.StdErr.WriteLine(WScript.ScriptName + " file");
    WScript.Quit(1);
}


var filespec = WScript.Arguments(0);
WScript.Echo(WScript.ScriptName + " " + filespec);
var installer = WScript.CreateObject("WindowsInstaller.Installer");
var database = installer.OpenDatabase(filespec, msiOpenDatabaseMode.Transact);

var sql;
var view;
var record;

try {
    // var fileId = FindFileIdentifier(database, filename);
    // if (!fileId){
    //     throw new Error ("Unable to find '" + filename + "' in File table");
    // }

    WScript.Echo("Updating the Control table...");

    // Move the checkbox on the exit dialog, so that it appears in the row of buttons.
    // We do this because the background on the checkbox is gray, unchangeably so.
    // but the bg for the row of buttons is gray, so it looks ok if moved there.
    sql = "SELECT `Dialog_`, `Control`, `Type`, `X`, `Y`, `Width`, `Height`  FROM `Control` WHERE `Dialog_`='ExitDialog' AND `Control`='OptionalCheckBox'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    // index starts at 1
    record.IntegerData(4) = 14;  // X
    record.IntegerData(5) = 243; // Y
    record.IntegerData(6) = 120; // Width
    record.IntegerData(7) = 16;  // Height
    view.Modify(msiViewModify.Replace, record);
    view.Close();

    // Insert a new checkbox into the InstallDirDlg.
    // This one controls whether to associate zip files to DotNetZip.

    sql = "SELECT `Control` FROM `Control` WHERE `Control`='CheckboxAssoc'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var controlExists = null;
    if (record != null) controlExists = record.StringData(1);
    if (record == null || controlExists != "CheckboxAssoc") {

        sql = "INSERT INTO `Control` (`Dialog_`, `Control`, `Type`, `X`, `Y`, " +
            "`Width`, `Height`, `Attributes`, `Property`, `Text`, `Control_Next`, `Help`) " +
            "VALUES ('InstallDirDlg', 'CheckboxAssoc', 'CheckBox', '20', '124', '184', '14', '3', "+
            "'WANT_ZIP_ASSOCIATIONS', 'Associate .zip files to DotNetZip', 'Next', '|')";
            view = database.OpenView(sql);
            view.Execute();
            view.Close();

            sql = "UPDATE `Control` SET `Control`.`Control_Next` = 'CheckboxAssoc' " +
                "WHERE `Control`.`Dialog_`='InstallDirDlg' AND `Control`.`Control`='ChangeFolder'";
            view = database.OpenView(sql);
            view.Execute();
            view.Close();
    }

    // Tweak the existing controls on the InstallDirDlg: move them up a bit, shrink the label
    sql = "UPDATE `Control` SET `Control`.`Y` = 76 " +
        "WHERE `Control`.`Dialog_`='InstallDirDlg' AND `Control`.`Control`='Folder'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 96 " +
        "WHERE `Control`.`Dialog_`='InstallDirDlg' AND `Control`.`Control`='ChangeFolder'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Height` = 16 " +
        "WHERE `Control`.`Dialog_`='InstallDirDlg' AND `Control`.`Control`='FolderLabel'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();


    // Tweak the existing controls on the ProgressDlg: move the status
    // text up a bit, and make it taller, because it was being clipped
    // by its own height, and also by the proximity of the progress bar.
    // These seem like basic fit-and-finish problems that Wix shouldn't have.
    // WIX is an idiot.
    sql = "UPDATE `Control` SET `Control`.`Y` = 96, `Control`.`Height` = 14 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='StatusLabel'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 96, `Control`.`Height` = 14 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='ActionText'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 58 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='TextInstalling'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 58 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='TextChanging'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 58 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='TextRepairing'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Y` = 58 " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='TextRemoving'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();

    // if (checkboxChecked) {
    //     WScript.Echo("Updating the Property table...");
    //     // Set the default value of the CheckBox
    //     sql = "INSERT INTO `Property` (`Property`, `Value`) VALUES ('LAUNCHAPP', '1')";
    //     view = database.OpenView(sql);
    //     view.Execute();
    //     view.Close();
    // }
    // WScript.Echo("G");

    database.Commit();
}
catch(e) {
    WScript.StdErr.WriteLine(e);
    WScript.Quit(1);
}



