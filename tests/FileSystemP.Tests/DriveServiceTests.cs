using FileSystemP.Core.Services;

namespace FileSystemP.Tests;

public class DriveServiceTests
{
    // --- GetDrives ---

    [Fact]
    public void GetDrives_ReturnsAtLeastOneDrive()
    {
        var drives = DriveService.GetDrives();

        Assert.NotEmpty(drives);
    }

    [Fact]
    public void GetDrives_ReturnsOnlyReadyDrives()
    {
        var drives = DriveService.GetDrives();

        Assert.All(drives, d => Assert.True(d.IsReady));
    }

    // --- GetSpecificDrive ---

    [Fact]
    public void GetSpecificDrive_ReturnsCorrectDrive()
    {
        var drive = DriveService.GetSpecificDrive(@"C:\");

        Assert.NotNull(drive);
        Assert.Equal(@"C:\", drive.Name);
    }

    [Fact]
    public void GetSpecificDrive_ReturnsNullForNonExistentDrive()
    {
        var existingNames = DriveInfo.GetDrives().Select(d => d.Name).ToHashSet();
        var nonExistent = Enumerable.Range('A', 26)
            .Select(c => $"{(char)c}:\\")
            .First(name => !existingNames.Contains(name));

        var result = DriveService.GetSpecificDrive(nonExistent);

        Assert.Null(result);
    }
}
