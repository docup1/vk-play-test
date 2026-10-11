namespace WeatherApp.Presentation;

/// <summary>
/// Определяет путь к файлу со списком городов.
/// </summary>
internal static class CityListPathResolver
{
    private const string DefaultFileName = "Cities.txt";

    /// <summary>
    /// Приоритет: аргумент командной строки -> файл рядом с приложением ->
    /// файл в текущем каталоге.
    /// </summary>
    public static string Resolve(string[] args)
    {
        foreach (string argument in args)
        {
            if (!string.IsNullOrWhiteSpace(argument) && !argument.StartsWith('-'))
            {
                return argument;
            }
        }

        string nearExecutable = Path.Combine(AppContext.BaseDirectory, DefaultFileName);

        if (File.Exists(nearExecutable))
        {
            return nearExecutable;
        }

        string inCurrentDirectory = Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName);

        if (File.Exists(inCurrentDirectory))
        {
            return inCurrentDirectory;
        }

        return nearExecutable;
    }
}
