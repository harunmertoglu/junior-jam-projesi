using UnityEngine;

// Karakterin tam altına yuvarlak gölge koyar, oyuncu nereye ineceğini görür.
// Player root'una eklenir. Gölge objesi Player'ın child'ı olabilir ama Visual'ın child'ı olmamalı.
public class PlayerGroundShadow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform shadowTransform;

    [Header("Shadow")]
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float maxShadowDistance = 30f;
    // Zeminle aynı yükseklikte titreşmesin diye biraz yukarıda
    [SerializeField] float shadowHeightOffset = 0.02f;

    void LateUpdate()
    {
        SetShadowPosition();
    }

    void SetShadowPosition()
    {
        RaycastHit groundHit;
        bool hitGround = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out groundHit, maxShadowDistance, groundLayers, QueryTriggerInteraction.Ignore);

        shadowTransform.gameObject.SetActive(hitGround);

        if (hitGround)
        {
            shadowTransform.position = groundHit.point + Vector3.up * shadowHeightOffset;
        }
    }
}
