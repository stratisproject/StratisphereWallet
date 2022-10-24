using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using StratisNodeApi;
using UnityEngine;
using UnityEngine.UI;

public class CreateNFTWindow : WindowBase
{
    public InputField NameInputField, SymbolInputField, RoyaltyAddressInputField, RoyaltyPercentageInputField;

    public Toggle OwnerOnlyMintingToggle;

    public Button DeployButton;

    async void Awake()
    {
        this.DeployButton.onClick.AddListener(async delegate
        {
            string nftName = NameInputField.text;
            string symbol = SymbolInputField.text;
            bool ownerOnlyMinting = OwnerOnlyMintingToggle.isOn;

            string royaltyAddress = RoyaltyAddressInputField.text;

            if (!double.TryParse(RoyaltyPercentageInputField.text, out double royaltyPercentage))
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Royalty percentage value is incorrect.", "NFT DEPLOYMENT");
                return;
            }

            NameInputField.text = SymbolInputField.text = string.Empty;

            Task<string> deployNFTContract = NFTWrapper.DeployNFTContractAsync(NFTWallet.Instance.StratisUnityManager, nftName, symbol, ownerOnlyMinting, royaltyAddress, royaltyPercentage);

            ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(deployNFTContract);

            string resultString = "NFT deployment complete. Success: " + receipt.Success + Environment.NewLine + "NFT contract address: " + receipt.NewContractAddress +
                                  Environment.NewLine  + Environment.NewLine + "NFT contract address that you've just deployed was added to the MINT window." +
                                  Environment.NewLine + "You now can mint NFT in the MINT window.";

            Debug.Log(resultString);

            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT DEPLOYMENT");

            if (receipt.Success)
                await NFTWallet.Instance.RegisterKnownNFTAsync(nftName, symbol, ownerOnlyMinting, receipt.NewContractAddress, NFTWallet.Instance.StratisUnityManager.GetAddress().ToString());
        });
    }

    public override UniTask ShowAsync(bool hideOtherWindows = true)
    {
        RoyaltyAddressInputField.text = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();

        return base.ShowAsync(hideOtherWindows);
    }
}
