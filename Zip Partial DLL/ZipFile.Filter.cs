// ZipFile.Filter.cs
//
// Copyright (c) 2009 Microsoft Corporation.  All rights reserved.
//
// This module defines methods in the ZipFile class associated to the FileFilter capability -
// selecting files to add into the archive, or selecting entries to retrieve from the archive
// based on criteria including the filename, size, date, or attributes. 
//
// These methods are segregated into a different module to facilitate easy exclusion for those
// people who wish to have a smaller library without this function. 
// 
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//

using System;


namespace Ionic.Zip
{

    partial class ZipFile
    {
        /// <summary>
        /// Retrieve entries from the zipfile by specified criteria.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        ///
        /// <para>
        /// Specify the criteria in statements of 3 elements: a noun, an operator, and a value.
        /// Consider the string "name != *.doc" .  The noun is "name".  The operator is "!=",
        /// implying "Not Equal".  The value is "*.doc".  That criterion, in English, says "all
        /// files with a name that does not end in the .doc extension."
        /// </para> 
        ///
        /// <para>
        /// Supported nouns include "name" for the filename; "atime", "mtime", and "ctime" for
        /// last access time, last modfied time, and created time of the file, respectively;
        /// "attributes" for the file attributes; and "size" for the file length (uncompressed).
        /// The "attributes" and "name" nouns both support = and != as operators.  The "size",
        /// "atime", "mtime", and "ctime" nouns support = and !=, and &gt;, &gt;=, &lt;, &lt;=
        /// as well.
        /// </para> 
        ///
        /// <para>
        /// Specify values for the file attributes as a string with one or more of the
        /// characters H,R,S,A in any order, implying Hidden, ReadOnly, System, and Archive,
        /// respectively.  To specify a time, use YYYY-MM-DD-HH:mm:ss as the format.  If you
        /// omit the HH:mm:ss portion, it is assumed to be 00:00:00 (midnight). The value for a
        /// size criterion is expressed in integer quantities of bytes, kilobytes (use k or kb
        /// after the number), megabytes (m or mb), or gigabytes (g or gb).  The value for a
        /// name is a pattern to match against the filename, potentially including wildcards.
        /// The pattern follows CMD.exe glob rules: * implies one or more of any character,
        /// while ? implies one character.  Currently you cannot specify a pattern that includes
        /// spaces.
        /// </para> 
        ///
        /// <para>
        /// Some examples: a string like "attributes != H" retrieves all entries whose
        /// attributes do not include the Hidden bit.  A string like "mtime > 2009-01-01"
        /// retrieves all entries with a last modified time after January 1st, 2009.  For
        /// example "size &gt; 2gb" retrieves all entries whose uncompressed size is greater
        /// than 2gb.
        /// </para> 
        ///
        /// <para>
        /// You can combine criteria with the conjunctions AND or OR. Using a string like "name
        /// = *.txt AND size &gt;= 100k" for the selectionCriteria retrieves entries whose names
        /// end in  .txt, and whose uncompressed size is greater than or equal to
        /// 100 kilobytes.
        /// </para>
        ///
        /// <para>
        /// For more complex combinations of criteria, you can use parenthesis to group clauses
        /// in the boolean logic.  Absent parenthesis, the precedence of the criterion atoms is
        /// determined by order of appearance.  Unlike the C# language, the AND conjunction does
        /// not take precendence over the logical OR.  This is important only in strings that
        /// contain 3 or more criterion atoms.  In other words, "name = *.txt and size &gt; 1000
        /// or attributes = H" implies "((name = *.txt AND size &gt; 1000) OR attributes = H)"
        /// while "attributes = H OR name = *.txt and size &gt; 1000" evaluates to "((attributes
        /// = H OR name = *.txt) AND size &gt; 1000)".  When in doubt, use parenthesis.
        /// </para>
        ///
        /// <para>
        /// Using time properties requires some extra care. If you want to retrieve all entries
        /// that were last updated on 2009 February 14, specify "mtime &gt;= 2009-02-14 AND
        /// mtime &lt; 2009-02-15".  Read this to say: all files updated after 12:00am on
        /// February 14th, until 12:00am on February 15th.  You can use the same bracketing
        /// approach to specify any time period - a year, a month, a week, and so on.
        /// </para>
        ///
        /// <para>
        /// The syntax allows one special case: if you provide a string with no spaces, it is treated as
        /// a pattern to match for the filename.  Therefore a string like "*.xls" will be equivalent to 
        /// specifying "name = *.xls".  
        /// </para>
        /// 
        /// <para>
        /// The ExclusionCriteria can be used to exclude files from the retrieved set, in the same way.
        /// </para>
        ///
        /// <para>
        /// There is no logic in this method that insures that the file inclusion criteria
        /// are internally consistent.  For example, it's possible to specify criteria that
        /// says the file must have a size of less than 100 bytes, as well as a size that
        /// is greater than 1000 bytes.  Obviously no file will ever satisfy such criteria,
        /// but this method does not detect such inconsistencies. The caller is responsible 
        /// for insuring the criteria are sensible.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from
        /// storage.  When creating a new ZipFile, this method will work only after the
        /// ZipArchive has been Saved to the disk (the ZipFile class subsequently and
        /// implicitly reads the Zip archive from storage.)  Calling SelectEntries on a
        /// ZipFile that has not yet been saved will deliver undefined results.
	/// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     var txtFiles = zip1.SelectEntries("name = *.txt");
        ///     foreach (ZipEntry e in txtFiles)
        ///     {
        ///         e.Extract();
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <returns>a collection of ZipEntry objects that conform to the inclusion spec</returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<ZipEntry> SelectEntries(String selectionCriteria)
        {
            return SelectEntries(selectionCriteria, null);
        }

