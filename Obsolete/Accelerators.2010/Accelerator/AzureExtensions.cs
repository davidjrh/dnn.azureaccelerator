using System;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Extension methods on Windows Azure classes.
    /// </summary>
    public static class AzureExtensions
    {
        /// <summary>
        /// Test for the existence of a blob.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        /// <returns></returns>
        public static Boolean Exists(this CloudBlob blob)
        {
            try {
                blob.FetchAttributes();
                return true;
            } catch ( StorageClientException e ) {
                if ( e.ErrorCode == StorageErrorCode.ResourceNotFound )
                    return false;
                throw;
            }
        }

        /// <summary>
        /// Test for the existence of a blob container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        public static Boolean Exists(this CloudBlobContainer container)
        {
            try {
                container.FetchAttributes();
                return true;
            } catch ( StorageClientException e ) {
                if ( e.ErrorCode == StorageErrorCode.ResourceNotFound )
                    return false;
                throw;
            }
        }

        /// <summary>
        /// Downloads to blob to a lcoal file.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="overwriteFileIfExists">if set to <c>true</c> [overwrite file if exists].</param>
        /// <returns></returns>
        public static void DownloadToFile(this CloudStorageAccount account, String blobUri, String localPath, Boolean overwriteFileIfExists)
        {
            //b| Bug: should throw the appropriate and meaningful argument exception.
            if ( account == null || String.IsNullOrEmpty(blobUri) || String.IsNullOrEmpty(localPath) )
                return;
            CloudBlob cloudBlob = account.CreateCloudBlobClient().GetBlobReference(blobUri);
            if (!cloudBlob.Exists())
                return;
            if ( File.Exists(localPath) && !overwriteFileIfExists )
                return;
            File.Delete(localPath);
            cloudBlob.DownloadToFile(localPath);
        }

        /// <summary>
        /// Downloads the blob file as a string.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <returns></returns>
        public static String DownloadText(this CloudStorageAccount account, String blobUri)
        {
            //b| Bug: should throw the appropriate and meaningful argument exception.
            if ( account == null || String.IsNullOrEmpty(blobUri) )
                return null;
            CloudBlob cloudBlob = account.CreateCloudBlobClient().GetBlobReference(blobUri);
            using ( var ms = new MemoryStream() )
            {
                cloudBlob.DownloadToStream(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using ( var sr = new StreamReader(ms) )
                    return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Uploads the file to blob storage.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <param name="createContainerIfNotExists">if set to <c>true</c> [create container if not exists].</param>
        /// <param name="overwriteBlobIfExists">if set to <c>true</c> [overwrite BLOB if exists].</param>
        /// <returns></returns>
        public static void UploadFile(this CloudStorageAccount account, String localPath, String blobUri, Boolean createContainerIfNotExists, Boolean overwriteBlobIfExists)
        {
            //b| Bug: should throw the appropriate and meaningful argument exception.
            if ( account == null || !File.Exists(localPath) || String.IsNullOrEmpty(blobUri) )
                return;
            CloudBlob blob = account.CreateCloudBlobClient().GetBlobReference(blobUri);
            if (createContainerIfNotExists)
                blob.Container.CreateIfNotExist();
            if ( overwriteBlobIfExists )
                blob.DeleteIfExists();
            blob.UploadFile(localPath);
        }

        /// <summary>
        /// Uploads a local VHD (virutal hard drive) file to an Azure cloud page blob (cloud drive).
        /// </summary>
        /// <remarks>
        /// Azure provisioning on a newly crated PageBlob will ensure that all zeros exist.
        /// 
        /// Given this, any upload of a local VHD can be minimized by limiting transfer to 
        /// only those pages (512 byte segments) which contain dirty bits. Also, want to avoid 
        /// a bunch of small upload batches of 512b, so simply testing each page for empty and 
        /// immediatly acting on the uploads in place isnt a good idea.
        /// 
        /// The code below attempts to queue up the largest possible block of dirty pages and
        /// then write them all at once.  (Well, more correctly, the largest block in each buffer
        /// window.) 
        /// 
        /// It does this by looking ahead for the next empty page. Everything in between
        /// two empty pages is dirty, and an upload begins. If each successive page is empty
        /// it just keeps incrementing the offsets without uploading anything.
        /// 
        /// Sure, disk fragmentation may result in massive tiny uploads instead of blocks, but
        /// considering the nature of a VHD, this should be the exception to the rule, with 
        /// dirty and empty areas of the disk grouped together.
        /// </remarks>
        /// <param name="blob">The target blob (cloud drive).</param>
        /// <param name="vhd">The local VHD file (virtual hard drive).</param>
        /// <returns>Size uploaded.</returns>
        public static Int64 UploadCloudDrive(this CloudPageBlob blob, FileInfo vhd)
        {
            const Int32 _1Kb = 1024;
            const Int32 _1Mb = _1Kb * _1Kb;
            const Int32 _4Mb = _1Mb * 4;
            const Int32 pageSize = 512;
            const Int32 bufferSize = _4Mb;
            const Int32 pagesInBuffer = bufferSize / pageSize;
        
            //i| Provision the Cloud Drive.
            Int64 cloudDriveSize = (Int32)Math.Ceiling((double)vhd.Length / pageSize) * pageSize;
            blob.Create(cloudDriveSize);

            //i| Set our local resources.
            FileStream stream = vhd.OpenRead();
            Byte[] buffer = new Byte[bufferSize];
            Int64 vhdOffset = 0;

            while (buffer.Fill(stream) > 0)
            {
                Int32 prevEmpty = -1;
                Int32 nextEmpty = 0;

                do {
                    nextEmpty = buffer.SeekNextEmpty(pageSize, prevEmpty);
                    if (nextEmpty-prevEmpty > 1)
                    {
                        Int32 byteOffset = (prevEmpty + 1) * pageSize;
                        blob.WritePages(new MemoryStream(buffer, byteOffset, (nextEmpty-prevEmpty)*pageSize), vhdOffset + byteOffset);
                    }
                    prevEmpty = nextEmpty;
                } while (nextEmpty < pagesInBuffer);

                //i| Status
                Console.WriteLine("{0,4} of {1} Mb ( {2,5:P2} )", vhdOffset / _1Mb, cloudDriveSize / _1Mb, ((Double)vhdOffset / cloudDriveSize));
                
                vhdOffset += bufferSize;
            }

            //i| Success
            Console.WriteLine("Blob successfully uploaded.");

            //i| Create snapshot.
            var snapshot = blob.CreateSnapshot();
            if (snapshot.SnapshotTime != null)
                Console.WriteLine("Snapshot created:  '{0}'", BlobRequest.Get(snapshot.Uri, 0, snapshot.SnapshotTime.Value.ToUniversalTime(), null).RequestUri.AbsoluteUri);
            else
                Console.WriteLine("Unable to create snapshot of uploaded page blob.");

            return vhdOffset;
        }

        
        /// <summary>
        /// Uploads the VHD.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="uploadUrl">The upload URL.</param>
        /// <param name="overwriteIfExists">Overwrite if exists.</param>
        public static void UploadVhd(this CloudStorageAccount account, String localPath, String uploadUrl, Boolean overwriteIfExists)
        {
            const Int32 _1KB = 1024;
            const Int32 _1MB = _1KB * _1KB;
            const Int32 _4MB = 4 * _1MB;

            const Int32 _PAGESIZE = 512;
            const Int32 _BUFFERSIZE = _4MB;
            const Int32 _BUFFERPAGES = _BUFFERSIZE/_PAGESIZE;

            CloudPageBlob blob = account.CreateCloudBlobClient().GetPageBlobReference(uploadUrl);
            if (blob.Exists() && !overwriteIfExists)
                throw new TargetException("The page blob already exists and forced overwrite set to false.");
            blob.Container.CreateIfNotExist();

            Func<Int64, Int64> roundToNearest = (currentSize) => (Int32)Math.Ceiling((double)currentSize / _PAGESIZE) * _PAGESIZE;
            Int64 localSize = new FileInfo(localPath).Length;
            Int64 roundedSize = (Int32) Math.Ceiling((double) localSize/_PAGESIZE)*_PAGESIZE;
            Int64 roundedUpSize = roundToNearest(new FileInfo(localPath).Length);
            Console.WriteLine("Total size of upload:  {0} Mb", roundedUpSize / _1MB);

            blob.Create(roundedUpSize);

            FileStream stream = File.OpenRead(localPath);
            Byte[]     buffer = new Byte[_BUFFERSIZE];
            Int32      bytesRead;
            Int64      vhdOffset = 0;

            // read 4MB at a time
            while ( ( bytesRead = stream.Read(buffer, 0, _BUFFERSIZE) ) > 0 )
            {
                // fill the remainder (if any) with zeros
                for ( Int32 i = bytesRead; i < _BUFFERSIZE; i++ )
                {
                    buffer[i] = 0;
                }

                Int32 offsetToTransfer = 0;
                Int32 sizeToTransfer = 0;
                for ( Int32 page = 0; page < _BUFFERPAGES; page++ )
                {
                    //i| test whether entire page is empty
                    Boolean empty = true;
                    for ( Int32 i = 0; i < _PAGESIZE; i++ )
                    {
                        if ( buffer[page * _PAGESIZE + i] != 0 )
                        {
                            empty = false;
                            break;
                        }
                    }

                    //i| if not empty, include the page in our next transfer
                    if ( !empty )
                    {
                        sizeToTransfer += _PAGESIZE;
                        Console.Write("-");
                    }
                    else
                    {
                        Console.Write("+");
                        //i| transfer everything we have so far
                        if ( sizeToTransfer > 0 )
                        {
                            blob.WritePages(new MemoryStream(buffer, offsetToTransfer, sizeToTransfer), vhdOffset);
                            vhdOffset += sizeToTransfer;
                        }
                        //i| start the next transfer after this page
                        sizeToTransfer = 0;
                        offsetToTransfer = ( page + 1 ) * _PAGESIZE;
                        //i| increment where we are in the VHD by a page
                        vhdOffset += _PAGESIZE;
                    }
                }

                // transfer whatever we have left at the end of the buffer
                if ( sizeToTransfer > 0 )
                {
                    blob.WritePages(new MemoryStream(buffer, offsetToTransfer, sizeToTransfer), vhdOffset);
                    vhdOffset += sizeToTransfer;
                }

                //i| Status
                Console.WriteLine("{0,4} of {1} Mb ( {2,5:P2} )", vhdOffset / _1MB, roundedUpSize / _1MB, ((Double)vhdOffset / roundedUpSize));
            }

            //i| Success
            Console.WriteLine("Blob successfully uploaded.");

            //i| Create snapshot.
            var snapshot = blob.CreateSnapshot();
            if (snapshot.SnapshotTime != null)
                Console.WriteLine("Snapshot created:  '{0}'", BlobRequest.Get(snapshot.Uri, 0, snapshot.SnapshotTime.Value.ToUniversalTime(), null).RequestUri.AbsoluteUri);
            else
                Console.WriteLine("Unable to create snapshot of uploaded page blob.");
            }

    }

    /// <summary>
    /// Extension methods for byte arrays (byte[]).
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Determines whether a page in the provided buffer contains all zeros.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageNumber">The page number to check for empty.</param>
        /// <returns>
        /// 	<c>true</c> if page in the buffer is empty; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsPageEmpty(this Byte[] buffer, Int32 pageSize, Int32 pageNumber)
        {
            Int32 byteOffset = pageNumber * pageSize;
            Int32 pageEnd = byteOffset + pageSize;
            do { if (buffer[byteOffset++] != 0) return false;
            } while (byteOffset < pageEnd);
            return true;
        }

        /// <summary>
        /// Fills the specified buffer with content from the supplied stream.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="stream">The stream.</param>
        /// <returns>Number of bytes read from the buffer.</returns>
        public static Int32 Fill(this Byte[] buffer, FileStream stream)
        {
            Int32 count = stream.Read(buffer, 0, buffer.Length);
            Array.Clear(buffer, count, buffer.Length);
            return count;
        }

        /// <summary>
        /// Seeks the next empty.
        /// </summary>
        /// <param name="buffer">The buffer to search.</param>
        /// <param name="pageSize">Size of the pages.</param>
        /// <param name="pageIndex">The index of the page to start from.</param>
        /// <returns>The index of the next empty page. If no empty page found the page after the end of the buffer.</returns>
        public static Int32 SeekNextEmpty(this Byte[] buffer, Int32 pageSize, Int32 pageIndex)
        {
            do {
            } while (!buffer.IsPageEmpty(pageSize, ++pageIndex) && pageIndex < (buffer.Length / pageSize));
            return pageIndex;
        }
    }

    //public class TransferBuffer
    //{
    //    private Byte[] _buffer = new Byte[BufferSize];

    //    public Byte[] Buffer
    //    {
    //        get { return _buffer; }
    //    }

    //    public Boolean FillBuffer(FileStream stream)
    //    {
    //        Int32 count = stream.Read(Buffer, 0, Buffer.Length);
    //        Array.Clear(Buffer, count, Buffer.Length);
    //        return (count > 0);
    //    }

    //    public Boolean IsPageEmpty(Int32 pageNumber)
    //    {
    //        Int32 byteOffset = pageNumber * PageSize;
    //        do
    //        {
    //            if (Buffer[byteOffset++] != 0) return false;
    //        } while (byteOffset < BufferSize);
    //        return true;
    //    }

    //    public Int32 SeekNextEmpty(Int32 pageOffset)
    //    {
    //        do
    //        {
    //        } while (!IsPageEmpty(++pageOffset) && pageOffset < PagesInBuffer);
    //        return pageOffset;
    //    }
    //}
}
