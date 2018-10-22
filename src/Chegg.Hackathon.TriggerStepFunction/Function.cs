using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Chegg.Hackathon.TriggerStepFunction
{
  public class FileInfo
  {
    public string Key { get; set; }
    public string Bucket { get; set; }
  }

  public class AmazonS3StorageConfig
  {
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
  }

  public class Function : IDisposable
  {
    private readonly AmazonStepFunctionsClient functionsClient;
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

      functionsClient = new AmazonStepFunctionsClient(this.credentials, RegionEndpoint.USEast1);
    }

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
      var s3Event = evnt.Records?[0].S3;
      if (s3Event == null)
      {
        return;
      }

      FileInfo fileInfo = new FileInfo { Bucket = s3Event.Bucket.Name, Key = s3Event.Object.Key };

      string input = JsonConvert.SerializeObject(fileInfo);

      StartExecutionResponse response = await functionsClient.StartExecutionAsync(new StartExecutionRequest { Input = input, Name = Guid.NewGuid().ToString("N"), StateMachineArn = "arn:aws:states:us-east-1:518495728486:stateMachine:hackathon-2018-chegg" }).ConfigureAwait(false);
    }

    public void Dispose()
    {
      functionsClient?.Dispose();
    }
  }
}
