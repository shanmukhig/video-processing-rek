using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using Chegg.Hackathon.CopyVideo;

namespace Chegg.Hackathon.CopyVideo.Tests
{
  public class FunctionTest
  {
    [Fact]
    public async Task TestToUpperFunction()
    {
      var function = new Function();
      var context = new TestLambdaContext();

      FileInfo fileInfo = new FileInfo
      {
        Key = "unsafe-video.mp4",
        Bucket = "videos-2-process",
        result = true
      };

      await function.FunctionHandler(fileInfo, context).ConfigureAwait(false);
    }
  }
}
