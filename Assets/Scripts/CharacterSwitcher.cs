using UnityEngine;
using System.Collections.Generic;

public class CharacterSwitcher : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private CharacterDatabase _characterDatabase;

    [Header("Current Character")]
    [SerializeField] private string _currentCharacterId;

    private GameObject _currentModel;
    private Animator _animator;
    private RuntimeAnimatorController _animatorController;

    // Sword references
    private Transform _swordHand;
    private Transform _swordSheathed;
    private Vector3 _swordHandLocalPos;
    private Vector3 _swordHandLocalRot;
    private Vector3 _swordSheathedLocalPos;
    private Vector3 _swordSheathedLocalRot;
    private string _swordHandBone;
    private string _swordSheathedBone;

    public string CurrentCharacterId => _currentCharacterId;
    public CharacterDatabase Database => _characterDatabase;
    public Animator CurrentAnimator => _animator;

    public event System.Action<Animator> OnCharacterSwitched;

    void Awake()
    {
        // Find current character model
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("SM_Chr_"))
            {
                _currentModel = child.gameObject;
                _animator = child.GetComponent<Animator>();
                if (_animator != null)
                {
                    _animatorController = _animator.runtimeAnimatorController;
                }
                break;
            }
        }

        // Cache sword positions
        CacheSwordTransforms();
    }

    void CacheSwordTransforms()
    {
        if (_currentModel == null) return;

        _swordHand = FindInHierarchy(_currentModel.transform, "SwordHand");
        _swordSheathed = FindInHierarchy(_currentModel.transform, "SwordSheathed");

        if (_swordHand != null)
        {
            _swordHandLocalPos = _swordHand.localPosition;
            _swordHandLocalRot = _swordHand.localEulerAngles;
            _swordHandBone = _swordHand.parent.name;
        }

        if (_swordSheathed != null)
        {
            _swordSheathedLocalPos = _swordSheathed.localPosition;
            _swordSheathedLocalRot = _swordSheathed.localEulerAngles;
            _swordSheathedBone = _swordSheathed.parent.name;
        }
    }

    public void SwitchCharacter(string characterId)
    {
        if (_characterDatabase == null)
        {
            Debug.LogError("[CharacterSwitcher] No character database assigned!");
            return;
        }

        CharacterData charData = _characterDatabase.GetCharacter(characterId);
        if (charData == null)
        {
            Debug.LogError($"[CharacterSwitcher] Character not found: {characterId}");
            return;
        }

        SwitchToCharacter(charData);
    }

    public void SwitchToCharacter(CharacterData charData)
    {
        if (charData.prefab == null)
        {
            Debug.LogError($"[CharacterSwitcher] No prefab for character: {charData.characterId}");
            return;
        }

        // Cache current sword transforms before destroying model
        CacheSwordTransforms();

        Debug.Log($"[CharacterSwitcher] Switching - swordHand: {(_swordHand != null ? _swordHand.name + " (active:" + _swordHand.gameObject.activeSelf + ")" : "null")}, swordSheathed: {(_swordSheathed != null ? _swordSheathed.name + " (active:" + _swordSheathed.gameObject.activeSelf + ")" : "null")}");

        // Store sword GameObjects and their active states
        GameObject swordHandObj = _swordHand != null ? _swordHand.gameObject : null;
        GameObject swordSheathedObj = _swordSheathed != null ? _swordSheathed.gameObject : null;
        bool swordHandWasActive = swordHandObj != null && swordHandObj.activeSelf;
        bool swordSheathedWasActive = swordSheathedObj != null && swordSheathedObj.activeSelf;

        // Unparent swords so they don't get destroyed
        if (swordHandObj != null) swordHandObj.transform.SetParent(transform);
        if (swordSheathedObj != null) swordSheathedObj.transform.SetParent(transform);

        // Destroy old model
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }

        // Instantiate new model
        _currentModel = Instantiate(charData.prefab, transform);
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;
        _currentModel.name = charData.prefab.name; // Remove "(Clone)"

        // Setup animator
        _animator = _currentModel.GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = _currentModel.AddComponent<Animator>();
        }
        if (_animatorController != null)
        {
            _animator.runtimeAnimatorController = _animatorController;
            _animator.applyRootMotion = false;
        }

        // Reattach swords to new bones
        if (swordHandObj != null && !string.IsNullOrEmpty(_swordHandBone))
        {
            Transform newBone = FindBoneRecursive(_currentModel.transform, _swordHandBone);
            if (newBone != null)
            {
                swordHandObj.transform.SetParent(newBone);
                swordHandObj.transform.localPosition = _swordHandLocalPos;
                swordHandObj.transform.localEulerAngles = _swordHandLocalRot;
                swordHandObj.transform.localScale = Vector3.one;
                _swordHand = swordHandObj.transform;
            }
        }

        if (swordSheathedObj != null && !string.IsNullOrEmpty(_swordSheathedBone))
        {
            Transform newBone = FindBoneRecursive(_currentModel.transform, _swordSheathedBone);
            if (newBone != null)
            {
                swordSheathedObj.transform.SetParent(newBone);
                swordSheathedObj.transform.localPosition = _swordSheathedLocalPos;
                swordSheathedObj.transform.localEulerAngles = _swordSheathedLocalRot;
                swordSheathedObj.transform.localScale = Vector3.one;
                _swordSheathed = swordSheathedObj.transform;
            }
        }

        // Restore sword active states
        if (swordHandObj != null) swordHandObj.SetActive(swordHandWasActive);
        if (swordSheathedObj != null) swordSheathedObj.SetActive(swordSheathedWasActive);

        _currentCharacterId = charData.characterId;
        Debug.Log($"[CharacterSwitcher] Switched to: {charData.displayName}, swordHand: {(swordHandObj != null ? swordHandObj.activeSelf.ToString() : "null")}, swordSheathed: {(swordSheathedObj != null ? swordSheathedObj.activeSelf.ToString() : "null")}");

        // Notify listeners
        OnCharacterSwitched?.Invoke(_animator);
    }

    Transform FindInHierarchy(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindInHierarchy(child, name);
            if (found != null) return found;
        }
        return null;
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name.ToLower().Contains(boneName.ToLower()))
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null) return found;
        }
        return null;
    }
}
