using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;

namespace Chegg.Hackathon.DetectInstructor.Tests
{
  public class FunctionTest
  {
    [Fact]
    public async Task TestToUpperFunction()
    {

      // Invoke the lambda function and confirm the string was upper cased.
      Function function = new Function();
      TestLambdaContext context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Key = "Amy Bartel - ME - 5073-6-7P.mp4",
        Bucket = "videos-2-process"
      };

      bool success = await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);

      Assert.True(success, "should be a valid video");
    }
  }
}
