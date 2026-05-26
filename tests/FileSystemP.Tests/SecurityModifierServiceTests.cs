using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using Xunit;

namespace FileSystemP.Tests;

public class SecurityModifierServiceTests
{
    [Fact]
    public void ValidateIdentity_WithExistingUser_ReturnsTrue()
    {
        var service = new SecurityModifierService();
        string currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        bool result = service.ValidateIdentity(currentUser);
        Assert.True(result);
    }

    [Fact]
    public void ValidateIdentity_WithNonExistentUser_ReturnsFalse()
    {
        var service = new SecurityModifierService();
        bool result = service.ValidateIdentity("NonExistentUser_12345");
        Assert.False(result);
    }
}
