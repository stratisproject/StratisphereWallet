using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Video;

public class DisplayAnimationWIndow : WindowBase
{
    public Button Close;
    public Text InfoText;

    public RawImage Image;

    public VideoPlayer Player;

    public async void Awake()
    {
        this.Close.onClick.AddListener(async delegate { await this.HideAsync(); });
    }

    public async UniTask ShowPopupAsync(string resourceUri, string info)
    {
        //if (!resourceUri.EndsWith(".mp4"))
        //{
        //    InfoText.text = "UNSUPPORTED MEDIA TYPE";
        //    await this.ShowAsync(false);
        //    return;
        //}

        Image.gameObject.SetActive(true);
        Player.url = resourceUri;
        Player.isLooping = true;
        InfoText.text = info;

        await this.ShowAsync(false);
    }

    public override UniTask HideAsync()
    {
        Image.gameObject.SetActive(false);
        return base.HideAsync();
    }
}
