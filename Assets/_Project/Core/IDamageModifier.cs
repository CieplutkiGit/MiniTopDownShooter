namespace Cieplutki.MiniTopDownShooter.Core
{
    public interface IDamageModifier
    {
        DamageData ModifyDamage(DamageData damage);
    }
}
