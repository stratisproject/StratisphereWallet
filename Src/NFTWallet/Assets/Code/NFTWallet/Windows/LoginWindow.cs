using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NBitcoin;
using UnityEngine;
using UnityEngine.UI;

public class LoginWindow : WindowBase
{
    public InputField MnemonicInputField, PassphraseInputField;
    public Button GenerateNewMnemonicButton, LogInButton, RemovePlayerPrefsButton;
    public Text NewMnemonicWarningText;

    public Text MnemonicInputFieldPlaceholderText;

    public Dropdown NetworkDropDown;
    
    private const string MnemonicKey = "MnemonicST";

    private List<TargetNetwork> targetNetworks = new List<TargetNetwork>();

    private TargetNetwork selectedNetwork;

    void Start()
    {
        targetNetworks.Add(NFTWallet.Instance.DefaultNetwork);

        if (NFTWallet.Instance.DefaultNetwork == TargetNetwork.CirrusMain)
            targetNetworks.Add(TargetNetwork.CirrusTest);
        else
            targetNetworks.Add(TargetNetwork.CirrusMain);

        selectedNetwork = NFTWallet.Instance.DefaultNetwork;

        NetworkDropDown.ClearOptions();
        List<string> options = targetNetworks.Select(x => x.ToString()).ToList();
        NetworkDropDown.AddOptions(options);
    }

    async void Awake()
    {
        this.GenerateNewMnemonicButton.onClick.AddListener(delegate
        {
            Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            this.MnemonicInputField.text = mnemonic.ToString();
        });

        NetworkDropDown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedNetwork = targetNetworks[optionNumber];
        });

        this.LogInButton.onClick.AddListener(async delegate
        {
            bool presavedMnemonicExists = PlayerPrefs.HasKey(MnemonicKey);
            bool mnemonicEntered = !string.IsNullOrEmpty(MnemonicInputField.text);


            if (!presavedMnemonicExists && !mnemonicEntered)
            {
                // No mnemonic entered
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("You need to enter or generate mnemonic!", "ERROR");
                return;
            }

            string mnemonic = mnemonicEntered ? MnemonicInputField.text : PlayerPrefs.GetString(MnemonicKey);
            
            // Validate mnemonic
            try
            {
                new Mnemonic(mnemonic, Wordlist.English);
            }
            catch (Exception e)
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(e.Message, "INCORRECT MNEMONIC");
                return;
            }

            PlayerPrefs.SetString(MnemonicKey, mnemonic);

            string passphrase = this.PassphraseInputField.text;
            if (string.IsNullOrEmpty(passphrase))
                passphrase = null;

            this.PassphraseInputField.text = string.Empty;

            bool success = await NFTWallet.Instance.InitializeAsync(mnemonic, selectedNetwork, passphrase);

            if (success)
                await NFTWalletWindowManager.Instance.WalletWindow.ShowAsync();
            else
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Can't initialize NFT wallet. Probably can't reach API server.", "INITIALIZATION ERROR");

            await NFTWallet.Instance.AddKnownContractsIfMissingAsync();

            MnemonicInputField.text = string.Empty;
        });

        RemovePlayerPrefsButton.onClick.AddListener(delegate
        {
            PlayerPrefs.DeleteAll();
        });
    }

    public override UniTask ShowAsync(bool hideOtherWindows = true)
    {
        bool mnemonicExists = PlayerPrefs.HasKey(MnemonicKey);

        if (mnemonicExists)
        {
            // Mnemonic was saved before
            MnemonicInputFieldPlaceholderText.text = "Previously saved mnemonic was loaded, click log in.";
        }

        NewMnemonicWarningText.gameObject.SetActive(!mnemonicExists);

        return base.ShowAsync(hideOtherWindows);
    }
}
