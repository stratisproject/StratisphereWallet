using NBitcoin;
using UnityEngine;
using UnityEngine.UI;

public class DisplayMnemonicWindow : WindowBase
{
    public Button BackButton, CopyButton, ContinueButton;

    public Text[] TwelveWordsTexts;

    private string generatedMnemonic;

    void Awake()
    {
        BackButton.onClick.AddListener(async () =>
        {
            await NFTWalletWindowManager.Instance.WelcomeWindow.ShowAsync();
        });

        CopyButton.onClick.AddListener(async () =>
        {
            GUIUtility.systemCopyBuffer = generatedMnemonic;
        });

        ContinueButton.onClick.AddListener(async () =>
        {
            NFTWalletWindowManager.Instance.CheckMnemonicWindow.SetMnemonic(this.generatedMnemonic);
            await NFTWalletWindowManager.Instance.CheckMnemonicWindow.ShowAsync();
        });
    }

    public void GenerateAndSetNewMnemonic()
    {
        Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
        generatedMnemonic = mnemonic.ToString();

        string[] words = generatedMnemonic.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            TwelveWordsTexts[i].text = words[i];
        }
    }
}
