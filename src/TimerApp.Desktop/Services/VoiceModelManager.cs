namespace TimerApp.Desktop.Services;

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

public static class VoiceModelManager
{
    private const string ModelUrl  = "https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip";
    private const string ModelName = "vosk-model-small-pt-0.3";

    public static string ModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TibiaTimer", "models", ModelName);

    public static bool IsDownloaded => Directory.Exists(ModelPath) &&
                                       Directory.GetFiles(ModelPath).Length > 0;

    public static async Task DownloadAsync(IProgress<int> progress)
    {
        var modelsDir = Path.GetDirectoryName(ModelPath)!;
        var zipPath   = Path.Combine(modelsDir, ModelName + ".zip");
        Directory.CreateDirectory(modelsDir);

        using var client   = new HttpClient();
        using var response = await client.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file   = File.Create(zipPath);

        var buffer     = new byte[8192];
        long downloaded = 0;
        int  read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (totalBytes > 0)
                progress.Report((int)(downloaded * 100 / totalBytes));
        }

        file.Close();

        ZipFile.ExtractToDirectory(zipPath, modelsDir, overwriteFiles: true);
        File.Delete(zipPath);
        progress.Report(100);
    }
}
