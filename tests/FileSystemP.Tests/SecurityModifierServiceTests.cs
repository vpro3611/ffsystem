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
    public void ApplySecurityChanges_AddsPermissionRule()
    {
        var service = new SecurityModifierService();
        string tempFile = Path.Combine(Path.GetTempPath(), $"fsp-test-{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");
        try
        {
            string currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            var newEntry = new FileSystemP.Core.MetadataService.DTO.PermissionEntryRecord(
                Identity: currentUser,
                Rights: System.Security.AccessControl.FileSystemRights.FullControl,
                Type: System.Security.AccessControl.AccessControlType.Allow,
                IsInherited: false
            );
            var transaction = new FileSystemP.Core.MetadataService.DTO.SecurityTransaction(
                IsInheritanceProtected: null,
                PreserveInheritanceOnProtect: false,
                Changes: new[] { new FileSystemP.Core.MetadataService.DTO.PermissionChange(null, newEntry) }
            );

            service.ApplySecurityChanges(tempFile, transaction);

            // Verify
            var security = new System.IO.FileInfo(tempFile).GetAccessControl();
            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            bool found = false;
            foreach (System.Security.AccessControl.FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.Value == currentUser && rule.FileSystemRights.HasFlag(System.Security.AccessControl.FileSystemRights.FullControl))
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
