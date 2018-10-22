using System;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Chegg.Hackathon.CopyVideo
{
  public class FileInfo
  {
    public string Key { get; set; }
    public string Bucket { get; set; }
    public bool result { get; set; }
  }

  public class AmazonS3StorageConfig
  {
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
  }


  public class Function : IDisposable
  {
    private readonly AmazonS3Client s3Client;
    private readonly BasicAWSCredentials credentials;
    private readonly AmazonS3StorageConfig config;

    public Function()
    {
      this.config = new AmazonS3StorageConfig
      {
        AccessKey = "AKIAJTXMJ7UJ47CT27GA", //Environment.GetEnvironmentVariable("accessKey"),
        SecretKey = "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" //Environment.GetEnvironmentVariable("secretKey"),
      };

      this.credentials = new BasicAWSCredentials(this.config.AccessKey, this.config.SecretKey);

      this.s3Client = new AmazonS3Client(this.credentials, new AmazonS3Config {RegionEndpoint = RegionEndpoint.USEast1});
    }

    public async Task<bool> FunctionHandler(FileInfo fileInfo, ILambdaContext context)
    {
      context.Logger.LogLine(JsonConvert.SerializeObject(fileInfo));

      var request = new CopyObjectRequest
      {
        SourceBucket = fileInfo.Bucket, SourceKey = fileInfo.Key, DestinationBucket = fileInfo.result ? "videos-validation-passed" : "videos-validation-failed",
        DestinationKey = fileInfo.Key
      };

      context.Logger.LogLine("copy object");

      CopyObjectResponse response = await this.s3Client.CopyObjectAsync(request).ConfigureAwait(false);

      if (response.HttpStatusCode != HttpStatusCode.OK)
      {
        return false;
      }

      context.Logger.LogLine("delete object");

      DeleteObjectResponse objectResponse = await this.s3Client.DeleteObjectAsync(new DeleteObjectRequest {Key = fileInfo.Key, BucketName = fileInfo.Bucket}).ConfigureAwait(false);

      return objectResponse.HttpStatusCode == HttpStatusCode.OK;
    }

    public void Dispose()
    {
      s3Client?.Dispose();
    }
  }
}
