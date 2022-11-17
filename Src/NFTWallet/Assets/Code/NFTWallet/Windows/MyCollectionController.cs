using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class MyCollectionController
{
    public bool CollectionLoadingInProgress { get; private set; }
    public ItemUpdateListener ItemsListener;

    private ConcurrentDictionary<string, NFTItem> storage;

    private List<NFTItem> itemsList
    {
        get => new List<NFTItem>(storage.Values);
    }

    private List<NFTMetadataModel> NFTMetadataModels;

    private string WalletAddress
    {
        get => NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();
    }
    private HttpClient client = new HttpClient();
    private CancellationTokenSource cancellation = new CancellationTokenSource();

    public MyCollectionController()
    {
        storage = new ConcurrentDictionary<string, NFTItem>();
        NFTMetadataModels = new List<NFTMetadataModel>();

        CollectionLoadingInProgress = false;
        client.Timeout = TimeSpan.FromSeconds(10);
    }

    public async UniTask OnShowAsync()
    {
        cancellation = new CancellationTokenSource();

        try
        {
            CollectionLoadingInProgress = true;
            await this.LoadItemsAsync(cancellation.Token);
        }
        finally
        {
            CollectionLoadingInProgress = false;
        }
    }

    public async UniTask OnHideAsync()
    {
        this.cancellation?.Cancel();

        while (CollectionLoadingInProgress)
            await UniTask.Delay(50);
    }

    public async UniTask LoadItemsAsync(CancellationToken token)
    {
        NFTMetadataModels = new List<NFTMetadataModel>(NFTMetadataModels.Count);

        List<NFTItem> items = await this.LoadCollectionItemsAsync(token);

        UnityEngine.Debug.Log(string.Format("Loading {0} items", items.Count));

        foreach (NFTItem item in items)
        {
            this.SaveItemToStorage(item);
        }

        await UniTask.SwitchToMainThread();

        await this.ItemsListener.OnItemsLoadedAsync(items);

        await UniTask.SwitchToThreadPool();

        List<UniTask> itemsLoadingTasks = new List<UniTask>();

        foreach (var item in items)
        {
            itemsLoadingTasks.Add(
                UniTask.Defer(async () => await this.LoadItemInfoAsync(item.ContractAddress, item.TokenID, token))
            );
        }

        await UniTask.WhenAll(itemsLoadingTasks);

        LogMetadataModels();

        PreconvertAnimations(
            itemsList
                .Select(item => item.AnimationURL)
                .Where(animationURL => !string.IsNullOrEmpty(animationURL))
                .ToList()
        );
    }

    private void LogMetadataModels()
    {
        var allModels = JsonConvert.SerializeObject(NFTMetadataModels);
        UnityEngine.Debug.Log(allModels);
    }

    public NFTItem GetItem(string contractAddress, long tokenID)
    {
        string key = contractAddress + "#" + tokenID;

        if (storage.ContainsKey(key))
        {
            return storage[key];
        }

        return null;
    }

    private void SaveItemToStorage(NFTItem item)
    {
        string key = item.ContractAddress + "#" + item.TokenID;
        storage[key] = item;
    }

    private async UniTask<List<NFTItem>> LoadCollectionItemsAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread();
        List<DeployedNFTModel> knownNfts = NFTWallet.Instance.LoadKnownNfts();
        token.ThrowIfCancellationRequested();

        await UniTask.SwitchToThreadPool();

        List<BlockCoreApi.OwnedNFTItem> myNfts = await NFTWallet.Instance.GetBlockCoreApi().GetOwnedNFTIds(this.WalletAddress);

        List<NFTItem> items = new List<NFTItem>();

        foreach (BlockCoreApi.OwnedNFTItem myNft in myNfts)
        {
            DeployedNFTModel knownNft = knownNfts.FirstOrDefault(x => x.ContractAddress == myNft.contractId);
            string nftName = (knownNft == null) ? string.Empty : knownNft.NftName;

            items.Add(new NFTItem()
            {
                ContractAddress = myNft.contractId,
                TokenID = myNft.id,
                Name = string.Format("{0}  ({1})", nftName, myNft.id),
                SellURL = MarketplaceIntegration.Instance.GetSellURI(myNft.contractId, myNft.id),
                ImageIsLoading = true,
                MetadataUri = myNft.uri
            });
        }

        return items;
    }

    private async UniTask LoadItemInfoAsync(string contractAddress, long tokenID, CancellationToken cancellationToken)
    {
        NFTItem item = this.GetItem(contractAddress, tokenID);

        cancellationToken.ThrowIfCancellationRequested();

        await this.LoadItemMetadataAsync(contractAddress, tokenID, item.MetadataUri, cancellationToken);

        await RequestImageLoadingAsync(item, cancellationToken);
    }

    private async UniTask LoadItemMetadataAsync(string contractAddress, long tokenID, string metadataUri, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var metadata = ConvertToNFTMetadata(await this.LoadJsonAsync(metadataUri, token));

        NFTMetadataModels.Add(metadata);

        var item = GetItem(contractAddress, tokenID);
        item.Description = metadata.Description;
        HandleImageURIs(item, metadata);
    }

    private void HandleImageURIs(NFTItem item, NFTMetadataModel metadata)
    {
        string imageUri = !IsAnimationURI(metadata.Image) ? metadata.Image : null;
        string animationUri = (!string.IsNullOrEmpty(metadata.AnimationUrl))
            ? metadata.AnimationUrl
            : IsAnimationURI(metadata.Image) ? metadata.Image : null;

        item.ImageURL = imageUri;

        if (animationUri != null && animationUri.StartsWith("ipfs://"))
            animationUri = "https://cloudflare-ipfs.com/ipfs/" + animationUri.Replace("ipfs://", string.Empty);

        item.AnimationURL = animationUri;
    }

    private async UniTask RequestImageLoadingAsync(NFTItem item, CancellationToken token)
    {
        if (!string.IsNullOrEmpty(item.ImageURL))
        {
            item.ImageIsLoading = true;
        }

        await RequestItemUpdateAsync(item);

        try
        {
            item.ImageTexture = await GetRemoteTextureAsync(item.ImageURL, token);
        }
        catch (OperationCanceledException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
        finally
        {
            item.ImageIsLoading = false;
        }

        await RequestItemUpdateAsync(item);
    }

    private bool IsAnimationURI(string uri)
    {
        return uri.EndsWith(".gif") ||
            uri.EndsWith(".mp4") ||
            uri.EndsWith(".mov") ||
            uri.EndsWith(".webm") ||
            uri.EndsWith(".webp") ||
            uri.EndsWith(".avi");
    }

    private async UniTask<string> LoadJsonAsync(string url, CancellationToken token)
    {
        if (url.StartsWith("ipfs://"))
            url = "https://cloudflare-ipfs.com/ipfs/" + url.Replace("ipfs://", string.Empty);

        try
        {
            HttpResponseMessage result = await this.client.GetAsync(url, token);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadAsStringAsync();
        }
        catch (OperationCanceledException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
            return null;
        }
    }

    private NFTMetadataModel ConvertToNFTMetadata(string json)
    {
        var settings = new JsonSerializerSettings();
        settings.DateFormatString = "YYYY-MM-DD";
        settings.ContractResolver = new CustomMetadataResolver();

        NFTMetadataModel model = null;

        if (json == null || string.IsNullOrEmpty(json))
        {
            model = new NFTMetadataModel();
            model.Name = model.Description = model.Image = "";
        }
        else
        {
            try
            {
                model = JsonConvert.DeserializeObject<NFTMetadataModel>(json, settings);
            }
            catch (JsonReaderException)
            {
                Debug.LogWarning("Broken json file!");
                model = new NFTMetadataModel();
                model.Description = model.AnimationUrl = model.Name = "[broken metadata]";
                model.Category = model.Category = model.ExternalUrl = model.Image = string.Empty;
            }
        }

        return model;
    }

    private async UniTask RequestItemUpdateAsync(NFTItem item)
    {
        await UniTask.SwitchToMainThread();

        if (ItemsListener != null)
        {
            await ItemsListener.OnItemUpdatedAsync(item);
        }

        await UniTask.SwitchToThreadPool();
    }

    private async UniTask<Texture2D> GetRemoteTextureAsync(string url, CancellationToken token, int timeoutSeconds = 30)
    {
        Texture2D texture;

        await UniTask.SwitchToMainThread();

        if (url.StartsWith("ipfs://"))
            url = "https://cloudflare-ipfs.com/ipfs/" + url.Replace("ipfs://", string.Empty);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = timeoutSeconds;
            await request.SendWebRequest().WithCancellation(token);

            if (request.result != UnityWebRequest.Result.Success)
            {
                await UniTask.SwitchToMainThread();
                UnityEngine.Debug.Log($"{request.error}, URL:{request.url}");
                return null;
            }

            texture = DownloadHandlerTexture.GetContent(request);
        }

        if (texture != null && (texture.width > 600 || texture.height > 600))
        {
            Texture2D resized = texture.ResizeTexture(600, 600);

            UnityEngine.Object.Destroy(texture);

            return resized;
        }

        return texture;
    }

    private void PreconvertAnimations(List<string> externalLinks)
    {
        Task.Run(async () =>
        {
            await Task.Delay(1);
            await MediaConverterManager.Instance.Client.RequestLinksConversionAsync(externalLinks);
        });
    }
}

public class NFTItem
{
    public string ID
    {
        get => this.ContractAddress + "#" + this.TokenID;
    }

    public string ContractAddress;
    public long TokenID;
    public string Name;
    public string Description;
    public string SellURL;
    public string ImageURL;
    public string AnimationURL;
    public string MetadataUri;
    public Texture2D ImageTexture;
    public bool ImageIsLoading;
}

public interface ItemUpdateListener
{
    public UniTask OnItemsLoadedAsync(List<NFTItem> items);
    public UniTask OnItemUpdatedAsync(NFTItem item);
}