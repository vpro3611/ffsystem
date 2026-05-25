using System.Security.AccessControl;
using FileSystemP.Core.MetadataService.DTO;
using System.Security.Principal;
using System.Runtime.Versioning;


namespace FileSystemP.Core.MetadataService.Providers.SecurityAndACL;

[SupportedOSPlatform("windows")]
public class SecurityMetadataProvider : ISecurityMetadataProvider
{
    private string _className = nameof(SecurityMetadataProvider);
    private static string _classNameStatic = nameof(SecurityMetadataProvider);

    private static FileSystemSecurity GetSecurity(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new AppException("Unsupported platform.", $"{_classNameStatic}.{nameof(GetSecurity)}()");
        }

        if (Directory.Exists(path))
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.GetAccessControl();
        }
        if (File.Exists(path))
        {
            FileInfo file = new FileInfo(path);
            return file.GetAccessControl();
        }

        throw new AppException($"Path not found: {path}", $"{_classNameStatic}.{nameof(GetSecurity)}()");
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
                    IsInherited: rule.IsInherited
                )
            );
        }

        return new SecurityMetadataRecord(Owner: owner?.Value, Group: group?.Value, Permissions: permissions);
    }
}