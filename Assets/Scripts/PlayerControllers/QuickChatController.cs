using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class QuickChatController : MonoBehaviour {
    public event Action ChatComplete;

    [SerializeField] SpriteRenderer _emojiSpriteRenderer;
    [SerializeField] SpriteRenderer _chatBubbleSpriteRenderer;
    [SerializeField] TextMeshPro _chatText;

    bool _isAddingRichTextTag;


    public void SetEmoji(Sprite emojiSprite) {
        _emojiSpriteRenderer.sprite = emojiSprite;
    }

    public void ClearEmoji() {
        _emojiSpriteRenderer.sprite = null;
    }

    public IEnumerator SetChatBubble(float typingSpeed, string chatText) {
        _chatBubbleSpriteRenderer.enabled = true;
        _chatText.text = chatText;
        _chatText.maxVisibleCharacters = 0;
        foreach (char letter in chatText.ToCharArray()) {
            if (letter == '<' || _isAddingRichTextTag) {
                _isAddingRichTextTag = letter != '>';
            } else {
                _chatText.maxVisibleCharacters++;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
    }

    public void ClearChatBubble() {
        _chatBubbleSpriteRenderer.enabled = false;
        _chatText.text = "";
    }
}
