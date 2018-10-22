using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Internal;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Chegg.Hackathon.TextDetection
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

  public class Function
  {
    private readonly AmazonS3StorageConfig config;
    private readonly BasicAWSCredentials credentials;
    private readonly AmazonSQSClient sqsClient;
    private readonly AmazonRekognitionClient rekClient;

    private const int MaxResults = 10;
    private const string QUrl = "https://sqs.us-east-1.amazonaws.com/518495728486/hackathon--2018-queue";

    public Function()
    {
      this.config = new AmazonS3StorageConfig
      {
        AccessKey = "AKIAJTXMJ7UJ47CT27GA", //Environment.GetEnvironmentVariable("accessKey"),
        SecretKey = "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" //Environment.GetEnvironmentVariable("secretKey"),
      };

      this.credentials = new BasicAWSCredentials(this.config.AccessKey, this.config.SecretKey);

      this.rekClient = new AmazonRekognitionClient(this.credentials, RegionEndpointProviderV2.RegionEndpoint.USEast1);

      this.sqsClient = new AmazonSQSClient(this.credentials, RegionEndpoint.USEast1);
    }

    public async Task<bool> FunctionHandler(FileInfo fileInfo, ILambdaContext context)
    {
      DetectTextRequest request = new DetectTextRequest
      {
        Video = new Video
        {
          S3Object = new S3Object
          {
            Bucket = fileInfo.Bucket,
            Name = fileInfo.Key
          }
        },
        NotificationChannel = new NotificationChannel { RoleArn = "arn:aws:iam::518495728486:role/hackathon-rek-role", SNSTopicArn = "arn:aws:sns:us-east-1:518495728486:AmazonRekognition-hackathon-2018" }
      };

      StartPersonTrackingResponse response = await rekClient.DetectTextAsync(request).ConfigureAwait(false);

      bool validLength = await ProcessVideoMessages(context, response);

      return validLength;
    }

    private async Task<bool> ProcessVideoMessages(ILambdaContext context, StartPersonTrackingResponse response)
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

    private async Task<bool> ProcessInterestedMessage(ILambdaContext context, StartPersonTrackingResponse response,
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

    //private readonly List<EmotionName> invalidEmotions = new List<EmotionName>
    //{
    //  EmotionName.SAD, EmotionName.SURPRISED, EmotionName.ANGRY, EmotionName.CONFUSED, EmotionName.DISGUSTED,
    //  EmotionName.UNKNOWN
    //};

    private async Task<bool> ProcessLabels(ILambdaContext context, string jobId)
    {
      bool success = true;

      GetPersonTrackingResponse response = null;

      do
      {
        GetPersonTrackingRequest request = new GetPersonTrackingRequest
        {
          JobId = jobId,
          MaxResults = MaxResults,
          NextToken = response?.NextToken,
          SortBy = PersonTrackingSortBy.TIMESTAMP
        };

        response = await this.rekClient.GetPersonTrackingAsync(request).ConfigureAwait(false);

        //more than one person detected

        if (response.Persons.Any(x => x.Person.Index != 0))
        {
          success = false;
          break;
        }

        if (response.Persons.Any(x => x.Person.Face?.Quality.Brightness < 50.0f || x.Person.Face?.Quality.Sharpness < 50.0f))
        {
          success = false;
          break;
        }

        if (response.Persons.Any(x => x.Person.Face?.Confidence < 50))
        {
          success = false;
          break;
        }

        //if(response.Persons.Any(x => x.Person.Face?.Emotions.Any(y => invalidEmotions.Contains(y.Type))))

      } while (response?.NextToken != null);

      return success;
    }
  }
}
