ent-BaseSimpleMob = { ent-['BaseMob', 'MobDamageable', 'MobPolymorphable', 'MobAtmosExposed'] }

  .suffix = AI
  .desc = { ent-['BaseMob', 'MobDamageable', 'MobPolymorphable', 'MobAtmosExposed'].desc }
ent-SimpleSpaceMobBase = { ent-['BaseSimpleMob', 'MobCombat', 'MobBloodstream', 'MobFlammable'] }

  .suffix = AI
  .desc = { ent-['BaseSimpleMob', 'MobCombat', 'MobBloodstream', 'MobFlammable'].desc }
ent-SimpleMobBase = { ent-['MobRespirator', 'MobAtmosStandard', 'SimpleSpaceMobBase'] }

  .suffix = AI
  .desc = { ent-['MobRespirator', 'MobAtmosStandard', 'SimpleSpaceMobBase'].desc }