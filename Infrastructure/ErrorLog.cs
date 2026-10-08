namespace Pace;

// The most recent unexpected error, kept locally for support. Exception type and stack
// only: messages can contain paths, identities or response text.
internal static class ErrorLog
{
    public static string FilePath => Path.Combine(Settings.DirectoryPath, "error.log");

    public static void Write(Exception e)
    {
        try
        {
            Directory.CreateDirectory(Settings.DirectoryPath);
            File.WriteAllText(FilePath, $"{DateTimeOffset.Now:O} Pace {AppUpdates.CurrentVersion}\n{e.GetType().FullName}\n{e.StackTrace}\n");
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // Logging must never take the tray app down.
        }
    }
}
