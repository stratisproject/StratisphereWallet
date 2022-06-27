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
    public Button GenerateNewMnemonicButton, LogInButton, RemovePlayerPrefsButton, SizeUpButton, SizeDownButton;
    public Text NewMnemonicWarningText;

    public Text MnemonicInputFieldPlaceholderText;

    public Dropdown NetworkDropDown, LanguageSelectDropdown;

    private const string ResolutionKey = "ResolutionST";

    private List<TargetNetwork> targetNetworks = new List<TargetNetwork>();

    private TargetNetwork selectedNetwork;

    private Wordlist selectedWordlist = Wordlist.English;

    private readonly List<Vector2> SupportedResolutions = new List<Vector2>()
    {
        new Vector2(960 ,540),
        new Vector2(1024, 576),
        new Vector2(1280, 720),
        new Vector2(1366, 768),
        new Vector2(1600, 900),
        new Vector2(1920, 1080),
        new Vector2(2560, 1440),
        new Vector2(3840, 2160)
    };

    private int currentResolutionIndex = -1;

    public async UniTask InitializeAsync()
    {
        this.GenerateNewMnemonicButton.onClick.AddListener(delegate
        {
            Mnemonic mnemonic = new Mnemonic(selectedWordlist, WordCount.Twelve);
            this.MnemonicInputField.text = mnemonic.ToString();
        });

        this.SizeUpButton.onClick.AddListener(async delegate
        {
            if (currentResolutionIndex < SupportedResolutions.Count - 1)
            {
                currentResolutionIndex++;
                SetResolutionFromIndex();
            }
        });

        this.SizeDownButton.onClick.AddListener(async delegate
        {
            if (currentResolutionIndex > 0)
            {
                currentResolutionIndex--;
                SetResolutionFromIndex();
            }
        });

        NetworkDropDown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedNetwork = targetNetworks[optionNumber];
        });

        LanguageSelectDropdown.onValueChanged.AddListener(delegate (int optionNumber)
        {
            selectedWordlist = MnemonicLanguages.GetWordlistByLanguage((WorldistLanguage)optionNumber);
        });

        this.LogInButton.onClick.AddListener(async delegate
        {
            await this.LogInAsync();
        });

        RemovePlayerPrefsButton.onClick.AddListener(delegate
        {
            PlayerPrefs.DeleteAll();
        });

        targetNetworks.Add(NFTWallet.Instance.DefaultNetwork);

        if (NFTWallet.Instance.DefaultNetwork == TargetNetwork.CirrusMain)
            targetNetworks.Add(TargetNetwork.CirrusTest);
        else
            targetNetworks.Add(TargetNetwork.CirrusMain);

        selectedNetwork = NFTWallet.Instance.DefaultNetwork;

        NetworkDropDown.ClearOptions();
        List<string> options = targetNetworks.Select(x => x.ToString()).ToList();
        NetworkDropDown.AddOptions(options);

        #if !UNITY_ANDROID && !UNITY_IPHONE
                if (PlayerPrefs.HasKey(ResolutionKey) && PlayerPrefs.GetInt(ResolutionKey) >= 0)
                {
                    currentResolutionIndex = PlayerPrefs.GetInt(ResolutionKey);
                }
                else
                {
                    currentResolutionIndex = 4;
                }

                SetResolutionFromIndex();
        #endif

        LanguageSelectDropdown.ClearOptions();
        List<string> languages = Enum.GetValues(typeof(WorldistLanguage)).Cast<WorldistLanguage>().Select(x => x.ToString()).ToList();
        LanguageSelectDropdown.AddOptions(languages);
    }

    public override UniTask ShowAsync(bool hideOtherWindows = true)
    {
        bool mnemonicExists = NFTWallet.Instance.IsMnemonicSaved();

        if (mnemonicExists)
        {
            // Mnemonic was saved before
            MnemonicInputFieldPlaceholderText.text = "Previously saved mnemonic was loaded, click log in.";
        }

        NewMnemonicWarningText.gameObject.SetActive(!mnemonicExists);

        return base.ShowAsync(hideOtherWindows);
    }

    private void SetResolutionFromIndex()
    {
        Screen.SetResolution((int)SupportedResolutions[currentResolutionIndex].x, (int)SupportedResolutions[currentResolutionIndex].y, false);
    }

    // mnemonic should exist
    public async UniTask LogInAsync()
    {
        bool presavedMnemonicExists = NFTWallet.Instance.IsMnemonicSaved();
        bool mnemonicEntered = !string.IsNullOrEmpty(MnemonicInputField.text);

        PlayerPrefs.SetInt(ResolutionKey, currentResolutionIndex);

        if (!presavedMnemonicExists && !mnemonicEntered)
        {
            // No mnemonic entered
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("You need to enter or generate mnemonic!", "ERROR");
            return;
        }

        string mnemonic = mnemonicEntered ? MnemonicInputField.text : NFTWallet.Instance.GetSavedMnemonic();

        // Validate mnemonic
        try
        {
            Wordlist wordlist = Wordlist.AutoDetect(mnemonic);
            new Mnemonic(mnemonic, wordlist);
        }
        catch (Exception e)
        {
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(e.Message, "INCORRECT MNEMONIC");
            return;
        }

        NFTWallet.Instance.SaveMnemonic(mnemonic);

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
    }
}
