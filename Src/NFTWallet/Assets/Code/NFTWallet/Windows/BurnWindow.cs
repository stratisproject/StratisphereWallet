using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Stratis.SmartContracts;
using StratisNodeApi;
using UnityEngine.UI;

public class BurnWindow : WindowBase
{
    public Dropdown NFTContractSelect_Dropdown;

    public InputField BurnIdInputField;

    public Text OwnedIdsText;

    public Button BurnButton;

    private List<string> contractAddresses;

    private List<BlockCoreApi.OwnedNFTItem> ownedNfts;
    private string selectedContract;

    void Awake()
    {
        NFTContractSelect_Dropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedContract = contractAddresses[optionNumber];
            DisplayAvailableIdsForSelectedContract();
        });

        BurnButton.onClick.AddListener(async delegate
        {
            if (string.IsNullOrEmpty(selectedContract) || string.IsNullOrEmpty(BurnIdInputField.text))
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("No contract or ID selected", "ERROR");
                return;
            }

            UInt256 burnId = UInt256.Parse(BurnIdInputField.text);
            BurnIdInputField.text = string.Empty;

            NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, selectedContract);

            Task<string> burnTask = wrapper.BurnAsync(burnId);

            ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(burnTask);
            
            bool success = receipt.Success;
            
            string resultString = string.Format("NFT burn success: {0}", success);
            
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT BURN");

            this.ownedNfts.Remove(this.ownedNfts.Single(x => x.contractId == selectedContract && x.id == burnId));

            DisplayAvailableIdsForSelectedContract();
        });
    }

    public override async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        await base.ShowAsync(hideOtherWindows);

        string myAddress = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString();

        this.ownedNfts = await NFTWallet.Instance.GetBlockCoreApi().GetOwnedNFTIds(myAddress);

        //this.LogAvailableIds();

        this.contractAddresses = ownedNfts.Select(x => x.contractId).Distinct().ToList();
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

    private void DisplayAvailableIdsForSelectedContract()
    {
        List<long> ownedIds = this.ownedNfts.Where(x => x.contractId == selectedContract).Select(x => x.id).ToList();
        
        if (ownedIds.Count == 0)
            OwnedIdsText.text = "You don't own any NFTs of that type.";
        else
            OwnedIdsText.text = "OwnedIDs:" + string.Join(",", ownedIds.OrderBy(x => x));
    }
}