        /// <summary>
        /// Retrieve entries from the zipfile by specified inclusion and exclusion criteria.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified inclusion and exclusion criteria.  The caller retrieves all
        /// files that fit the inclusion criteria, and do not fit the exclusion criteria. The
        /// criteria are described in a string format, and can include patterns for the
        /// filename; constraints on the size of the entry; constraints on the last modified,
        /// created, or last accessed time for the file described by the entry; or the
        /// attributes of the entry.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="SelectEntries(String)" />.
        /// </para> 
        ///
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from
        /// storage.  When creating a new ZipFile, this method will work only after the
        /// ZipArchive has been Saved to the disk (the ZipFile class subsequently and
        /// implicitly reads the Zip archive from storage.)  Calling SelectEntries on a
        /// ZipFile that has not yet been saved will deliver undefined results.
	/// </para>
	///
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// The following example retrieves entries from the zip file that have filenames ending in .txt
        /// but excludes from that list, any file with a size smaller than 100 bytes. 
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     var txtFiles = zip1.SelectEntries("name = *.txt", "size &lt; 100");
        ///     foreach (ZipEntry e in txtFiles)
        ///     {
        ///         e.Extract();
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// <param name="selectionCriteria">the selection criteria</param>
        ///
        /// <param name="exclusionCriteria">
        /// The criteria for exclusion.  Actually, the exclusionCriteria is
        /// redundant. Any criteria specified in the exclusionCriteria could also be specified in
        /// the selectionCriteria, just by logically negating the criteria.  In other words, a
        /// selectionCriteria of "size &gt; 50000" coupled with an exclusionCriteria of "name =
        /// *.txt" is equivalent to a selectionCriteria of "size &gt; 50000 AND name != *.txt"
        /// with no exclusionCriteria.  Despite this, this method is provided to allow for
        /// clarity in the interface for those cases where it makes sense to clearly delineate
        /// the exclusion criteria in the application code.
        /// </param>
        ///
        /// <returns>a collection of ZipEntry objects that conform to criteria</returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<ZipEntry> SelectEntries(String selectionCriteria, String exclusionCriteria)
        {
            Ionic.FileFilter ff = new Ionic.FileFilter(selectionCriteria, exclusionCriteria);
            return ff.SelectEntries(this);
        }


	/// <summary>
	/// Selects and Extracts a set of Entries from the ZipFile.
	/// </summary>
	///
        /// <remarks>
	/// <para>
	/// The entries are extracted into the current working directory. If 
	/// any of the files already exist, an exception will be thrown.
	/// </para>
        /// </remarks>
        ///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
	public void ExtractSelected(String selectionCriteria)
	{
	    foreach (ZipEntry e in SelectEntries(String selectionCriteria))
	    {
		e.Password = _Password; // possibly null
		e.Extract();
	    }
	}


	/// <summary>
	/// Selects and Extracts a set of Entries from the ZipFile.
	/// </summary>
	///
        /// <remarks>
	/// <para>
	/// The entries are extracted into the current working directory. If 
	/// any of the files already exist, and wantOverwrite is false, an exception will be thrown.
	/// </para>
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="SelectEntries(String)" />.
        /// </para> 
        /// </remarks>
	///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="wantOverwrite">True if the caller wants to overwrite any existing files 
	/// by the given name. </param>
	public void ExtractSelected(String selectionCriteria, bool wantOverwrite)
	{
	    foreach (ZipEntry e in SelectEntries(String selectionCriteria))
	    {
		e.Password = _Password; // possibly null
		e.Extract(wantOverwrite);
	    }
	}



	/// <summary>
	/// Selects and Extracts a set of Entries from the ZipFile.
	/// </summary>
	///
        /// <remarks>
	/// <para>
	/// The entries are extracted into the specified directory. If 
	/// any files already exist, an exception will be thrown.
	/// </para>
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="SelectEntries(String)" />.
        /// </para> 
        /// </remarks>
	///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="directoryName">the directory into which to extract. It will be created 
	/// if it does not exist.</param>
	public void ExtractSelected(String selectionCriteria, string directoryName)
	{
	    foreach (ZipEntry e in SelectEntries(String selectionCriteria))
	    {
		e.Password = _Password; // possibly null
		e.Extract(directoryName);
	    }
	}

