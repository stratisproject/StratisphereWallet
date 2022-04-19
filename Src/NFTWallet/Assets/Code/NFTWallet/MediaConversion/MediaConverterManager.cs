using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MediaConverterManager
{
    public static MediaConverterManager Instance;

    public readonly MediaConverterApi.MediaConverterClient Client;

    public MediaConverterManager(MediaConverterApi.MediaConverterClient client)
    {
        this.Client = client;

        Instance = this;
    }

    /// <summary>
    /// Convertes files to supported format.
    /// </summary>
    /// <param name="filesPath">Paths to files to be converted.</param>
    /// <returns>List of links to converted media files.</returns>
    public async Task<IDictionary<string, string>> ConvertFilesAsync(List<string> filesPath)
    {
        string requestId = (await Client.RequestFilesConversionAsync(filesPath)).RequestID;
        return await WaitForConversionResult(requestId, filesPath);
    }

    /// <summary>
    /// Convertes links to files to supported format.
    /// </summary>
    /// <param name="externalLinks">Links to files to be converted.</param>
    /// <returns>List of links to converted media files.</returns>
    public async Task<IDictionary<string, string>> ConvertLinksAsync(List<string> externalLinks)
    {
        string requestId = (await Client.RequestLinksConversionAsync(externalLinks)).RequestID;
        return await WaitForConversionResult(requestId, externalLinks);
    }

    private async Task<IDictionary<string, string>> WaitForConversionResult(string requestId, List<string> items)
    {
        while (true)
        {
            MediaConverterApi.ConversionResultModel result = await Client.GetConversionResultsAsync(requestId);

            if (result != null && result.Progress == MediaConverterApi.ConversionResultModel.ConversionProgress.COMPLETE)
            {
                return result.Links;
            }

            Debug.Log("Waiting for animation conversion result...");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
