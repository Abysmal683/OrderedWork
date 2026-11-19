# Credits and compatibility references

Ordered Work source code is original code written for this project from the user's requirements and the public RimWorld modding API.

## Third-party code and assets

- No third-party source code is copied into `Source/`.
- No third-party DLLs, XML, textures or other assets are bundled in this mod.
- The mod does not redistribute Vanilla Skills Expanded, Alpha Skills or Passion On Level Up Plus content.

## References consulted

These were consulted to understand public APIs and behavior. They are not code sources for Ordered Work.

- **RimWorld 1.7** (Ludeon Studios) — public modding API, `Def`, `Window`, `MainTabWindow`, `Passion`, `WorkTypeDef`.
- **RimWorld decompiled reference** (`josh-m/RW-Decompile`) — `Verse/Window.cs` and `RimWorld/MainTabWindow.cs`, read only to understand lifecycle and sizing. No code was copied.
- **[Vanilla Skills Expanded](https://github.com/Vanilla-Expanded/VanillaSkillsExpanded)** — `1.7/Source/VSE/Passions/PassionManager.cs` and `PassionDef.cs`, read only to learn the public `PassionManager.PassionToDef` / `PassionDef` contract used through reflection.
  - The repository has no LICENSE file at the time of inspection; treat it as all rights reserved.
  - Ordered Work does not include its code or assets. When VSE is installed, the mod reads `PassionDef.Icon` at runtime through the public API.
- **Alpha Skills** (Steam Workshop `3448953006`) — local XML/DLL inspected for the same `PassionDef` contract. No license file is visible in the package; treat it as all rights reserved. No code copied.
- **Passion On Level Up Plus** (Steam Workshop `3526025445`, Ayas) — local README/DLL inspected; no license file visible; no code copied.
- **Harmony** (brrainz.harmony) — runtime dependency only.

## Runtime compatibility

The VSE/Alpha Skills integration is a soft reflection layer in `Source/UI/WorkTypePaletteView.cs`. It:

- discovers `VSE.Passions.PassionManager` only when the assembly is loaded;
- calls `PassionToDef(Passion)` and reads `Icon`, `WorkBoxIcon`, `color` and `LabelCap`;
- catches reflection failures and falls back to vanilla icons;
- never writes to those mods' data;
- can be removed without breaking vanilla behavior.

If you are an author of one of these mods and want a different attribution or compatibility arrangement, open an issue or contact the maintainer.

## Icon asset policy

No external icon assets are currently bundled. If icons are added, only permissively licensed sets are acceptable, and the exact version/license must be recorded before integration.

| Source | License | Obligation |
|---|---|---|
| Lucide | ISC | Include the ISC copyright/permission notice |
| Feather | MIT | Include the MIT notice |
| Heroicons | MIT | Include the MIT notice |
| Tabler Icons | MIT | Include the MIT notice |
| Phosphor Icons | MIT | Include the MIT notice |
| Bootstrap Icons | MIT | Include the MIT notice |
| Material Symbols | Apache-2.0 | Include the NOTICE/license and respect trademark guidance |
| Kenney assets | CC0 | No attribution required; record provenance |
| Game-icons.net | CC BY 3.0 | Attribute the author and license |
| Font Awesome Free | CC BY 4.0 for icons; SIL OFL for fonts; MIT for code | Preserve the applicable notices |

Do not use icons with unknown, personal-use-only, paid, or share-alike terms unless the project explicitly accepts those terms. Verify each source file, not only the project landing page. SVG sources need conversion to a Unity-loadable texture; preserve the license next to the converted asset.
