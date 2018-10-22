using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace Chegg.Hackathon.UnsafeContent.Tests

{
  public class FunctionTest
  {
    [Fact]
    public async Task TestToUpperFunction()
    {
      Function function = new Function();
      ILambdaContext context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Key = "unsafe-video.mp4",
        Bucket = "videos-2-process"
      };

      bool success = await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);

      Assert.True(success, "a true is expected");
    }
  }
}
