using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.TestUtilities;

namespace Chegg.Hackathon.BlankVideo.Tests
{
  public class FunctionTest
  {
    [Fact]
    public async Task TestToUpperFunction()
    {
      Function function = new Function();
      TestLambdaContext context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Bucket = "videos-2-process",
        Key = "white_video.mp4"
      };

      bool success = await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);

      Assert.True(success, "a blank video is expected");
    }
  }
}
