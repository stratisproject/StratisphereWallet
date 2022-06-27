using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CheckMnemonicWindow : WindowBase
{
    public Button BackButton, ContinueButton;

    public InputField[] WordInputFields;

    public Text[] PlaceholderTexts;

    private string mnemonic;

    private System.Random r = new System.Random();

    private List<int> wordIndexesToCheck;

    void Awake()
    {
        BackButton.onClick.AddListener(async () =>
        {
            await NFTWalletWindowManager.Instance.DisplayMnemonicWindow.ShowAsync();
        });

        ContinueButton.onClick.AddListener(async () =>
        {
            await this.VerifyWordsAndLoginAsync();
        });
    }

    public void SetMnemonic(string mnemonic)
    {
        this.mnemonic = mnemonic;
    }

    public override UniTask ShowAsync(bool hideOtherWindows = true)
    {
        foreach (InputField inputField in WordInputFields)
            inputField.text = string.Empty;

        this.wordIndexesToCheck = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            int number = r.Next(0, 11);

            if (wordIndexesToCheck.Contains(number))
                i--;
            else
                wordIndexesToCheck.Add(number);
        }

        wordIndexesToCheck = wordIndexesToCheck.OrderBy(x => x).ToList();

        for (int i = 0; i < PlaceholderTexts.Length; i++)
        {
            PlaceholderTexts[i].text = "Word #" + (wordIndexesToCheck[i] + 1).ToString();
        }

        return base.ShowAsync(hideOtherWindows);
    }

    private async UniTask VerifyWordsAndLoginAsync()
    {
        string[] words = mnemonic.Split(' ');

        for (int i = 0; i < wordIndexesToCheck.Count; i++)
        {
            int wordNumberToCheck = wordIndexesToCheck[i];

            string wordToCompare = WordInputFields[i].text;

            bool successCompare = words[wordNumberToCheck] == wordToCompare;

            if (!successCompare)
            {
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Words dont match.", "Error");
                return;
            }
        }

        Debug.Log("Words checked");

        NFTWallet.Instance.SaveMnemonic(mnemonic);

        bool success = await NFTWallet.Instance.InitializeAsync(mnemonic, NFTWallet.Instance.DefaultNetwork);

        if (success)
            await NFTWalletWindowManager.Instance.WalletWindow.ShowAsync();
        else
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Can't initialize NFT wallet. Probably can't reach API server.", "INITIALIZATION ERROR");

        await NFTWallet.Instance.AddKnownContractsIfMissingAsync();
    }
}
