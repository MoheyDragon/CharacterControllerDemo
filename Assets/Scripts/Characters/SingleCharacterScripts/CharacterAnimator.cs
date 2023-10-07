using UnityEngine;
using System;

public class CharacterAnimator : MonoBehaviour
{
    CharacterBehaviorsController characterBehaviorsController;
    Animator animator;
    [SerializeField] AnimatorOverrideController overrideController;
    [Space]
    [SerializeField] Transform[] characterHands;
    [SerializeField] Weapon[] weaponsPrefabs;
                     Weapon[] weapons;
    [SerializeField] Transform weaponsParent;
    [SerializeField] Transform[] weaponsPockets;
    public Transform GetPocketTransformByIndex(int pocketIndex) => weaponsPockets[pocketIndex];
    [SerializeField] float SheathDuration=0.5f;
    int currentWeaponSelectedIndex;
    private void Awake()
    {
        characterBehaviorsController = GetComponent<CharacterBehaviorsController>();
    }
    private void Start()
    {
        _Setup();
    }
    private void _Setup()
    {
        animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = overrideController;
        StarterAssets.StarterAssetsInputs input = characterBehaviorsController.input;
        input.Attack += OnAttackPressed;
        input.Draw += OnDrawPressed;
        input.SwitchWeapon += OnChangeWeaponPressed;
        weapons = new Weapon[weaponsPrefabs.Length];
        for (int i = 0; i < weaponsPrefabs.Length; i++)
        {
            weapons[i] = Instantiate(weaponsPrefabs[i], weaponsParent);
            weapons[i].SetupWeapon(this);
        }
    }
    private const string speedParameter = "Speed";
    private float changeMovingStateDuration = 1;
    public void ControlAnimationSpeed(float targetSpeed)
    {
        float prevSpeed = animator.GetFloat(speedParameter);
        LeanTween.value(prevSpeed,targetSpeed,changeMovingStateDuration).setOnUpdate(
            (float value)=>animator.SetFloat(speedParameter, value));
    }
    private bool animatorInAction;
    public bool IsAnimatorInAction => animatorInAction;
    private bool IsWeaponDrawn;
    private string DrawAnimatorParameter = "Draw";
    private string SheathAnimatorParameter = "Sheath";
    private void OnDrawPressed()
    {
        if (animatorInAction) 
            return;

        if (IsWeaponDrawn)
            SheathWeapon();
        else
            DrawWeapon();
    }
    private string AttackAnimatorParameter = "Attack";
    private void OnAttackPressed()
    {
        if (animatorInAction)
            return;

        if (IsWeaponDrawn)
            Attack();
        else
            DrawWeapon();
    }
    bool weaponIndexSwitcherPending;
    private void OnChangeWeaponPressed()
    {
        if (weapons.Length == 1) return;
        if (animatorInAction) return;
        if (IsWeaponDrawn)
        {
            SheathWeapon();
            weaponIndexSwitcherPending = true;
        }
        else
        {
            DoWeaponSwitch();
        }
    }
    private void DoWeaponSwitch()
    {
        _SwitchCurrentWeaponIndex();
        weaponIndexSwitcherPending = false;
    }
    private void Attack()
    {
        animatorInAction = true;
        print(animatorInAction);
        animator.SetTrigger(AttackAnimatorParameter);
    }
    public void ReachedAttackingFrame()
    {
        weapons[currentWeaponSelectedIndex].HandleDamage();
    }
    private void DrawWeapon()
    {
        animatorInAction = true;
        print(animatorInAction);
        RaiseWeaponUpperBodyLayer();
        animator.SetTrigger(DrawAnimatorParameter);
    }
    private void SheathWeapon()
    {
        animatorInAction = true;
        print(animatorInAction);
        animator.SetTrigger(SheathAnimatorParameter);
    }
    public void MoveWeaponToCombatSlot()
    {
        EnterCombatMode();
        weapons[currentWeaponSelectedIndex].Draw(characterHands);
    }
    public void MoveWeaponToIdleSlot()
    {
        ExitCombatMode();
        weapons[currentWeaponSelectedIndex].Sheath();
    }
    public void AnimationFinished()
    {
        animatorInAction = false;
        print(animatorInAction);
    }
    public void OnDrawFinish()
    {
        animatorInAction = false;
        print(animatorInAction);
        IsWeaponDrawn = !IsWeaponDrawn;
    }
    
    private void EnterCombatMode()
    {
        ChangeLayerWeight(GetWeaponCombatLayer, 1);
    }
    private void ExitCombatMode()
    {
        ChangeLayerWeight(GetWeaponCombatLayer, 0,LowerWeaponUpperBodyLayer);
    }
    private void ChangeLayerWeight(int layerIndex, float targetWeight, Action callBack = null)
    {
        LeanTween.value(gameObject, 1, 0, SheathDuration).setEaseInOutSine().setOnUpdate((float value) =>
            animator.SetLayerWeight(layerIndex, targetWeight)
        ).setOnComplete(() =>{ if (callBack != null) callBack.Invoke(); });
    }
    private void _SwitchCurrentWeaponIndex()
    {
        currentWeaponSelectedIndex++;
        if (currentWeaponSelectedIndex==weapons.Length)
            currentWeaponSelectedIndex = 0;
    }
    private void RaiseWeaponUpperBodyLayer()
    {
        animator.SetLayerWeight(GetWeaponUpperBodyLayer, 1);
    }
    private void LowerWeaponUpperBodyLayer()
    {
        animator.SetLayerWeight(GetWeaponUpperBodyLayer, 0);
        if (weaponIndexSwitcherPending)
            DoWeaponSwitch();
    }
    // There Should be 2 layers per weapon, first is synched with the Base layer and has the non-combat animations that should play
    // while selecting this weapon (like walking- jumping,...), Second layer is not synched with base layer, and have an avatar mask 
    // of upper body only, and it's responsible of Drawing, Sheathin and attack animations
    private int GetWeaponCombatLayer=> (currentWeaponSelectedIndex * 2) + 1;
    private int GetWeaponUpperBodyLayer => (currentWeaponSelectedIndex * 2) + 2;
}
