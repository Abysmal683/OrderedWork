# PassionSource split

Goal: isolate optional VSE/Alpha Skills discovery, metadata and textures.

## Tasks

- [x] `PassionRegistry` owns reflection and registered passions.
- [x] `PassionMetadata` owns labels, descriptions, colours and learn rate.
- [x] `PassionIconResolver` owns optional `Icon`/`WorkBoxIcon` textures.
- [x] `PassionSource` remains a compatibility facade.
- [x] Existing callers compile without signature changes.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with and without VSE/Alpha Skills.

## Log

- L1. Discovery must not depend on UI or icon drawing.
- L2. Metadata must remain usable when no VSE is installed.
- L3. Icon resolver returns null and leaves fallback drawing to the UI.