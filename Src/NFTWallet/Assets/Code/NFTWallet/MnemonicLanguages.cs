using NBitcoin;

public static class MnemonicLanguages
{
    public static Wordlist GetWordlistByLanguage(WorldistLanguage language)
    {
        switch (language) {
            case WorldistLanguage.English: return Wordlist.English;
            case WorldistLanguage.French: return Wordlist.French;
            case WorldistLanguage.Spanish: return Wordlist.Spanish;
            case WorldistLanguage.ChineseTraditional: return Wordlist.ChineseTraditional;
            case WorldistLanguage.ChineseSimplified: return Wordlist.ChineseSimplified;
            case WorldistLanguage.Japanese: return Wordlist.Japanese;
        }

        return Wordlist.English;
    }
}

public enum WorldistLanguage
{
    English, French, Spanish, ChineseTraditional, ChineseSimplified, Japanese
}