# Weapons

## Weapon Definition

`WeaponDefinition` is the reusable data asset for projectile weapons.

Create one using:

**Assets > Create > Mini Top Down Shooter > Weapon Definition**

or:

**Tools > Mini Top Down Shooter > Setup & Validation > Create Weapon Definition**

## Fields

### Projectile Prefab

Projectile spawned by the gun's object pool.

### Damage

Damage applied by each projectile.

### Fire Interval

Minimum number of seconds between accepted shots.

### Projectiles Per Shot

Number of projectiles spawned by one accepted shot. Use one for pistols and rifles, or several for shotgun-style weapons.

### Spread Angle

Maximum random horizontal angle applied to each projectile. Zero fires straight.

### Default Pool Size

Number of projectiles prewarmed when the gun starts.

### Max Pool Size

Maximum retained pool size.

## Backwards compatibility

The `Gun` component preserves its original inline fields. Those values are used whenever no `WeaponDefinition` is assigned, so existing scenes and prefabs do not need to be migrated immediately.

## Example configurations

- Pistol: one projectile, moderate interval, minimal spread
- SMG: short interval, moderate spread
- Shotgun: multiple projectiles, larger spread
- Rifle: one projectile, higher damage, very low spread

The current base framework remains projectile-focused. Ammo, reload, hitscan, runtime weapon switching, recoil profiles, and pickup inventories can be added as separate modules without making the base gun class monolithic.