	/// <summary>
	/// Selects and Extracts a set of Entries from the ZipFile.
	/// </summary>
	///
        /// <remarks>
	/// <para>
	/// The entries are extracted into the specified directory. If 
	/// any of the files already exist, and wantOVerwrite is false, an exception will be thrown.
	/// </para>
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="SelectEntries(String)" />.
        /// </para> 
        /// </remarks>
	///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="directoryName">the directory into which to extract. It will be created 
	/// if it does not exist.</param>
        /// <param name="wantOverwrite">True if the caller wants to overwrite any existing files 
	/// by the given name. </param>
	public void ExtractSelected(String selectionCriteria, string directoryName, bool wantOverwrite)
	{
	    foreach (ZipEntry e in SelectEntries(String selectionCriteria))
	    {
		e.Password = _Password; // possibly null
		e.Extract(directoryName, wantOverwrite);
	    }
	}


        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the disk matching the specified criteria,
        /// starting at the specified disk directory, and adds them to the ZipFile.  For details
        /// on the syntax for the file selection criteria, see <see cref="SelectEntries(String)"/>.
        /// </remarks>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>

        public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk)
        {
            this.AddSelectedFiles(selectionCriteria, null, directoryOnDisk, null);
        }


        /// <summary>
        /// Adds to the ZipFile a selection of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the disk, starting at the specified disk directory,
        /// that match the specified inclusion criteria, and do not match the exclusion
        /// criteria, and adds those files to the ZipFile using the specified base directory in
        /// the archive.  For details on the syntax for the file selection criteria, see <see
        /// cref="SelectEntries(String)" />.
        /// </remarks>
        /// 
        /// <param name="selectionCriteria">The criteria for inclusion</param>
        /// <param name="exclusionCriteria">The criteria for exclusion</param>
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        public void AddSelectedFiles(String selectionCriteria, String exclusionCriteria, String directoryOnDisk, String directoryPathInArchive)
        {
            Ionic.FileFilter ff = new Ionic.FileFilter(selectionCriteria, exclusionCriteria);
            String[] filesToAdd = ff.SelectFiles(directoryOnDisk);
            this.AddFiles(filesToAdd, directoryPathInArchive);
        }

    }

}



namespace Ionic
{
    internal abstract partial class FileCriterion
    {
        internal abstract bool Evaluate(Ionic.Zip.ZipEntry entry);
    }


    internal partial class NameCriterion : FileCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            return _Evaluate(entry.FileName);
        }
    }


    internal partial class SizeCriterion : FileCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            return _Evaluate(entry.UncompressedSize);
        }
    }

    internal partial class TimeCriterion : FileCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            DateTime x;
            switch (Which)
            {
                case WhichTime.atime:
                    x = entry.Atime;
                    break;
                case WhichTime.mtime:
                    x = entry.Mtime;
                    break;
                case WhichTime.ctime:
                    x = entry.Ctime;
                    break;
                default:
                    throw new ArgumentException("Constraint");
            }
            return _Evaluate(x);
        }
    }


    internal partial class AttributesCriterion : FileCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            System.IO.FileAttributes fileAttrs = entry.Attributes;
            return _Evaluate(fileAttrs);
        }
    }


    internal partial class CompoundCriterion : FileCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            bool result = Left.Evaluate(entry);
            switch (Conjunction)
            {
                case LogicalConjunction.AND:
                    if (result)
                        result = Right.Evaluate(entry);
                    break;
                case LogicalConjunction.OR:
                    if (!result)
                        result = Right.Evaluate(entry);
                    break;
                default:
                    throw new ArgumentException("Conjunction");
            }
            return result;
        }
    }



    public partial class FileFilter
    {
        private bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            bool result = Include.Evaluate(entry);
            if (Exclude != null)
                result = result && !Exclude.Evaluate(entry);
            return result;
        }

        /// <summary>
        /// Retrieve the ZipEntry items in the ZipFile that conform to the specified criteria.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// This method applies the criteria set in the FileFilter instance (as described in
        /// the <see cref="FileFilter.SelectionCriteria"/>) to the specified ZipFile.  Using this
        /// method, for example, you can retrieve all entries from the given ZipFile that
        /// have filenames ending in .txt.
        /// </para>
        ///
        /// <para>
        /// Normally, applications would not call this method directly.  This method is used 
	/// by the ZipFile class.
        /// </para>
        ///
        /// <para>
        /// You can also retrieve entries based on size, time, and attributes. See <see
        /// cref="FileFilter.SelectionCriteria"/> for a description of the syntax of the
        /// SelectionCriteria string.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zip">The ZipFile from which to retrieve entries.</param>
        ///
        /// <returns>a ReadOnly collection of ZipEntry objects that conform to the criteria.</returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<Ionic.Zip.ZipEntry> SelectEntries(Ionic.Zip.ZipFile zip)
        {
            var list = new System.Collections.Generic.List<Ionic.Zip.ZipEntry>();

            foreach (Ionic.Zip.ZipEntry e in zip)
            {
                if (this.Evaluate(e))
                    list.Add(e);
            }

            return list.AsReadOnly();
        }
    }
}
