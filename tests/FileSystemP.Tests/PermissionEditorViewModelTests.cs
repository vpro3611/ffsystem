using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using FileSystemP.WPF.ViewModels;
using Moq;
using System.Security.AccessControl;
using Xunit;

namespace FileSystemP.Tests;

public class PermissionEditorViewModelTests
{
    [Fact]
    public void AddIdentity_WithValidUser_AddsToIdentities()
    {
        var mockService = new Mock<ISecurityModifierService>();
        mockService.Setup(s => s.ValidateIdentity("ValidUser")).Returns(true);
        var viewModel = new PermissionEditorViewModel("path", mockService.Object);

        viewModel.NewIdentityName = "ValidUser";
        viewModel.AddIdentityCommand.Execute(null);

        Assert.Contains(viewModel.Identities, i => i.Name == "ValidUser");
    }

    [Fact]
    public void ToggleRight_GeneratesCorrectTransaction()
    {
        var mockService = new Mock<ISecurityModifierService>();
        var viewModel = new PermissionEditorViewModel("path", mockService.Object);
        
        // Initial state
        var initialRecord = new PermissionEntryRecord("User", FileSystemRights.Read, AccessControlType.Allow, false);
        viewModel.LoadPermissions(new[] { initialRecord });
        
        // Select user and change right
        viewModel.SelectedIdentity = viewModel.Identities[0];
        viewModel.IsWriteSelected = true; // Add write permission

        var transaction = viewModel.GenerateTransaction();
        
        Assert.Equal(2, transaction.Changes.Count);
        Assert.Contains(transaction.Changes, c => c.OldEntry != null && c.OldEntry.Identity == "User");
        Assert.Contains(transaction.Changes, c => c.NewEntry != null && c.NewEntry.Rights == (FileSystemRights.Read | FileSystemRights.Write));
    }
}
