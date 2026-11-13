# Changelog

All notable changes to Ordered Work are documented here.
This format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.2.0-alpha.1] - 2026-11-13

### Added
- Pure `OrderedWork.Core` project with `DenseRank.NormalizeOrder` and `DenseRank.BuildRanks`.
- Console test harness (`Tests/OrderedWork.Tests`) covering normalization, dense ranks, appended ids, and null inputs.
- Technical-spike Mod Settings button (`Test dense rank on colonists (log)`) that writes dense rank 1..N to free colonists' `Pawn_WorkSettings`, raises `Pawn_WorkSettings.LowestPriority`, and logs the result.
- Version metadata (`About.xml`) and changelog tracking.

### Changed
- `OrderedWork.Core` targets `net10.0` to match the mod and test projects (offline build without `NETStandard.Library`).
- README documents the spike and warns not to use it in a main save.

## [0.1.0] - 2026-11-13

### Added
- Initial def-layer prototype: order work types and work givers, split a work type from a base type plus givers, merge givers into an existing work type.
- Split work types copy work tags, skills, life-stage availability and mech availability so traits and incapacities keep working.
