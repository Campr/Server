using Campr.Server.Lib.Infrastructure;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Campr.Server.Lib.Data
{
    class BlobContainer : IBlobContainer
    {
        public BlobContainer(CloudBlobContainer baseContainer)
        {
            Ensure.Argument.IsNotNull(baseContainer, "baseContainer");
            this.baseContainer = baseContainer;
        }
        
        private readonly CloudBlobContainer baseContainer;
        
        public IBlob GetBlob(string blobId)
        {
            var blockBlobReference = this.baseContainer.GetBlockBlobReference(blobId);
            return new Blob(blockBlobReference);
        }
    }
}