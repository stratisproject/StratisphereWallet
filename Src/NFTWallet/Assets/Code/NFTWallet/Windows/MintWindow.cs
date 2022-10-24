using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using StratisNodeApi;
using UnityEngine;
using UnityEngine.UI;

public class MintWindow : WindowBase
{
    public Dropdown NFTContractSelect_Dropdown, SelectCategory_Dropdown;

    public Button MintButton, TrackButton, CopySelectedContractButton, UntrackAllButton;

    public InputField MintToAddrInputField, UriInputField, DescriptionInputField, AttributesInputField, TrackContractInputField, AnimationInputField, NameInputField;

    private List<DeployedNFTModel> nftsForDeployment;

    private DeployedNFTModel selectedNft;

    private readonly List<string> categories = new List<string>() { "General", "Games", "Collectibles", "Art", "Photography", "Sports" };

    private string selectedCategory;

    void Awake()
    {
        selectedCategory = categories.First();

        SelectCategory_Dropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedCategory = categories[optionNumber];
        });

        NFTContractSelect_Dropdown.onValueChanged.AddListener(delegate(int optionNumber)
        {
            selectedNft = nftsForDeployment[optionNumber];
        });

        SelectCategory_Dropdown.ClearOptions();
        SelectCategory_Dropdown.AddOptions(categories);

        MintButton.onClick.AddListener(async delegate
        {
            string name = NameInputField.text;
            NameInputField.text = string.Empty;

            if (string.IsNullOrEmpty(name))
                name = selectedNft.NftName;

            string mintToAddr = MintToAddrInputField.text;

            string description = DescriptionInputField.text;
            DescriptionInputField.text = string.Empty;

            string uri = UriInputField.text;
            UriInputField.text = string.Empty;

            string animationUri = AnimationInputField.text;
            AnimationInputField.text = string.Empty;

            List<Attribute> attributesCollection = new List<Attribute>();
            string[] attrStr = AttributesInputField.text.Split(',');
            AttributesInputField.text = string.Empty;

            foreach (string s in attrStr)
            {
                if (!s.Contains("-"))
                    continue;

                string[] nameVal = s.Split('-');

                if (nameVal.Length != 2)
                    continue;

                attributesCollection.Add(new Attribute() { TraitType = nameVal[0], Value = nameVal[1]});
            }

            string jsonUri = null;

            try
            {
                jsonUri = await MarketplaceIntegration.Instance.UploadMetadataAsync(new NFTMetadataModel()
                {
                    Name = name,
                    Image = uri,
                    Description = description,
                    Attributes = attributesCollection.ToArray(),
                    Category = selectedCategory,
                    AnimationUrl = animationUri
                });
            }
            catch (Exception e)
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Failed to upload json metadata. Maybe server is down.", "NFT MINT");
                return;
            }

            NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, selectedNft.ContractAddress);
            Task<string> mintNftTask = wrapper.MintAsync(mintToAddr, jsonUri);

            ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(mintNftTask);

            bool success = receipt.Success;
            string nftId = receipt.ReturnValue;

            string resultString = string.Format("NFT mint success: {0}.{2}Minted NFT ID: {1}{2}If you've minted it to your address then this NFT will be shown in MY COLLECTION window.", success, nftId, Environment.NewLine);

            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT MINT");
        });

        TrackButton.onClick.AddListener(async delegate
        {
            string contractAddr = TrackContractInputField.text;
            TrackContractInputField.text = string.Empty;

            bool success = await NFTWallet.Instance.RegisterKnownNFTAsync(contractAddr);

            if (!success)
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Error: invalid NFT contract address.", "ERROR");
            else
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("NFT was added to watch list.", "NFT WATCH");
        });

        CopySelectedContractButton.onClick.AddListener(delegate
        {
            string contrAddr = selectedNft?.ContractAddress;

            GUIUtility.systemCopyBuffer = contrAddr;
        });

        UntrackAllButton.onClick.AddListener(async delegate
        {
            NFTWallet.Instance.PersistKnownNfts(new List<DeployedNFTModel>());

            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Cleared");
        });
    }

    public override UniTask ShowAsync(bool hideOtherWindows = true)
    {
        string myAddress = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();
        nftsForDeployment = NFTWallet.Instance.LoadKnownNfts().Where(x => x.OwnerOnlyMinting == null || (!x.OwnerOnlyMinting.Value || x.OwnerAddress == myAddress)).ToList();
        selectedNft = nftsForDeployment.FirstOrDefault();

        NFTContractSelect_Dropdown.ClearOptions();
        List<string> options = nftsForDeployment.Select(x => string.Format("{0} ({1})  -  {2}", x.NftName, x.Symbol, x.ContractAddress)).ToList();
        NFTContractSelect_Dropdown.AddOptions(options);

        MintToAddrInputField.text = myAddress;

        return base.ShowAsync(hideOtherWindows);
    }
}
