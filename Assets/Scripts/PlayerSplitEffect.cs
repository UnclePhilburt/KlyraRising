using UnityEngine;

public class PlayerSplitEffect : MonoBehaviour
{
    [Header("Split Settings")]
    [SerializeField] private float _splitForce = 5f;
    [SerializeField] private float _torqueForce = 10f;
    [SerializeField] private float _upwardForce = 3f;

    [Header("Upper Body Bones (will be hidden on lower half)")]
    [SerializeField] private string[] _upperBodyBones = new string[]
    {
        "Spine", "Chest", "Neck", "Head",
        "Shoulder", "Arm", "Hand", "Finger",
        "Clavicle"
    };

    [Header("Lower Body Bones (will be hidden on upper half)")]
    [SerializeField] private string[] _lowerBodyBones = new string[]
    {
        "Hips", "Pelvis", "Leg", "Thigh", "Calf", "Foot", "Toe", "UpLeg"
    };

    /// <summary>
    /// Call this to split the player in half. Returns the two halves.
    /// </summary>
    public static (GameObject upperHalf, GameObject lowerHalf) Split(GameObject player, float splitForce = 5f)
    {
        // Deactivate player first so clones are also inactive (prevents Awake from running)
        bool wasActive = player.activeSelf;
        player.SetActive(false);

        // Create two copies (inactive, so Awake doesn't run yet)
        GameObject upperHalf = Instantiate(player, player.transform.position, player.transform.rotation);
        GameObject lowerHalf = Instantiate(player, player.transform.position, player.transform.rotation);

        upperHalf.name = "PlayerUpper";
        lowerHalf.name = "PlayerLower";

        // Remove Photon components BEFORE activating (prevents ViewID conflicts)
        DestroyPhotonComponents(upperHalf);
        DestroyPhotonComponents(lowerHalf);

        // Disable player scripts
        DisablePlayerScripts(upperHalf);
        DisablePlayerScripts(lowerHalf);

        // Now activate the clones
        upperHalf.SetActive(true);
        lowerHalf.SetActive(true);

        // Keep original inactive (it's "dead")
        // player.SetActive(wasActive); // Don't reactivate - player is dead

        // Hide lower body on upper half
        HideBones(upperHalf, new string[] { "Hips", "Pelvis", "Leg", "Thigh", "Calf", "Foot", "Toe", "UpLeg" });

        // Hide upper body on lower half (keep hips as anchor)
        HideBones(lowerHalf, new string[] { "Spine", "Chest", "Neck", "Head", "Shoulder", "Arm", "Hand", "Finger", "Clavicle" });

        // Add rigidbodies for physics
        Rigidbody upperRb = upperHalf.AddComponent<Rigidbody>();
        Rigidbody lowerRb = lowerHalf.AddComponent<Rigidbody>();

        // Apply forces - upper body flies up and back
        Vector3 backDir = -player.transform.forward;
        upperRb.AddForce((Vector3.up * 3f + backDir * splitForce), ForceMode.VelocityChange);
        upperRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

        // Lower body crumples forward slightly
        lowerRb.AddForce((Vector3.down * 0.5f + player.transform.forward * 1f), ForceMode.VelocityChange);

        // Add colliders for ground collision
        AddSimpleCollider(upperHalf);
        AddSimpleCollider(lowerHalf);

        // Original player is already inactive from earlier
        // Destroy halves after a while
        Destroy(upperHalf, 10f);
        Destroy(lowerHalf, 10f);

        return (upperHalf, lowerHalf);
    }

    static void DestroyPhotonComponents(GameObject obj)
    {
        // Destroy all Photon networking components to prevent ID conflicts
        var photonViews = obj.GetComponentsInChildren<Photon.Pun.PhotonView>(true);
        foreach (var pv in photonViews)
        {
            Object.DestroyImmediate(pv);
        }

        var photonTransforms = obj.GetComponentsInChildren<Photon.Pun.PhotonTransformView>(true);
        foreach (var pt in photonTransforms)
        {
            Object.DestroyImmediate(pt);
        }

        var photonAnimators = obj.GetComponentsInChildren<Photon.Pun.PhotonAnimatorView>(true);
        foreach (var pa in photonAnimators)
        {
            Object.DestroyImmediate(pa);
        }
    }

    static void DisablePlayerScripts(GameObject obj)
    {
        // Disable common player scripts
        var controller = obj.GetComponent<ThirdPersonController>();
        if (controller != null) Object.DestroyImmediate(controller);

        var weapon = obj.GetComponent<WeaponController>();
        if (weapon != null) Object.DestroyImmediate(weapon);

        var animator = obj.GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;

        // Destroy any other components that might cause issues
        var charController = obj.GetComponent<CharacterController>();
        if (charController != null) Object.DestroyImmediate(charController);
    }

    static void HideBones(GameObject obj, string[] boneNames)
    {
        // For SkinnedMeshRenderer characters, we need to scale bones to zero
        // This collapses vertices weighted to those bones
        Transform[] allTransforms = obj.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            foreach (string boneName in boneNames)
            {
                if (t.name.ToLower().Contains(boneName.ToLower()))
                {
                    // Scale bone to zero - this collapses the skinned mesh vertices
                    t.localScale = Vector3.zero;
                    break;
                }
            }
        }
    }

    static void AddSimpleCollider(GameObject obj)
    {
        // Add a capsule collider for simple physics
        CapsuleCollider col = obj.AddComponent<CapsuleCollider>();
        col.height = 1f;
        col.radius = 0.3f;
        col.center = new Vector3(0, 0.5f, 0);
    }

    // Convenience method to split from BossIntro
    public static void SplitPlayer(GameObject player)
    {
        Split(player, 5f);
    }
}
