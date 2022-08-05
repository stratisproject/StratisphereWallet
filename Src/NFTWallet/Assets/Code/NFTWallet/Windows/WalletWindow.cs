using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NBitcoin;
using Stratis.SmartContracts;
using Unity3dApi;
using UnityEngine;
using UnityEngine.UI;

public class WalletWindow : WindowBase
{
    public Button CopyAddressButton, RefreshBalanceButton, SendTxButton, FaucetButton, QRButton;

    public Text AddressText, BalanceText;

    public InputField DestinationAddressInputField, AmountInputField;

    // ==== send window elements
    public Dropdown NFTContractSelect_Dropdown;

    public InputField SendIdInputField, DestinationAddrInputField;

    public Text OwnedIdsText;

    public Button SendButton;

    private List<string> contractAddresses;

    private OwnedNFTsModel ownedNfts;
    private string selectedContract;

    void Awake()
    {
        CopyAddressButton.onClick.AddListener(delegate { GUIUtility.systemCopyBuffer = this.AddressText.text; });
        RefreshBalanceButton.onClick.AddListener(async delegate { await this.RefreshBalanceAsync(); });

        SendTxButton.onClick.AddListener(async delegate
        {
            try
            {
                string destAddress = this.DestinationAddressInputField.text;
                Money amount = new Money(Decimal.Parse(this.AmountInputField.text), MoneyUnit.BTC);

                this.DestinationAddressInputField.text = this.AmountInputField.text = string.Empty;

                if (destAddress == NFTWallet.Instance.StratisUnityManager.GetAddress().ToString())
                {
                    await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Destination address can't be your address.", "ERROR");
                    return;
                }

                long currentBalanceSat = await NFTWallet.Instance.StratisUnityManager.Client.GetAddressBalanceAsync(destAddress);

                Task<string> sendTxTask = NFTWallet.Instance.StratisUnityManager.SendTransactionAsync(destAddress, amount);

                await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilDestBalanceChanges(destAddress, currentBalanceSat, sendTxTask);

                await this.RefreshBalanceAsync();
            }
            catch (Exception e)
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(e.ToString(), "ERROR");
            }
        });

        FaucetButton.onClick.AddListener(async delegate
        {
            string faucetReceivedKey = "faucetReceived";

            bool faucetReceived = false;

            if (PlayerPrefs.HasKey(faucetReceivedKey))
                faucetReceived = PlayerPrefs.GetInt(faucetReceivedKey) != 0;

            if (faucetReceived)
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("You've already received faucet funds.", "ERROR");
                return;
            }

            string faucetMnemonic = "matter solar quote boat resource peasant home resource sail damage tonight silent";
            Money faucetAmount = new Money(5, MoneyUnit.BTC);
            string destAddr = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();

            StratisUnityManager stratisUnityManager = new StratisUnityManager(new Unity3dClient(NFTWallet.Instance.ApiUrl), NFTWallet.Instance.Network,
                new Mnemonic(faucetMnemonic, Wordlist.English));

            Task<string> sendTxTask = stratisUnityManager.SendTransactionAsync(destAddr, faucetAmount);

            long currentBalanceSat = await NFTWallet.Instance.StratisUnityManager.Client.GetAddressBalanceAsync(destAddr);

            await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilDestBalanceChanges(destAddr, currentBalanceSat, sendTxTask);

            await this.RefreshBalanceAsync();

            PlayerPrefs.SetInt(faucetReceivedKey, 1);
        });

        QRButton.onClick.AddListener(async delegate
        {
            await NFTWalletWindowManager.Instance.QRWindow.ShowPopupAsync(NFTWallet.Instance.StratisUnityManager.GetAddress().ToString());
        });

        // ==== Send window elements
        OwnedIdsText.text = "Owned IDs: ";

        NFTContractSelect_Dropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedContract = contractAddresses[optionNumber];
            DisplayAvailableIdsForSelectedContract();
        });

        SendButton.onClick.AddListener(async delegate
        {
            if (string.IsNullOrEmpty(selectedContract) || string.IsNullOrEmpty(SendIdInputField.text) || string.IsNullOrEmpty(DestinationAddrInputField.text))
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("No contract or ID or dest address selected", "ERROR");
                return;
            }

            UInt256 sendId = UInt256.Parse(SendIdInputField.text);
            string destAddr = DestinationAddrInputField.text;

            DestinationAddrInputField.text = SendIdInputField.text = string.Empty;


            NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, selectedContract);

            Task<string> sendTask = wrapper.TransferFromAsync(NFTWallet.Instance.StratisUnityManager.GetAddress().ToString(), destAddr, sendId);

            ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(sendTask);

            bool success = receipt?.Success ?? false;

            string resultString = string.Format("NFT send success: {0}", success);

            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT SEND");

            this.ownedNfts.OwnedIDsByContractAddress.First(x => x.Key == selectedContract).Value.Remove((long)sendId);
            DisplayAvailableIdsForSelectedContract();
        });
    }

    public override async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        this.AddressText.text = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();

        await base.ShowAsync(hideOtherWindows);

        await this.RefreshBalanceAsync();

        // === send window
        string myAddress = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();
        ownedNfts = await NFTWallet.Instance.StratisUnityManager.Client.GetOwnedNftsAsync(myAddress);

        this.contractAddresses = ownedNfts.OwnedIDsByContractAddress.Keys.ToList();
        this.selectedContract = this.contractAddresses.FirstOrDefault();

        List<DeployedNFTModel> knownContracts = NFTWallet.Instance.LoadKnownNfts();
        List<string> options = new List<string>();

        foreach (string contractAddress in contractAddresses)
        {
            string option = contractAddress;

            DeployedNFTModel knownContract = knownContracts.FirstOrDefault(x => x.ContractAddress == contractAddress);

            if (knownContract != null)
                option += string.Format("   : {0} ({1})", knownContract.NftName, knownContract.Symbol);

            options.Add(option);
        }

        NFTContractSelect_Dropdown.ClearOptions();
        NFTContractSelect_Dropdown.AddOptions(options);

        DisplayAvailableIdsForSelectedContract();
    }

    private async UniTask RefreshBalanceAsync()
    {
        this.BalanceText.text = (await NFTWallet.Instance.StratisUnityManager.GetBalanceAsync()).ToString();
    }

    private void DisplayAvailableIdsForSelectedContract()
    {
        KeyValuePair<string, ICollection<long>> contractToIds = this.ownedNfts.OwnedIDsByContractAddress.FirstOrDefault(x => x.Key == selectedContract);

        string idsString;
        if (contractToIds.Value == null || !contractToIds.Value.Any())
            idsString = "You don't own any NFTs of that type.";
        else idsString = string.Join(",", contractToIds.Value.Distinct().OrderBy(x => x));

        OwnedIdsText.text = "OwnedIDs:" + idsString;
    }
}
