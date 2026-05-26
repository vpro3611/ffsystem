using System.Security.AccessControl;
using FileSystemP.Core.MetadataService.DTO;
using System.Security.Principal;
using System.Runtime.Versioning;


namespace FileSystemP.Core.MetadataService.Providers.SecurityAndACL;

[SupportedOSPlatform("windows")]
public class SecurityMetadataProvider : ISecurityMetadataProvider
{
    private static FileSystemSecurity GetSecurity(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new AppException("Unsupported platform.", $"{nameof(SecurityMetadataProvider)}.{nameof(GetSecurity)}()");
        }

        FileSystemInfo info = Directory.Exists(path) 
            ? new DirectoryInfo(path) 
            : new FileInfo(path);

        if (!info.Exists)
        {
             throw new AppException($"Path not found: {path}", $"{nameof(SecurityMetadataProvider)}.{nameof(GetSecurity)}()");
        }

        return info switch
        {
            DirectoryInfo d => d.GetAccessControl(),
            FileInfo f => f.GetAccessControl(),
            _ => throw new AppException($"Unsupported file system object: {path}")
        };
    }

    public SecurityMetadataRecord GetSecurityMetadata(string path)
    {
        FileSystemSecurity security = GetSecurity(path);
        
        var owner = security.GetOwner(typeof(NTAccount));

        var group = security.GetGroup(typeof(NTAccount));

        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: true,
            targetType: typeof(NTAccount));

        List<PermissionEntryRecord> permissions = [];

        foreach (FileSystemAccessRule rule in rules)
        {
            permissions.Add(
                new PermissionEntryRecord(
                    Identity: rule.IdentityReference.Value,
                    Rights: rule.FileSystemRights,
                    Type: rule.AccessControlType,
                    IsInherited: rule.IsInherited,
                    InheritanceFlags: rule.InheritanceFlags,
                    PropagationFlags: rule.PropagationFlags
                )
            );
        }

        return new SecurityMetadataRecord(Owner: owner?.Value, Group: group?.Value, Permissions: permissions);
    }
}