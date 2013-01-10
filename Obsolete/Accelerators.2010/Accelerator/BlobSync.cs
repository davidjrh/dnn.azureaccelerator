using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Accelerator;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.Diagnostics;

namespace smarx.BlobSync
{
    /// <summary>
    /// Provides data for updating a file event.
    /// </summary>
    public class UpdatingFileEventArgs : CancelEventArgs
    {
        public CloudBlob Blob;
        public String    LocalPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingFileEventArgs"/> class.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        /// <param name="localPath">The local path.</param>
        public UpdatingFileEventArgs(CloudBlob blob, String localPath)
        {
            Blob = blob;
            LocalPath = localPath;
        }
    }

    /// <summary>
    /// Delegate for handling the file update event.
    /// </summary>
    public delegate void UpdatingFileHandler(Object sender, UpdatingFileEventArgs args);

    public enum SyncDirection
    {
        Download,
        Upload
    }

    /// <summary>
    /// Performs synchronization between blob and local file system storage.
    /// </summary>
    public class BlobSync
    {
        public event UpdatingFileHandler UpdatingFile;

#region | FIELDS

        private readonly Dictionary<String, String> _localSyncList = new Dictionary<String, String>();
        private Thread _blobSyncThread;
        private CloudBlobDirectory _blobDirectory;
        private String _blobDirectoryUri;
        
#endregion

        public CloudBlobDirectory BlobSyncDirectory
        {
            get { return _blobDirectory; }
            set
            {
                _blobDirectory = value;
                _blobDirectoryUri = value.Uri.ToString(); //i| refactor so we do this only when the sync blob root changes and not at each file compare.
            }
        }

        public Boolean IsStarted { get { return _blobSyncThread != null && _blobSyncThread.ThreadState != System.Threading.ThreadState.Running; } }
        public String LocalSyncRoot { get; set; }
        public Boolean IgnoreAdditionalFiles { get; set; }
        public SyncDirection SyncDirection { get; set; }

