ent-BaseGlassBox = { ent-['BaseStructureDynamic', 'BaseItemCabinetGlass'] }

  .desc = { ent-['BaseStructureDynamic', 'BaseItemCabinetGlass'].desc }
ent-GlassBox = glass box
    .desc = A sturdy showcase for an expensive exhibit.
ent-GlassBoxLaser = { ent-GlassBox }
    .suffix = AntiqueLaser
    .desc = { ent-GlassBox.desc }
ent-GlassBoxLaserOpen = { ent-GlassBoxLaser }
    .suffix = AntiqueLaser, Open
    .desc = { ent-GlassBoxLaser.desc }
ent-GlassBoxLaserFilled = { ent-GlassBoxLaser }
    .suffix = AntiqueLaser, Filled
    .desc = { ent-GlassBoxLaser.desc }
ent-GlassBoxLaserFilledOpen = { ent-['GlassBoxLaserFilled', 'GlassBoxLaserOpen'] }

  .suffix = AntiqueLaser, Filled, Open
  .desc = { ent-['GlassBoxLaserFilled', 'GlassBoxLaserOpen'].desc }
ent-GlassBoxFrame = glass box frame
    .desc = A glassless sturdy showcase for an expensive exhibit.
    .suffix = Frame
ent-GlassBoxBroken = broken glass box
    .desc = A broken showcase for a stolen expensive exhibit.
    .suffix = Broken
