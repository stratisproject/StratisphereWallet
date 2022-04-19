using System.Collections.Generic;
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
        string mp4AnimationUri = resourceUri;

        if (!resourceUri.EndsWith(".mp4"))
        {
            InfoText.text = "Converting animation to mp4...";
            
            IDictionary<string, string> result = await MediaConverterManager.Instance.ConvertLinksAsync(new List<string>() {resourceUri});

            if (!result.TryGetValue(resourceUri, out mp4AnimationUri))
            {
                InfoText.text = "Conversion failed.";
                return;
            }
        }

        Image.gameObject.SetActive(true);
        Player.url = mp4AnimationUri;
        InfoText.text = info;

        await this.ShowAsync(false);
    }

    public override UniTask HideAsync()
    {
        Image.gameObject.SetActive(false);
        return base.HideAsync();
    }
}
