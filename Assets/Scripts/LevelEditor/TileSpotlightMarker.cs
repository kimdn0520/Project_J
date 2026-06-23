using UnityEngine;

/// <summary>
/// 그리드 셀 위에 배치되는 스팟라이트 마커.
/// Light 컴포넌트를 통해 실제 조명을 제어하고, 썸네일 아이콘으로 에디터에서 위치를 시각화한다.
/// </summary>
[RequireComponent(typeof(Light))]
public class TileSpotlightMarker : MonoBehaviour
{
    [Header("Light Settings")]
    public Color lightColor = Color.white;
    public float intensity = 1.5f;
    public float range = 3f;

    private Light _light;

    private void Awake()
    {
        _light = GetComponent<Light>();
        ApplySettings();
    }

    public void Apply(Color color, float newIntensity, float newRange)
    {
        lightColor = color;
        intensity = newIntensity;
        range = newRange;
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (_light == null) return;
        _light.type = LightType.Point;
        _light.color = lightColor;
        _light.intensity = intensity;
        _light.range = range;
        _light.renderMode = LightRenderMode.ForcePixel;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.15f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
