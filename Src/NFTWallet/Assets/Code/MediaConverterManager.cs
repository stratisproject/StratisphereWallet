using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts.CLR.Serialization;
using Stratis.SmartContracts.Core;
using Stratis.SmartContracts.RuntimeObserver;
using Unity3dApi;
using UnityEngine;
using Network = NBitcoin.Network;

public class MediaConverterManager
{
    public readonly MediaConverterApi.MediaConverterClient Client;

    public MediaConverterManager(MediaConverterApi.MediaConverterClient client)
    {
        this.Client = client;
    }

    /// <summary>
    /// Convertes files to supported format.
    /// </summary>
    /// <param name="filesPath">Paths to files to be converted.</param>
    /// <returns>List of links to converted media files.</returns>
    public async Task<List<string>> ConvertFilesAsync(List<string> filesPath)
    {
        string requestId = (await Client.RequestFilesConversionAsync(filesPath)).RequestID;
        return await WaitForConversionResult(requestId, filesPath);
    }

    /// <summary>
    /// Convertes links to files to supported format.
    /// </summary>
    /// <param name="externalLinks">Links to files to be converted.</param>
    /// <returns>List of links to converted media files.</returns>
    public async Task<List<string>> ConvertLinksAsync(List<string> externalLinks)
    {
        string requestId = (await Client.RequestLinksConversionAsync(externalLinks)).RequestID;
        return await WaitForConversionResult(requestId, externalLinks);
    }

    private async Task<List<string>> WaitForConversionResult(string requestId, List<string> items)
    {
        while (true)
        {
            MediaConverterApi.ConversionResultModel result = await Client.GetConversionResultsAsync(requestId);

            if (result != null && result.Progress == MediaConverterApi.ConversionResultModel.ConversionProgress.COMPLETE)
            {
                return items
                    .Select(path => result.Links.TryGetValue(path, out var value) ? value : "")
                    .ToList();
            }

            Debug.Log("Waiting for result...");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
