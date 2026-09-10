using CPE_Review_Hub.Security;
using Xunit;

namespace CPE_Review_Hub.Tests;

public class LessonImageUrlPolicyTests
{
    [Theory]
    [InlineData("https://upload.wikimedia.org/wikipedia/commons/4/47/Binary_heap.svg")]
    [InlineData("https://upload.wikimedia.org/wikipedia/commons/0/0a/Power_triangle.jpg")]
    public void DirectWikimediaMediaUrlIsAllowed(string url) =>
        Assert.True(LessonImageUrlPolicy.IsAllowedDirectImage(url));

    [Theory]
    [InlineData("http://upload.wikimedia.org/wikipedia/commons/a/a.svg")]
    [InlineData("https://commons.wikimedia.org/wiki/File:Binary_heap.svg")]
    [InlineData("https://example.com/diagram.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    [InlineData("file:///C:/diagram.png")]
    [InlineData("https://127.0.0.1/diagram.png")]
    public void UnsafeOrNonDirectUrlIsRejected(string url) =>
        Assert.False(LessonImageUrlPolicy.IsAllowedDirectImage(url));

    [Fact]
    public void CommonsFilePageIsAllowed() =>
        Assert.True(LessonImageUrlPolicy.IsAllowedSourcePage(
            "https://commons.wikimedia.org/wiki/File:Binary_heap.svg"));
}
