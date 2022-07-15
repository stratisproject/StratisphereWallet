using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Stratis.SmartContracts;
using Unity3dApi;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MyCollectionWindow : WindowBase
{
    public GameObject ContentGameObject;

    public Text StatusText;

    public CollectionItem CollectionCopyFromItem;

    public ScrollRect ScrollRect;

    public List<CollectionItem> SpawnedItems = new List<CollectionItem>();

    public Sprite ImageNotAvailableSprite, LoadingImageSprite, NoImageButAnimationSprite;

    private float defaultScrollRectVerticalPosition;

    private HttpClient client = new HttpClient();

    private CancellationTokenSource cancellation;

    private bool collectionLoadingInProgress = false;


    void Awake()
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        defaultScrollRectVerticalPosition = ScrollRect.verticalNormalizedPosition;
    }

    public override async UniTask HideAsync()
    {
        this.cancellation?.Cancel();

        while (collectionLoadingInProgress)
            await Task.Delay(50);

        await base.HideAsync();
    }

    public override async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        collectionLoadingInProgress = true;

        UniTask showTask = this.showAsync(hideOtherWindows);

        await showTask;

        collectionLoadingInProgress = false;
    }

    private async UniTask showAsync(bool hideOtherWindows = true)
    {
        try
        {
            this.cancellation = new CancellationTokenSource();

            Debug.Log("Loading collection started");

            this.StatusText.text = "loading collection";
            ScrollRect.verticalNormalizedPosition = this.defaultScrollRectVerticalPosition;

            // Disable prev spawned items.
            foreach (CollectionItem prevSpawn in SpawnedItems)
                prevSpawn.gameObject.SetActive(false);

            await base.ShowAsync(hideOtherWindows);

            Debug.Log("Loading collection items");

            // Populates SpawnedItems
            await this.LoadCollectionItemsAsync(this.cancellation.Token);

            // Resize content transform
            RectTransform contentTransform = ContentGameObject.GetComponent<RectTransform>();
            GridLayoutGroup gridLayoutGroup = ContentGameObject.GetComponent<GridLayoutGroup>();
            float cellSize = gridLayoutGroup.cellSize.y;
            float spacing = gridLayoutGroup.spacing.y;
            int rows = (int)Math.Ceiling(((decimal)SpawnedItems.Count / 3));
            contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, (cellSize + spacing) * rows);

            // Load images
            List<CollectionItem> notLoaded = this.SpawnedItems.Where(x => !x.ImageLoadedOrAttemptedToLoad && x.NFTUri.StartsWith("https://")).ToList();

            List<UniTask<Texture2D>> loadTasks = new List<UniTask<Texture2D>>();

            Debug.Log("Loading json metadata for " + notLoaded.Count + " items.");

            if (this.cancellation.IsCancellationRequested)
                return;

            // Preload json
            Dictionary<string, string> jsonUriToJson = await this.LoadJsonFilesAsync(notLoaded.Select(x => x.NFTUri).Where(x => x.EndsWith(".json")), this.cancellation.Token);

            Debug.Log("Loading textures");
            this.StatusText.text = "loading textures...";

            // Load textures and animations
            List<string> animationsToConvert = new List<string>();
            List<NFTMetadataModel> metadataModels = new List<NFTMetadataModel>();

            for (int i = 0; i < notLoaded.Count; i++)
            {
                if (this.cancellation.IsCancellationRequested)
                    return;

                this.StatusText.text = "requesting textures " + (i + 1).ToString() + " / " + notLoaded.Count;

                try
                {
                    string uri = notLoaded[i].NFTUri;
                    string imageUri;

                    bool animationAvailable = false;
                    string animationUrl = null;

                    string json = jsonUriToJson[uri];
                    var settings = new JsonSerializerSettings();
                    settings.DateFormatString = "YYYY-MM-DD";
                    settings.ContractResolver = new CustomMetadataResolver();
                    NFTMetadataModel model = JsonConvert.DeserializeObject<NFTMetadataModel>(json, settings);

                    metadataModels.Add(model);

                    imageUri = model.Image;
                    notLoaded[i].TitleText.text = model.Name;

                    string animationUri = (!string.IsNullOrEmpty(model.AnimationUrl)) ? model.AnimationUrl :
                                            (model.Image.EndsWith(".webp") || model.Image.EndsWith(".gif") || model.Image.EndsWith(".mp4")) ? model.Image : null;

                    if (animationUri != null)
                    {
                        animationAvailable = true;
                        animationUrl = animationUri;
                        animationsToConvert.Add(model.AnimationUrl);
                    }

                    notLoaded[i].DisplayAnimationButton.gameObject.SetActive(animationAvailable);

                    if (animationAvailable)
                        notLoaded[i].NFTImage.sprite = NoImageButAnimationSprite;

                    bool image = !(imageUri.EndsWith(".gif") || imageUri.EndsWith(".mov") || imageUri.EndsWith(".webm") || imageUri.EndsWith(".avi")) && !string.IsNullOrEmpty(imageUri);
                    if (image)
                    {
                        UniTask<Texture2D> loadTask = this.GetRemoteTextureAsync(imageUri, this.cancellation.Token);
                        loadTasks.Add(loadTask);
                    }
                    else
                        loadTasks.Add(GetNullTextureAsync());

                    if (animationAvailable)
                    {
                        var index = i;
                        notLoaded[i].DisplayAnimationButton.onClick.RemoveAllListeners();
                        notLoaded[i].DisplayAnimationButton.onClick.AddListener(async delegate
                        {
                            await NFTWalletWindowManager.Instance.AnimationWindow.ShowPopupAsync(animationUrl,
                                "NFTID: " + notLoaded[index].NFTID);
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }

            var allModels = JsonConvert.SerializeObject(metadataModels);
            Debug.Log(allModels);

            // Convert animations. Fire and forget
            PreconvertAnimations(animationsToConvert);

            Texture2D[] loaded = await UniTask.WhenAll(loadTasks);

            this.StatusText.text = "creating sprites...";

            for (int i = 0; i < loaded.Length; i++)
            {
                if (this.cancellation.IsCancellationRequested)
                    return;

                Texture2D texture = loaded[i];

                notLoaded[i].ImageLoadedOrAttemptedToLoad = true;

                if (texture == null)
                {
                    if (!notLoaded[i].DisplayAnimationButton.gameObject.activeSelf)
                        notLoaded[i].NFTImage.sprite = ImageNotAvailableSprite;

                    continue;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);

                notLoaded[i].NFTImage.sprite = sprite;
            }

            this.StatusText.text = "Collection loaded";
        }
        catch (Exception e)
        {
            Debug.LogError("Error while loading collection: " + e.ToString());
        }
    }

    /// <summary>
    /// Spawns collection items and adds them to SpawnedItems if it's not there.
    /// Loads URIs.
    /// </summary>
    private async Task LoadCollectionItemsAsync(CancellationToken token)
    {
        List<DeployedNFTModel> knownNfts = NFTWallet.Instance.LoadKnownNfts();
        OwnedNFTsModel myNfts = await NFTWallet.Instance.StratisUnityManager.Client.GetOwnedNftsAsync(NFTWallet.Instance.StratisUnityManager.GetAddress().ToString());

        List<Task> uriLoadTasks = new List<Task>();

        foreach (KeyValuePair<string, ICollection<long>> contrAddrToOwnedIds in myNfts.OwnedIDsByContractAddress)
        {
            if (token.IsCancellationRequested)
                return;

            string contractAddr = contrAddrToOwnedIds.Key;
            List<long> ownedIds = contrAddrToOwnedIds.Value.Distinct().ToList();

            DeployedNFTModel knownNft = knownNfts.FirstOrDefault(x => x.ContractAddress == contractAddr);
            string nftName = (knownNft == null) ? string.Empty : knownNft.NftName;

            NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, contractAddr);

            // Create item for each owned ID or enable already spawned item
            for (int i = 0; i < ownedIds.Count; i++)
            {
                long currentId = ownedIds[i];

                var alreadySpawnedItem = SpawnedItems.SingleOrDefault(x => x.ContractAddr == contractAddr && x.NFTID == currentId);

                if (alreadySpawnedItem != null)
                {
                    alreadySpawnedItem.gameObject.SetActive(true);
                    continue;
                }

                CollectionItem cItem = GameObject.Instantiate(CollectionCopyFromItem, ContentGameObject.transform);
                cItem.gameObject.SetActive(true);
                SpawnedItems.Add(cItem);

                cItem.NFTID = currentId;
                cItem.ContractAddr = contractAddr;
                cItem.DescriptionText.text = string.Format("{0}  ({1})", nftName, currentId);
                string sellUri = MarketplaceIntegration.Instance.GetSellURI(contractAddr, currentId);
                cItem.Sell_Button.onClick.AddListener(delegate { Application.OpenURL(sellUri); });


                Task TaskUriLoad = Task.Run(async () =>
                {
                    string uri = await wrapper.TokenURIAsync((UInt256) currentId);
                    cItem.NFTUri = uri;
                });

                uriLoadTasks.Add(TaskUriLoad);
            }
        }

        this.StatusText.text = "loading nft URIs...";

        await Task.WhenAll(uriLoadTasks);
    }

    /// <summary>
    /// Returns mapping jsonUri to json
    /// </summary>
    /// <returns></returns>
    private async Task<Dictionary<string, string>> LoadJsonFilesAsync(IEnumerable<string> uris, CancellationToken token)
    {
        this.StatusText.text = "loading json metadata";

        Dictionary<string, string> jsonUriToJson = new Dictionary<string, string>();
        object locker = new object();
        List<Task> tasks = new List<Task>();

        foreach (string uri in uris)
        {
            if (token.IsCancellationRequested)
                return null;

            string u = uri;
            var task = Task.Run(async () =>
            {
                string json = null;

                try
                {
                    HttpResponseMessage result = await this.client.GetAsync(u, token);
                    result.EnsureSuccessStatusCode();

                    json = await result.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }

                lock (locker)
                {
                    jsonUriToJson.Add(u, json);
                }
            });

            tasks.Add(task);
        }

        while (!token.IsCancellationRequested)
        {
            if (tasks.Count > 0)
                await Task.WhenAny(tasks);

            int completedCount = tasks.Count(x => x.IsCompleted);

            await Task.Delay(10);
            this.StatusText.text = string.Format("loading json metadata {0} / {1}", completedCount, tasks.Count);
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.StatusText.transform as RectTransform);

            if (completedCount == tasks.Count)
                break;
        }

        return jsonUriToJson;
    }

    private async UniTask<Texture2D> GetRemoteTextureAsync(string url, CancellationToken token)
    {
        Texture2D texture;

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            var asyncOp = www.SendWebRequest();

            while (asyncOp.isDone == false)
            {
                await Task.Delay(1000 / 30);//30 hertz

                if (token.IsCancellationRequested)
                    return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log($"{www.error}, URL:{www.url}");
                return null;
            }

            texture = DownloadHandlerTexture.GetContent(www);
        }

        if (texture != null && (texture.width > 600 || texture.height > 600))
        {
            Texture2D resized = ResizeTexture(texture, 600, 600);

            Object.Destroy(texture);

            return resized;
        }

        return texture;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Point;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

    private async UniTask<Texture2D> GetNullTextureAsync()
    {
        return null;
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
