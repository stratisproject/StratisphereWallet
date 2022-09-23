using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NBitcoin;
using Stratis.SmartContracts;
using Unity3dApi;
using UnityEngine;
using UnityEngine.UI;

public class MyCollectionWindow : WindowBase, ItemUpdateListener
{
    public GameObject ContentGameObject;

    public Text StatusText;

    public CollectionItem CollectionCopyFromItem;

    public ScrollRect ScrollRect;

    public List<CollectionItem> SpawnedItems = new List<CollectionItem>();

    public Sprite ImageNotAvailableSprite, LoadingImageSprite, NoImageButAnimationSprite;

    private float defaultScrollRectVerticalPosition;

    private MyCollectionController myCollectionController = new MyCollectionController();

    void Awake()
    {
        myCollectionController.ItemsListener = this;
        defaultScrollRectVerticalPosition = ScrollRect.verticalNormalizedPosition;
    }

    public override async UniTask HideAsync()
    {
        await myCollectionController.OnHideAsync();
        await base.HideAsync();
    }

    public override async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        await this.PerformShowAsync(hideOtherWindows);
    }

    private async UniTask PerformShowAsync(bool hideOtherWindows = true)
    {
        try
        {
            Debug.Log("Loading collection started");

            this.StatusText.text = "loading collection";
            ScrollRect.verticalNormalizedPosition = this.defaultScrollRectVerticalPosition;

            // Disable prev spawned items.
            foreach (CollectionItem prevSpawn in SpawnedItems)
                GameObject.Destroy(prevSpawn.gameObject);

            SpawnedItems = new List<CollectionItem>(SpawnedItems.Count);

            await base.ShowAsync(hideOtherWindows);

            await this.myCollectionController.OnShowAsync();
            await UniTask.SwitchToMainThread();

            this.StatusText.text = "Collection loaded";
            Debug.Log("Loading collection finished");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Loading collection cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError("Error while loading collection: " + e.ToString());
        }
    }

    public async UniTask OnItemsLoadedAsync(List<NFTItem> items)
    {
        foreach (var item in items)
        {
            var collectionItem = CreateItem();
            UpdateCollectionItem(collectionItem, item);
        }

        this.ResizeContent();
    }

    public async UniTask OnItemUpdatedAsync(NFTItem item)
    {
        var collectionItem = SpawnedItems.Find((collectionItem) =>
        {
            return collectionItem.ContractAddr == item.ContractAddress
                    && collectionItem.NFTID == item.TokenID;
        });

        if (collectionItem != null)
        {
            UpdateCollectionItem(collectionItem, item);
        }
    }

    private void ResizeContent()
    {
        // Resize content transform
        RectTransform contentTransform = ContentGameObject.GetComponent<RectTransform>();
        GridLayoutGroup gridLayoutGroup = ContentGameObject.GetComponent<GridLayoutGroup>();
        float cellSize = gridLayoutGroup.cellSize.y;
        float spacing = gridLayoutGroup.spacing.y;
        int rows = (int)Math.Ceiling(((decimal)SpawnedItems.Count / 3));
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, (cellSize + spacing) * rows);
    }

    private CollectionItem CreateItem()
    {
        CollectionItem collectionItem = GameObject.Instantiate(CollectionCopyFromItem, ContentGameObject.transform);
        SpawnedItems.Add(collectionItem);
        collectionItem.gameObject.SetActive(true);
        return collectionItem;
    }

    private void UpdateCollectionItem(CollectionItem collectionItem, NFTItem item)
    {
        collectionItem.ContractAddr = item.ContractAddress;
        collectionItem.NFTID = item.TokenID;
        collectionItem.TitleText.text = item.Name;
        collectionItem.DescriptionText.text = item.Description;

        collectionItem.Send_Button.onClick.RemoveAllListeners();
        collectionItem.Send_Button.onClick.AddListener(async delegate
        {
            await NFTWalletWindowManager.Instance.SendNFTPopupWindow.ShowPopupAsync(item.ContractAddress, item.TokenID, async delegate(string destinationAddress)
            {
                await OnSendNFTConfirmedAsync(item.ContractAddress, item.TokenID, destinationAddress);
            });
        });

        if (item.ImageIsLoading)
        {
            collectionItem.NFTImage.sprite = this.LoadingImageSprite;
        }
        else if (item.ImageTexture != null)
        {
            var texture = item.ImageTexture;
            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            collectionItem.NFTImage.sprite = sprite;
        }
        else if (item.AnimationURL != null)
        {
            collectionItem.NFTImage.sprite = NoImageButAnimationSprite;
        }
        else
        {
            collectionItem.NFTImage.sprite = ImageNotAvailableSprite;
        }

        collectionItem.DisplayAnimationButton.gameObject.SetActive(item.AnimationURL != null);

        if (item.AnimationURL != null)
        {
            collectionItem.DisplayAnimationButton.onClick.RemoveAllListeners();
            collectionItem.DisplayAnimationButton.onClick.AddListener(async delegate
            {
                await NFTWalletWindowManager.Instance.AnimationWindow.ShowPopupAsync(item.AnimationURL, "NFTID: " + item.TokenID);
            });
        }
    }

    private async Task OnSendNFTConfirmedAsync(string contractAddress, long nftId, string destinationAddress)
    {
        Debug.Log(string.Format("OnSendNFTConfirmedAsync contractAddress: {0}, nftId: {1}, destinationAddress: {2}", contractAddress, nftId, destinationAddress));

        if (string.IsNullOrEmpty(contractAddress) || string.IsNullOrEmpty(destinationAddress))
        {
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Destination address or contract address is incorrect.", "ERROR");
            return;
        }

        try
        {
            // Check that address is a valid address
            var addr = new BitcoinPubKeyAddress(destinationAddress, NFTWallet.Instance.Network);
        }
        catch (FormatException)
        {
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Destination address is invalid.", "ERROR");
            return;
        }

        UInt256 sendId = UInt256.Parse(nftId.ToString());

        NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, contractAddress);

        Task<string> sendTask = wrapper.TransferFromAsync(NFTWallet.Instance.StratisUnityManager.GetAddress().ToString(), destinationAddress, sendId);

        ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(sendTask);

        bool success = receipt?.Success ?? false;

        string resultString = string.Format("NFT send success: {0}", success);

        if (success)
        {
            resultString += Environment.NewLine + "Item that was sent will disappear from your collection after refresh once backend indexes this nft contract.";
        }

        await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT SEND");
    }
}
