using System;
using System.IO;
using Serilog.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

using socialized;

namespace Common
{
    public class AwsUploader : FileManager
    {
        private string bucketName;
        private RegionEndpoint region;
        private IAmazonS3 s3Client;
        private TransferUtility fileTransferUtility;

        public AwsUploader(Logger log) : base(log)
        {
            this.log = log;
            var configuration = Program.serverConfiguration();
            this.region = RegionEndpoint.GetBySystemName(configuration.GetValue<string>("aws_bucket_region"));
            this.bucketName = configuration.GetValue<string>("aws_bucket_name");
            this.s3Client = new AmazonS3Client(configuration.GetValue<string>("aws_access_key_id"),
                configuration.GetValue<string>("aws_secret_key_id"), 
                region);
            this.fileTransferUtility = new TransferUtility(s3Client);
        }
        public override string SaveFile(IFormFile file, string RelativePath)
        {
            string fileName = CreateHash(10);
            ChangeDailyPath();
            string fileRelativePath = RelativePath + dailyFolder;
            if (SaveTo(file, fileRelativePath + fileName))
                return fileRelativePath + fileName;
            return null;
        }
        public bool SaveTo(IFormFile file, string relativeFilePath)
        {
            try {
                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest {
                    BucketName = bucketName,
                    InputStream = file.OpenReadStream(),
                    StorageClass = S3StorageClass.StandardInfrequentAccess,
                    PartSize = file.Length,
                    Key = relativeFilePath,
                    CannedACL = S3CannedACL.PublicRead,
                };
                fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                fileTransferUtilityRequest.Metadata.Add("param2", "Value2");
                fileTransferUtility.Upload(fileTransferUtilityRequest);
                log.Information("Upload file to s3 bucket, file path -> " + relativeFilePath);
                return true;
            }
            catch (AmazonS3Exception e) {
                log.Error("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e) {
                log.Error("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return false;
        }
        public string SaveFile(Stream stream, string RelativePath)
        {
            string fileName = CreateHash(10);
            ChangeDailyPath();
            string relativePath = RelativePath + dailyFolder + fileName;
            if (SaveTo(stream, relativePath))
                return relativePath;
            return null;
        }
        public bool SaveTo(Stream stream, string relativeFilePath)
        {
            try {
                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest {
                    BucketName = bucketName,
                    InputStream = stream,
                    StorageClass = S3StorageClass.StandardInfrequentAccess,
                    PartSize = stream.Length,
                    Key = relativeFilePath,
                    CannedACL = S3CannedACL.PublicRead,
                };
                fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                fileTransferUtilityRequest.Metadata.Add("param2", "Value2");
                fileTransferUtility.Upload(fileTransferUtilityRequest);
                log.Information("Upload file to s3 bucket, file path -> " + relativeFilePath);
                return true;
            }
            catch (AmazonS3Exception e) {
                log.Error("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e) {
                log.Error("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return false;
        }
        
        public bool SaveTo(string fullPathFile, string relativeFilePath)
        {
            try {
                System.IO.FileStream stream = System.IO.File.OpenRead(fullPathFile);
                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest {
                    BucketName = bucketName,
                    InputStream = stream,
                    StorageClass = S3StorageClass.StandardInfrequentAccess,
                    PartSize = stream.Length,
                    Key = relativeFilePath,
                    CannedACL = S3CannedACL.PublicRead,
                };
                fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                fileTransferUtilityRequest.Metadata.Add("param2", "Value2");
                fileTransferUtility.Upload(fileTransferUtilityRequest);
                log.Information("Upload file to s3 bucket, file path -> " + relativeFilePath);
                return true;
            }
            catch (AmazonS3Exception e) {
                log.Error("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e) {
                log.Error("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return false;
        }
    }
}