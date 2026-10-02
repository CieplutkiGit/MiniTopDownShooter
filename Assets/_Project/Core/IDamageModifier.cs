namespace Core
{
    public interface IDamageModifier
    {
        DamageData ModifyDamage(DamageData damage);
    }
}