        public Dictionary<String, String> LocalSyncList { get { return _localSyncList; } }
        public String BlobSyncDirectoryUri { get { return _blobDirectoryUri; } }
        public CloudBlobContainer Container { get { return BlobSyncDirectory.Container; } }
       
#region | CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobSync"/> class.
        /// </summary>
        public BlobSync()
        {
           
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobSync"/> class.
        /// </summary>
        /// <param name="blobDirectory">The root cloud blob directory.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="ignoreExisting">if set to <c>true</c> [ignore existing].</param>
        public BlobSync(CloudBlobDirectory blobDirectory, String localPath, SyncDirection direction, Boolean ignoreExisting)
        {
            BlobSyncDirectory = blobDirectory;
            LocalSyncRoot = localPath;
            SyncDirection = SyncDirection;
            IgnoreAdditionalFiles = ignoreExisting;
            SyncDirection = direction;
        }

#endregion
#region | EVENTS

        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <returns></returns>
        public Boolean Validate()
        {
            LogLevel.Information.TraceContent("BlobSync", "Sync Validation...", this.ToTraceString());
            if ( BlobSyncDirectory == null || String.IsNullOrEmpty(LocalSyncRoot) )
            {
                LogLevel.Error.Trace("BlobSync", "Sync cannot continue: Container or local path not set.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Start the thread to sync blobs at intervals until Stop() is called.
        /// </summary>
        public void Start(TimeSpan interval)
        {
            _blobSyncThread = new Thread(() =>
                                           {
                                               while ( true )
                                               {
                                                   try
                                                   {
                                                       SyncAll();
                                                   }
                                                   catch (Exception ex)
                                                   {
                                                       LogLevel.Error.TraceException("BlobSync", ex, "An exception occurred when performing blob to local storage synchroniztion");
                                                   }
                                                   Thread.Sleep(interval);
                                               }
                                           });
            _blobSyncThread.Start();
        }

        /// <summary>
        /// Stops the blob sync, terminating any in-progress transfers.
        /// </summary>
        public void Stop()
        {
            lock(_blobSyncThread)
            {
                if (IsStarted && _blobSyncThread != null && _blobSyncThread.ThreadState == System.Threading.ThreadState.Running)
                    _blobSyncThread.Abort();
            }
        }

        /// <summary>
        /// Performs the synchronization of file system and blob storage.
        /// </summary>
        public Boolean SyncAll()
        {
            if (!Validate())
                return false;

            //i| Get all of the blob in container.
            //BlobRequestOptions blobRequestOptions = new BlobRequestOptions
            //                                            {
            //                                                UseFlatBlobListing = true,
            //                                                BlobListingDetails = BlobListingDetails.Metadata
            //                                            };
            //List<IListBlobItem> list = Container.ListBlobs(blobRequestOptions).ToList();
            Dictionary<String,CloudBlob> cloudBlobs = BlobSyncDirectory.ListBlobs(new BlobRequestOptions
                                                                                {
                                                                                    UseFlatBlobListing = true,
                                                                                    BlobListingDetails = BlobListingDetails.Metadata
                                                                                }).OfType<CloudBlob>().ToDictionary(b => b.Uri.ToString(), b => b);

            if ( !IgnoreAdditionalFiles )
            {
                var cloudBlobNames = new HashSet<String>(cloudBlobs.Keys); //Select(b => b.Uri.ToString()));
                var localBlobNames = new HashSet<String>(LocalSyncList.Keys); 

                if (SyncDirection == SyncDirection.Download)
                {
                    localBlobNames.ExceptWith(cloudBlobNames); 
                    foreach (var uri in localBlobNames)
                    {
                        //i|
                        //i| Delete all local files without corresponding blob files.
                        //i|
                        String localPath = GetLocalPath(uri);
                        LogLevel.Information.Trace("BlobSync", "FileDelete : Deleting Local File : {{ [Filename: '{0}'] }}.", localPath);
                        File.Delete(localPath);
                        LocalSyncList.Remove(uri);
                    }
                }
                else if (SyncDirection == SyncDirection.Upload)
                {
                    cloudBlobNames.ExceptWith(localBlobNames);
                    foreach ( var uri in cloudBlobNames )
                    {
                        //i|
                        //i| Delete all azure storage blogs without corresponding local files.
                        //i|
                        CloudBlob blob = cloudBlobs[uri];
                        LogLevel.Information.Trace("BlobSync", "FileDelete : Deleting Storage Blob : {{ [BlobUri: '{0}'] }}.", uri);
                        blob.DeleteIfExists();
                        cloudBlobs.Remove(uri);
                    }
                }
            }

            if (SyncDirection == SyncDirection.Download)
                foreach ( var kvp in cloudBlobs )
                {
                    var blobUri = kvp.Key;
                    var blob = kvp.Value;
                    if ( !LocalSyncList.ContainsKey(blobUri) || LocalSyncList[blobUri] != blob.Attributes.Properties.ETag)
                    {
                        var localPath      = GetLocalPath(blobUri);
                        var localDirectory = Path.GetDirectoryName(localPath);
                        var args           = new UpdatingFileEventArgs(blob, localPath);
                        LogLevel.Information.Trace("BlobSync", "Local File Update : {{ [LocalSyncRoot: '{0}'], [SourceBlobUri: '{1}'] }}.", localPath, blobUri);
                        if ( UpdatingFile != null )
                        {
                            UpdatingFile(this, args);
                        }
                        if ( !args.Cancel )
                        {
                            Directory.CreateDirectory(localDirectory);
                            blob.DownloadToFile(localPath);
                        }
                        LocalSyncList[blobUri] = blob.Properties.ETag;
                    }
                }
            else if (SyncDirection == SyncDirection.Upload)
            {
                var syncList = new Dictionary<String, FileInfo>().SyncListFromDirectory(new DirectoryInfo(LocalSyncRoot), BlobSyncDirectoryUri);
                var toSyncList = (from      sl in syncList
                                  where     !cloudBlobs.ContainsKey(sl.Key) || cloudBlobs[sl.Key].Properties.Length != sl.Value.Length
                                  select    sl
                                 );
                foreach (var kvp in toSyncList)
                {
                    LogLevel.Information.Trace("BlobSync", "Updating Blob : {{ [SourceFile]: '{0}', [BlobUri]: '{1}' }}.", kvp.Value.FullName, kvp.Key);
                    CloudBlob blob = Container.GetBlobReference(kvp.Key);
                    blob.UploadFile(kvp.Value.FullName);
                }
            }
            return true;
        }

#endregion
#region | UTILITY METHODS
         
        /// <summary>
        /// Gets the local path.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private String GetLocalPath(String uri)
        {
            return Path.Combine(LocalSyncRoot, uri.Substring(BlobSyncDirectoryUri.Length).ReplacePathChar('\\'));
            
            //// skip domain and container
            //// note that this would almost certainly break running against dev storage (where paths look different)
            //foreach (var segment in new Uri(uri).Segments.Skip(2))
            //{
            //    path = Path.Combine(path, segment);
            //}
            //return path;
        }

#endregion

    }

    public static class SyncExtensions
    {
        public static Dictionary<String,FileInfo> SyncListFromDirectory(this Dictionary<String,FileInfo> syncList, DirectoryInfo directory, String syncUri)
        {
            foreach (var subdir in directory.GetDirectories())
            {
                String suburi = String.Format("{0}/{1}", syncUri, subdir.Name);
                syncList.SyncListFromDirectory(subdir, suburi);
            }
            foreach (var file in directory.GetFiles())
            {
                String fileuri = String.Format("{0}/{1}", syncUri, file.Name);
                syncList[fileuri] = file;
            }
            return syncList;
        }
    }
}