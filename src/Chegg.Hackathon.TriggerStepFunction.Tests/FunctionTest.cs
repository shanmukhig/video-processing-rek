using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.S3Events;
using Amazon.S3.Util;

namespace Chegg.Hackathon.TriggerStepFunction.Tests
{
  public class FunctionTest
  {
    [Fact]
    public async Task TestS3EventLambdaFunction()
    {
      var s3Event = new S3Event
      {
        Records = new List<S3EventNotification.S3EventNotificationRecord>
        {
            new S3EventNotification.S3EventNotificationRecord
            {
                S3 = new S3EventNotification.S3Entity
                {
                    Bucket = new S3EventNotification.S3BucketEntity {Name = "videos-2-process" },
                    Object = new S3EventNotification.S3ObjectEntity {Key = "unsafe-video.mp4" }
                }
            }
        }
      };

      Function function = new Function();

      // Invoke the lambda function and confirm the content type was returned.
      await function.FunctionHandler(s3Event, null).ConfigureAwait(false);
    }
  }
}
