using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Chegg.Hackathon.UnsafeContent
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
    private readonly AmazonS3StorageConfig config;
    private readonly BasicAWSCredentials credentials;
    private readonly AmazonSQSClient sqsClient;
    private readonly AmazonRekognitionClient rekClient;

    private const int MaxResults = 10;
    private const string QUrl = "https://sqs.us-east-1.amazonaws.com/518495728486/hackathon--2018-queue";

    private readonly IList<string> labels = new List<string>
    {
      "Explicit Nudity",
      "Suggestive"
    };

    public Function()
    {
      this.config = new AmazonS3StorageConfig
      {
        AccessKey = "AKIAJTXMJ7UJ47CT27GA", //Environment.GetEnvironmentVariable("accessKey"),
        SecretKey = "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" //Environment.GetEnvironmentVariable("secretKey"),
      };

      this.credentials = new BasicAWSCredentials(this.config.AccessKey, this.config.SecretKey);

      this.rekClient = new AmazonRekognitionClient(this.credentials, RegionEndpoint.USEast1);

      this.sqsClient = new AmazonSQSClient(this.credentials, RegionEndpoint.USEast1);
    }

    public async Task<bool> FunctionHandler(FileInfo fileInfo, ILambdaContext context)
    {
      StartContentModerationRequest request = new StartContentModerationRequest
      {
        Video = new Video
        {
          S3Object = new S3Object
          {
            Bucket = fileInfo.Bucket,
            Name = fileInfo.Key
          }
        },
        MinConfidence = 40.0f,
        NotificationChannel = new NotificationChannel { RoleArn = "arn:aws:iam::518495728486:role/hackathon-rek-role", SNSTopicArn = "arn:aws:sns:us-east-1:518495728486:AmazonRekognition-hackathon-2018" }
      };

      StartContentModerationResponse response = await rekClient.StartContentModerationAsync(request).ConfigureAwait(false);

      bool validLength = await ProcessVideoMessages(context, response);

      return validLength;
    }

    private async Task<bool> ProcessVideoMessages(ILambdaContext context, StartContentModerationResponse response)
    {
      bool validLength;

      do
      {
        ReceiveMessageResponse messageResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
          QueueUrl = QUrl,
          MaxNumberOfMessages = 10,
          WaitTimeSeconds = 15
        }).ConfigureAwait(false);

        if (!messageResponse.Messages.Any())
        {
          continue;
        }

        Message message = messageResponse.Messages.SingleOrDefault(x => x.Body.Contains(response.JobId));

        if (message == null)
        {
          context.Logger.LogLine("job received is not of interest");

          messageResponse.Messages.ForEach(async msg => await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
          {
            QueueUrl = QUrl,
            ReceiptHandle = msg.ReceiptHandle
          }));
        }
        else
        {
          validLength = await ProcessInterestedMessage(context, response, message).ConfigureAwait(false);
          break;
        }
      } while (true);

      return validLength;
    }

    private async Task<bool> ProcessInterestedMessage(ILambdaContext context, StartContentModerationResponse response,
      Message message)
    {
      bool validLength = false;

      if (message.Body.Contains("SUCCEEDED"))
      {
        context.Logger.LogLine(
          $"job with jobId {response.JobId} found and succeeded, continuing to process remaining messages");
        validLength = await this.ProcessLabels(context, response.JobId).ConfigureAwait(false);
      }
      else
      {
        context.Logger.LogLine($"job with jobId {response.JobId} found and failed, please check cloud watch logs for more details");
      }

      await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
      {
        QueueUrl = QUrl,
        ReceiptHandle = message.ReceiptHandle
      }).ConfigureAwait(false);

      return validLength;
    }

    private async Task<bool> ProcessLabels(ILambdaContext context, string jobId)
    {
      GetContentModerationResponse response = null;

      do
      {
        GetContentModerationRequest request = new GetContentModerationRequest
        {
          JobId = jobId,
          MaxResults = MaxResults,
          NextToken = response?.NextToken,
          SortBy = ContentModerationSortBy.TIMESTAMP
        };

        response = await this.rekClient.GetContentModerationAsync(request).ConfigureAwait(false);

        return response.ModerationLabels.Any(x =>
          this.labels.Contains(x.ModerationLabel.Name) || this.labels.Contains(x.ModerationLabel.ParentName) &&
          x.ModerationLabel.Confidence > 40);

      } while (response?.NextToken != null);
    }

    public void Dispose()
    {
      this.sqsClient?.Dispose();
      this.rekClient?.Dispose();
    }
  }
}
