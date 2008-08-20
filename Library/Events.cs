using System;
using System.Collections.Generic;
using System.Text;

namespace Ionic.Utils.Zip
{

    /// <summary>
    /// Provides information about the progress of a save or extract operation.
    /// </summary>
    public class ZipProgressEventArgs : EventArgs
    {
        private int _entriesTotal;
	private bool _cancel;
	private String _nameOfLatestEntry;


        internal ZipProgressEventArgs(int entriesTotal, string lastEntry) 
	{
            this._entriesTotal = entriesTotal;
            this._nameOfLatestEntry = lastEntry;
	}

        /// <summary>
        /// The total number of entries to be saved or extracted.
        /// </summary>
        public int EntriesTotal
        {
            get { return _entriesTotal; }
        }

        /// <summary>
        /// The name of the last entry saved or extracted.
        /// </summary>
        public string NameOfLatestEntry
        {
            get { return _nameOfLatestEntry; }
        }

        /// <summary>
        /// In an event handler, set this to cancel the save or extract 
	/// operation that is in progress.
        /// </summary>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = _cancel || value; }
        }
    }


    /// <summary>
    /// Provides information about the progress of a save operation.
    /// </summary>
    public class SaveProgressEventArgs : ZipProgressEventArgs
    {
        private int _entriesSaved;

        /// <summary>
        /// Constructor for the SaveProgressEventArgs.
        /// </summary>
        /// <param name="entriesTotal">The total number of entries in the zip archive.</param>
        /// <param name="entriesSaved">Number of entries that have been saved.</param>
        /// <param name="lastEntry">The last entry saved.</param>
        internal SaveProgressEventArgs(int entriesTotal, int entriesSaved, string lastEntry) 
	    : base(entriesTotal,lastEntry)
        {
            this._entriesSaved = entriesSaved;
        }

        /// <summary>
        /// Number of entries saved so far.
        /// </summary>
        public int EntriesSaved
        {
            get { return _entriesSaved; }
        }

    }


    /// <summary>
    /// Provides information about the progress of the extract operation.
    /// </summary>
    public class ExtractProgressEventArgs : ZipProgressEventArgs
    {
        private int _entriesExtracted;
	private bool _overwrite;
	private string _target;

        /// <summary>
        /// Constructor for the ExtractProgressEventArgs.
        /// </summary>
        /// <param name="entriesTotal">The total number of entries in the zip archive.</param>
        /// <param name="entriesExtracted">Number of entries that have been extracted.</param>
        /// <param name="lastEntry">The last entry extracted.</param>
        /// <param name="extractLocation">The location to which entries are extracted.</param>
        /// <param name="wantOverwrite">indicates whether the extract operation will overwrite existing files.</param>
        internal ExtractProgressEventArgs(int entriesTotal, int entriesExtracted, string lastEntry, string extractLocation, bool wantOverwrite)
	    : base(entriesTotal,lastEntry)

        {
            this._entriesExtracted = entriesExtracted;
	    this._overwrite= wantOverwrite;
	    this._target= extractLocation;
        }

        /// <summary>
        /// Number of entries extracted so far.
        /// </summary>
        public int EntriesExtracted
        {
            get { return _entriesExtracted; }
        }

        /// <summary>
        /// True if the extract operation overwrites existing files.
        /// </summary>
        public bool Overwrite
        {
            get { return _overwrite; }
        }

        /// <summary>
        /// Returns the extraction target location, a filesystem path. 
        /// </summary>
        public String ExtractLocation
        {
            get { return _target; }
        }


    }


    /// <summary>
    /// Used to provide event information about the Save .
    /// </summary>
    public class SaveEventArgs : EventArgs
    {
        private String _name;

        /// <summary>
        /// Constructor for a SaveEventArgs.
        /// </summary>
        /// <param name="archiveName">The name of the archive being saved.</param>
        internal SaveEventArgs(string archiveName)
        {
            _name = archiveName;
        }

        /// <summary>
        /// Returns the archive name.
        /// </summary>
        public String ArchiveName
        {
            get { return _name; }
        }
    }


    /// <summary>
    /// Used to provide event information about the Extract operation.
    /// </summary>
    public class ExtractEventArgs : EventArgs
    {
        private String _name;
        private String _target;
        private bool _overwrite;

        /// <summary>
        /// Constructor for a ExtractEventArgs.
        /// </summary>
        /// <param name="archiveName">The name of the archive being extracted.</param>
        /// <param name="extractLocation">The location to which  the archive is being extracted.</param>
        /// <param name="wantOverwrite">whether the extract operation overwrites existing files.</param>
        internal ExtractEventArgs(string archiveName, string extractLocation, bool wantOverwrite)
        {
            this._name = archiveName;
            this._target = extractLocation;
	    this._overwrite= wantOverwrite;
        }

        /// <summary>
        /// Returns the archive name.
        /// </summary>
        public String ArchiveName
        {
            get { return _name; }
        }

        /// <summary>
        /// Returns the extraction target location, or "(stream)" if the target is a stream.
        /// </summary>
        public String ExtractLocation
        {
            get { return _target; }
        }

        /// <summary>
        /// A boolean indicating whether the extract operation overwrites existing files.
        /// </summary>
        public bool Overwrite
        {
            get { return _overwrite; }
        }
    }

}
