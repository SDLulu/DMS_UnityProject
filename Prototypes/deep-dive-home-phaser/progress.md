Original prompt: 사용자는 DEEP DIVE: HOME 시나리오 초안을 바탕으로, Unity로 바로 만들기보다 Phaser에서 시각 요소와 연출 느낌을 빠르게 피드백하며 잡고 나중에 Unity로 옮기고 싶다고 했다. 기존 Unity 플레이어 조작은 기준 감각으로 참고한다.

## Decisions
- Prototype path: `C:\UnityProjects\DMS\Prototypes\deep-dive-home-phaser`
- Purpose: visual direction, scene flow, dialogue timing, tutorial-room pacing.
- First slice: title -> prologue tutorial rooms -> HOME core failed extraction -> player home terminal.
- Phaser controls should only approximate Unity feel: move, jump, dash, attack, interact.

## TODO
- Build playable Phaser slice.
- Verify title/start, movement, jump, dash, attack, room gates, HOME core interaction, forced return, terminal text.
- Leave Unity project files untouched.

## Log
- Created Vite + TypeScript + Phaser scaffold.
- Implemented title, prologue rooms, player movement/jump/dash/attack, melee/drone enemies, HOME core interaction, forced return, home terminal, DOM dialogue/HUD, glitch overlay, and `render_game_to_text`.
- `npm run build` passed. Vite warned that Phaser bundle is larger than 500 kB, which is expected for this prototype.
- Switched Phaser renderer to `CANVAS` after headless Chromium reported a WebGL framebuffer error during visual verification.
- First automated playtest found the jump/dash tutorial gap too strict for a first visual-flow prototype, so Room 00-2 platform gaps were widened.
- Added hidden `window.__deepDiveTest` helpers so automated checks can jump to late rooms without changing the visible prototype.
- Pivoted the prototype from playable slice to code-driven visual reel after the user clarified that the goal is to judge graphics and direction.
- Replaced the gameplay-first main scene with 7 visual review scenes: title mood, dive start, action slice, HOME core, extraction failure, player home, ending door.
- Added `.gitignore` for local web artifacts and removed obsolete gameplay smoke/playtest scripts.

## Verification
- `npm run build` passes.
- `node visual-reel-capture.cjs` captured all 7 visual scenes with no console errors.
- After the existing-asset pass, `npm run build` and `node visual-reel-capture.cjs` pass again.

## Generated Image Pass
- Used built-in image generation to create 3 background concepts and 1 residual guardian concept.
- Saved project assets under `public/assets/generated/`:
  - `bg_title_alley.png`
  - `bg_memory_home.png`
  - `bg_player_room.png`
  - `spr_residual_guardian.png`
- Removed the guardian chroma-key background locally with the imagegen helper, then composited the transparent sprite into the extraction-failure scene.
- Updated the reel so generated images provide art direction while Phaser code still provides scanlines, glitch bars, HOME core rings, dialogue timing, rain, flashes, and scene controls.

## Existing Asset Pass
- Pivoted away from generated concept art as the main look after feedback that the target should feel closer to Sanabi-style pixel cyberpunk.
- Copied selected existing Unity project assets from `Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art` into `public/assets/craftpix/` for Phaser preview use.
- Rebuilt the visual reel around existing pixel assets: parallax city layers, market tiles, billboards/pillars, player idle sprite, and enemy idle sprite.
- Added code-side neon billboard labels, darker foreground silhouettes, rain, scanlines, speed lines, slash trails, and hit-burst effects to make the review closer to a high-contrast pixel-action direction.
- Removed direct use of TV sprite-sheet images as billboards because they were sheet assets, not single props.
- Replaced per-frame Phaser `tileSprite` background creation with regular image layers after the in-app browser showed a canvas out-of-memory error. This keeps the visual preview stable while retaining layered background motion.

## Next Suggestions
- Add a small scene selector overlay only for review builds if feedback sessions need faster jumps between rooms.
- Replace remaining code-drawn room props, drone, terminal, and apartment props with existing Unity pixel assets or newly made sprites.
- Make a dedicated action-timing capture around scene 3 at 40-60% progress so slash/hit-stop can be judged from the strongest frame instead of the scene opening.
- Tune enemy damage and invulnerability once the team decides whether the prototype should feel forgiving or close to Katana Zero.
