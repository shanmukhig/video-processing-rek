using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.TestUtilities;

namespace Chegg.Hackathon.PersonTracking.Tests
{
  public class FunctionTest
  {
    [Fact]
    public async Task TestToUpperFunction()
    {

      // Invoke the lambda function and confirm the string was upper cased.
      var function = new Function();
      var context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Key = "Hannah Syndergaard - ME - 722157-2-2P.mp4",
        Bucket = "videos-2-process"
      };

      await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);
    }
  }
}
