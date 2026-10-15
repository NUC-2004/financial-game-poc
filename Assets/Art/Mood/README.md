# Mood portraits

Original pixel art supplied by the user: 笑脸64px.aseprite, 平静64px.aseprite, 疲惫64px.aseprite, 撑不住64px.aseprite.

Exported without visual changes through Aseprite's batch exporter to Happy.png, Calm.png, Tired.png, Exhausted.png. Each PNG is 64×64 with transparency. Import as Sprite, Point filtering, no compression or mipmaps. The room card displays the portrait at 128×128 reference UI units.

Internal thresholds: Happy ≥80, Calm 60–79, Tired 40–59, Exhausted <40. A value of 40 is Tired. Reaching zero still ends the run. Gameplay shows expressions and qualitative feedback; numeric mood values remain available internally for development.
