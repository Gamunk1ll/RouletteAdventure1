using System.Collections;
using UnityEngine;

public class BattleShopStageController : MonoBehaviour
{
    public Transform stageTransform;
    public Vector3 battleEuler = Vector3.zero;
    public Vector3 shopEuler = new Vector3(0f, 180f, 0f);

    public float flipDuration = 0.65f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public GameObject battleSideObjects;
    public GameObject shopSideObjects;

    private Coroutine flipRoutine;

    private void Awake()
    {
        ApplyBattleStateImmediate();
    }

    public void ApplyBattleStateImmediate()
    {
        if (stageTransform != null)
            stageTransform.localRotation = Quaternion.Euler(battleEuler);

        if (battleSideObjects != null)
            battleSideObjects.SetActive(true);

        if (shopSideObjects != null)
            shopSideObjects.SetActive(false);
    }

    public void ApplyShopStateImmediate()
    {
        if (stageTransform != null)
            stageTransform.localRotation = Quaternion.Euler(shopEuler);

        if (battleSideObjects != null)
            battleSideObjects.SetActive(false);

        if (shopSideObjects != null)
            shopSideObjects.SetActive(true);
    }

    public void FlipToShop()
    {
        StartFlip(shopEuler, false, true);
    }

    public void FlipToBattle()
    {
        StartFlip(battleEuler, true, false);
    }

    private void StartFlip(Vector3 targetEuler, bool battleActive, bool shopActive)
    {
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);

        flipRoutine = StartCoroutine(FlipRoutine(targetEuler, battleActive, shopActive));
    }

    private IEnumerator FlipRoutine(Vector3 targetEuler, bool battleActive, bool shopActive)
    {
        if (stageTransform == null)
            yield break;

        Quaternion start = stageTransform.localRotation;
        Quaternion target = Quaternion.Euler(targetEuler);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);
            float eased = flipCurve != null ? flipCurve.Evaluate(t) : t;
            stageTransform.localRotation = Quaternion.Slerp(start, target, eased);
            yield return null;
        }

        stageTransform.localRotation = target;

        if (battleSideObjects != null)
            battleSideObjects.SetActive(battleActive);

        if (shopSideObjects != null)
            shopSideObjects.SetActive(shopActive);

        flipRoutine = null;
    }
}
