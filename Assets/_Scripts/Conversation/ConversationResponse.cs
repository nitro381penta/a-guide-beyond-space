using System;

[Serializable]
public class ConversationResponse
{
    public string transcript;
    public string display_text;
    public string answer_text;
    public string audio_base64;
}