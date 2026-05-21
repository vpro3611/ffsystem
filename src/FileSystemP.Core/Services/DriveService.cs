namespace FileSystemP.Core.Services;

public static class DriveService
{
    public static List<DriveInfo> GetDrives()
    {
        try
        {
            return DriveInfo
                .GetDrives()
                .Where(d => d.IsReady)
                .ToList();
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{nameof(DriveService)}.{nameof(GetDrives)}()", e.Source, e);
        }
    }

    public static DriveInfo? GetSpecificDrive(string driveName)
    {
        return DriveInfo.GetDrives().FirstOrDefault(d => d.Name == driveName);
    }
}