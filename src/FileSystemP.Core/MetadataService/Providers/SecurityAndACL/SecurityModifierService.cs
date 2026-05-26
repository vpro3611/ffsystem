namespace FileSystemP.Core.MetadataService.Providers.SecurityAndACL;

public interface ISecurityModifierService
{
    bool ValidateIdentity(string identity);
}

public class SecurityModifierService : ISecurityModifierService
{
    public bool ValidateIdentity(string identity)
    {
        try
        {
            new System.Security.Principal.NTAccount(identity).Translate(typeof(System.Security.Principal.SecurityIdentifier));
            return true;
        }
        catch { return false; }
    }
}
