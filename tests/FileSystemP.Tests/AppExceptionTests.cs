using FileSystemP.Core;

namespace FileSystemP.Tests;

public class AppExceptionTests
{
    [Fact]
    public void AppException_CanBeThrown()
    {
        Action act = () => throw new AppException("msg", "MyService.Method()", "source");
        var ex = Record.Exception(act);

        Assert.IsType<AppException>(ex);
    }

    [Fact]
    public void AppException_CanBeCaughtAsBaseException()
    {
        Exception? caught = null;
        try { throw new AppException("msg", "MyService.Method()", "source"); }
        catch (Exception e) { caught = e; }

        Assert.IsType<AppException>(caught);
    }

    [Fact]
    public void AppException_MessageIsAccessible()
    {
        var ex = new AppException("something went wrong", "MyService.Method()", "source");

        Assert.Equal("something went wrong", ex.Message);
    }

    [Fact]
    public void AppException_InnerExceptionIsAccessible()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AppException("outer", "MyService.Method()", "source", inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AppException_WithoutInnerException_InnerExceptionIsNull()
    {
        var ex = new AppException("msg", "MyService.Method()", "source");

        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void AppException_NullableFieldsDoNotThrow()
    {
        var ex = Record.Exception(() => new AppException("msg", null, null));

        Assert.Null(ex);
    }
}
