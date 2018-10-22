using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace Chegg.Hackathon.VideoLength.Tests
{
  public class FunctionTests
  {
    [Fact]
    public async Task TestToUpperFunction()
    {
      Function function = new Function();
      TestLambdaContext context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Key = "Amy Bartel - ME - 5073-6-7P.mp4",
        Bucket = "videos-2-process"
      };

      bool result = await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);

      Assert.True(result, "the video should have been a valid one");
    }
  }
}
