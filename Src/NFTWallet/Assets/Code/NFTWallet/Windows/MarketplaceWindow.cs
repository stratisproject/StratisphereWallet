using UnityEngine.UI;

public class MarketplaceWindow : WindowBase
{
    public InputField LoginData_InputField, TransferData_InputField;

    public Button Login_Button, Transfer_Button;

    void Awake()
    {
        this.Login_Button.onClick.AddListener(async delegate
        {
            string loginData = LoginData_InputField.text;
            LoginData_InputField.text = string.Empty;
            await MarketplaceIntegration.Instance.LogInToNFTMarketplaceAsync(loginData);
        });

        this.Transfer_Button.onClick.AddListener(async delegate
        {
            string transferData = TransferData_InputField.text;
            TransferData_InputField.text = string.Empty;
            await MarketplaceIntegration.Instance.ExecuteMarketplaceRequestAsync(transferData);
        });
    }
}
