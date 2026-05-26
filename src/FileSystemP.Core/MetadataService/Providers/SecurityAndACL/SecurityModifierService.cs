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
        System.Diagnostics.Debug.WriteLine($"ApplySecurityChanges for {path}");
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        FileSystemSecurity security = info switch
        {
            DirectoryInfo d => d.GetAccessControl(),
            FileInfo f => f.GetAccessControl(),
            _ => throw new ArgumentException("Invalid path")
        };

        if (transaction.IsInheritanceProtected.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"Setting inheritance protection: {transaction.IsInheritanceProtected.Value}");
            security.SetAccessRuleProtection(transaction.IsInheritanceProtected.Value, transaction.PreserveInheritanceOnProtect);
        }

        foreach (var change in transaction.Changes)
        {
            if (change.OldEntry != null && !change.OldEntry.IsInherited)
            {
                var rule = new FileSystemAccessRule(
                    new NTAccount(change.OldEntry.Identity),
                    change.OldEntry.Rights,
                    change.OldEntry.InheritanceFlags,
                    change.OldEntry.PropagationFlags,
                    change.OldEntry.Type);
                bool removed = security.RemoveAccessRule(rule); // RemoveAccessRule is generally what we want for explicit rules
                System.Diagnostics.Debug.WriteLine($"Removing explicit rule for {change.OldEntry.Identity}: {removed} (Rights: {change.OldEntry.Rights})");
            }

            if (change.NewEntry != null)
            {
                var rule = new FileSystemAccessRule(
                    new NTAccount(change.NewEntry.Identity),
                    change.NewEntry.Rights,
                    change.NewEntry.InheritanceFlags,
                    change.NewEntry.PropagationFlags,
                    change.NewEntry.Type);
                security.AddAccessRule(rule);
                System.Diagnostics.Debug.WriteLine($"Adding rule for {change.NewEntry.Identity} (Rights: {change.NewEntry.Rights})");
            }
        }

        if (info is DirectoryInfo di) di.SetAccessControl((DirectorySecurity)security);
        else if (info is FileInfo fi) fi.SetAccessControl((FileSecurity)security);
        System.Diagnostics.Debug.WriteLine("SetAccessControl completed");
    }
}
