using FileSystemP.Core.MetadataService.DTO;
using System.Security.AccessControl;
using System.Security.Principal;

using System.Runtime.Versioning;

namespace FileSystemP.Core.MetadataService.Providers.SecurityAndACL;

public interface ISecurityModifierService
{
    bool ValidateIdentity(string identity);
    void ApplySecurityChanges(string path, SecurityTransaction transaction);
}

[SupportedOSPlatform("windows")]
public class SecurityModifierService : ISecurityModifierService
{
    public bool ValidateIdentity(string identity)
    {
        try
        {
            new NTAccount(identity).Translate(typeof(SecurityIdentifier));
            return true;
        }
        catch { return false; }
    }

    public void ApplySecurityChanges(string path, SecurityTransaction transaction)
    {
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        FileSystemSecurity security = info switch
        {
            DirectoryInfo d => d.GetAccessControl(),
            FileInfo f => f.GetAccessControl(),
            _ => throw new ArgumentException("Invalid path")
        };

        if (transaction.IsInheritanceProtected.HasValue)
        {
            security.SetAccessRuleProtection(transaction.IsInheritanceProtected.Value, transaction.PreserveInheritanceOnProtect);
        }

        foreach (var change in transaction.Changes)
        {
            if (change.OldEntry != null)
            {
                security.RemoveAccessRule(new FileSystemAccessRule(
                    new NTAccount(change.OldEntry.Identity),
                    change.OldEntry.Rights,
                    change.OldEntry.Type));
            }

            if (change.NewEntry != null)
            {
                security.AddAccessRule(new FileSystemAccessRule(
                    new NTAccount(change.NewEntry.Identity),
                    change.NewEntry.Rights,
                    change.NewEntry.Type));
            }
        }

        if (info is DirectoryInfo di) di.SetAccessControl((DirectorySecurity)security);
        else if (info is FileInfo fi) fi.SetAccessControl((FileSecurity)security);
    }
}
