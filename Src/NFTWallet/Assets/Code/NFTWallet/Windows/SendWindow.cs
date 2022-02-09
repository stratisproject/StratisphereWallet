using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Stratis.SmartContracts;
using Unity3dApi;
using UnityEngine.UI;

public class SendWindow : WindowBase
{
    public Dropdown NFTContractSelect_Dropdown, NFTID_Dropdown;

    public InputField DestinationAddrInputField;

    public Button SendButton;

    private List<string> contractAddresses;

    private OwnedNFTsModel ownedNfts;
    private string selectedContract;

    private List<long> currentIdsCollection;
    private long selectedId;

    void Awake()
    {
        NFTContractSelect_Dropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedContract = contractAddresses[optionNumber];
            SetupAvailableIdsForSelectedContract();
        });

        NFTID_Dropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedId = currentIdsCollection[optionNumber];
        });

        SendButton.onClick.AddListener(async delegate
        {
            if (string.IsNullOrEmpty(selectedContract))
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("No contract or ID selected", "ERROR");
                return;
            }

            UInt256 sendId = UInt256.Parse(selectedId.ToString());
            string destAddr = DestinationAddrInputField.text;

            DestinationAddrInputField.text = string.Empty;

            NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, selectedContract);
            
            Task<string> sendTask = wrapper.TransferFromAsync(NFTWallet.Instance.StratisUnityManager.GetAddress().ToString(), destAddr, sendId);
            
            ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(sendTask);
            
            bool success = receipt.Success;
            
            string resultString = string.Format("NFT send success: {0}", success);
            
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "NFT SEND");
            
            this.ownedNfts.OwnedIDsByContractAddress.First(x => x.Key == selectedContract).Value.Remove((long)sendId);
            SetupAvailableIdsForSelectedContract();
        });
    }

    public override async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        await base.ShowAsync(hideOtherWindows);

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

        SetupAvailableIdsForSelectedContract();
    }
    
    private void SetupAvailableIdsForSelectedContract()
    {
        KeyValuePair<string, ICollection<long>> contractToIds = this.ownedNfts.OwnedIDsByContractAddress.FirstOrDefault(x => x.Key == selectedContract);

        if (contractToIds.Value != null && contractToIds.Value.Any())
        {
            NFTID_Dropdown.options.Clear();

            this.currentIdsCollection = contractToIds.Value.Distinct().ToList();
            List<string> options = this.currentIdsCollection.Select(x => x.ToString()).ToList();
            NFTID_Dropdown.AddOptions(options);

            selectedId = this.currentIdsCollection.First();
        }
    }
}
