using UnityEngine;
using UnityEngine.UI;

public static class Utility
{
    public static int RandomInt(int minInclusive, int maxExclusive)
    {
        return Random.Range(minInclusive, maxExclusive);
    }

    public static void SetResizeScale(this Image image)
    {
        image.rectTransform.localScale = Vector3.one;
        bool isStandardWidth = image.overrideSprite.rect.width > image.overrideSprite.rect.height;
        float ratio = 1f;
        if (isStandardWidth)
        {
            ratio = image.rectTransform.rect.width / image.overrideSprite.rect.width;
            float scale = (ratio * image.overrideSprite.rect.height) / image.rectTransform.rect.height;
            image.rectTransform.localScale = new Vector3(1.0f, scale, 1.0f);
        }
        else
        {
            ratio = image.rectTransform.rect.height / image.overrideSprite.rect.height;
            float scale = (ratio * image.overrideSprite.rect.width) / image.rectTransform.rect.width;
            image.rectTransform.localScale = new Vector3(scale, 1.0f, 1.0f);
        }
    }
}
